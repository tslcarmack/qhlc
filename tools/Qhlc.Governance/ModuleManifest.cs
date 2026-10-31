using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Qhlc.Governance;

public sealed class ModuleEntry
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Assembly { get; set; } = string.Empty;

    public List<string> DependsOn { get; set; } = [];

    public List<string> PublicNamespaces { get; set; } = [];
}

public sealed class ModuleManifestRoot
{
    public List<ModuleEntry> Modules { get; set; } = [];
}

public static class ModuleManifest
{
    public static ModuleManifestRoot Load(string repoRoot)
    {
        var path = Path.Combine(repoRoot, "governance", "registry", "modules.yaml");
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<ModuleManifestRoot>(yaml)
            ?? throw new InvalidOperationException("modules.yaml 解析失败。");
    }

    public static IReadOnlyDictionary<string, ModuleEntry> ById(ModuleManifestRoot root)
    {
        return root.Modules.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyDictionary<string, ModuleEntry> ByAssembly(ModuleManifestRoot root)
    {
        return root.Modules.ToDictionary(m => m.Assembly, StringComparer.OrdinalIgnoreCase);
    }
}
