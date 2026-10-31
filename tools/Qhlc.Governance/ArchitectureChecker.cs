using System.Xml.Linq;

namespace Qhlc.Governance;

public static class ArchitectureChecker
{
    public static IReadOnlyList<string> Validate(string repoRoot)
    {
        var errors = new List<string>();
        var root = ModuleManifest.Load(repoRoot);
        var byId = ModuleManifest.ById(root);
        var byAssembly = ModuleManifest.ByAssembly(root);

        foreach (var module in root.Modules)
        {
            var csprojPath = Path.Combine(repoRoot, "src", module.Assembly, $"{module.Assembly}.csproj");
            if (!File.Exists(csprojPath))
            {
                errors.Add($"模块 {module.Id} 缺少项目文件 {csprojPath}");
                continue;
            }

            var references = ReadProjectReferences(csprojPath)
                .Select(r => Path.GetFileNameWithoutExtension(r))
                .Where(name => name.StartsWith("Qhlc.", StringComparison.Ordinal))
                .ToList();

            var allowed = new HashSet<string>(
                module.DependsOn.Select(id => byId[id].Assembly),
                StringComparer.OrdinalIgnoreCase);

            foreach (var reference in references)
            {
                if (!allowed.Contains(reference))
                {
                    errors.Add(
                        $"模块 {module.Id} ({module.Assembly}) 引用了 {reference}，但 manifest 未允许（depends_on: [{string.Join(", ", module.DependsOn)}]）。");
                }
            }

            foreach (var depId in module.DependsOn)
            {
                if (!byId.TryGetValue(depId, out var dep))
                {
                    errors.Add($"模块 {module.Id} 的 depends_on 含未知 ID {depId}");
                    continue;
                }

                if (!references.Contains(dep.Assembly, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"模块 {module.Id} 在 manifest 依赖 {depId} ({dep.Assembly})，但 csproj 未包含 ProjectReference。");
                }
            }
        }

        errors.AddRange(DetectCycles(root));

        var gameCsproj = Path.Combine(repoRoot, "Qhlc.Game.csproj");
        if (File.Exists(gameCsproj))
        {
            var gameRefs = ReadProjectReferences(gameCsproj)
                .Select(r => Path.GetFileNameWithoutExtension(r))
                .Where(name => name.StartsWith("Qhlc.", StringComparison.Ordinal))
                .ToList();
            foreach (var reference in gameRefs)
            {
                if (!byAssembly.ContainsKey(reference))
                {
                    errors.Add($"Qhlc.Game 引用了未在 modules.yaml 登记的程序集 {reference}。");
                }
            }
        }

        return errors;
    }

    private static List<string> ReadProjectReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        return doc.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    private static IEnumerable<string> DetectCycles(ModuleManifestRoot root)
    {
        var errors = new List<string>();
        var byId = ModuleManifest.ById(root);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<string>();

        foreach (var module in root.Modules)
        {
            try
            {
                Visit(module.Id);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"模块依赖存在环：{ex.Message}");
                break;
            }
        }

        return errors;

        void Visit(string id)
        {
            if (visited.Contains(id))
            {
                return;
            }

            if (!visiting.Add(id))
            {
                var cycleStart = stack.IndexOf(id);
                var cycle = stack.Skip(cycleStart).Append(id);
                throw new InvalidOperationException(string.Join(" -> ", cycle));
            }

            stack.Add(id);
            if (byId.TryGetValue(id, out var entry))
            {
                foreach (var dep in entry.DependsOn)
                {
                    Visit(dep);
                }
            }

            stack.RemoveAt(stack.Count - 1);
            visiting.Remove(id);
            visited.Add(id);
        }
    }
}
