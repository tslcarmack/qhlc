using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Qhlc.Governance;

public sealed class ApiContractFile
{
    public string Id { get; set; } = string.Empty;

    public string OwnerModule { get; set; } = string.Empty;

    public List<string> Consumers { get; set; } = [];
}

public static class ContractChecker
{
    public static IReadOnlyList<string> Validate(string repoRoot)
    {
        var errors = new List<string>();
        var manifest = ModuleManifest.Load(repoRoot);
        var byId = ModuleManifest.ById(manifest);
        var contractsDir = Path.Combine(repoRoot, "governance", "registry", "contracts");
        if (!Directory.Exists(contractsDir))
        {
            errors.Add("缺少 governance/registry/contracts 目录。");
            return errors;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        foreach (var file in Directory.GetFiles(contractsDir, "API-*.yaml"))
        {
            var contract = deserializer.Deserialize<ApiContractFile>(File.ReadAllText(file))
                ?? throw new InvalidOperationException($"无法解析 {file}");

            if (string.IsNullOrWhiteSpace(contract.Id))
            {
                errors.Add($"{file} 缺少 id。");
            }

            if (contract.Consumers.Count == 0)
            {
                errors.Add($"{file} 缺少 consumers。");
            }

            if (string.IsNullOrWhiteSpace(contract.OwnerModule))
            {
                errors.Add($"{file} 缺少 owner_module。");
            }
            else if (!byId.ContainsKey(contract.OwnerModule))
            {
                errors.Add($"{file} 的 owner_module {contract.OwnerModule} 未在 modules.yaml 登记。");
            }

            foreach (var consumer in contract.Consumers)
            {
                if (!byId.ContainsKey(consumer))
                {
                    errors.Add($"{file} 的 consumer {consumer} 未在 modules.yaml 登记。");
                }
            }
        }

        return errors;
    }
}
