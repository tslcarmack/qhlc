using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Qhlc.Governance;

public sealed class ManualReviewItem
{
    public string Description { get; set; } = string.Empty;

    public bool Confirmed { get; set; }
}

public sealed class ChangeImpact
{
    public List<string> Modules { get; set; } = [];

    public List<string> Apis { get; set; } = [];

    public List<ManualReviewItem> ManualReview { get; set; } = [];
}

public sealed class ChangeVerification
{
    public List<string> Tests { get; set; } = [];
}

public sealed class ChangeRecord
{
    public string Id { get; set; } = string.Empty;

    public List<string> Requirements { get; set; } = [];

    public List<string> Scope { get; set; } = [];

    public ChangeImpact Impact { get; set; } = new();

    public ChangeVerification Verification { get; set; } = new();

    public List<string> Documentation { get; set; } = [];
}

public static class ChangeRecordLoader
{
    public static ChangeRecord Load(string repoRoot, string changeId)
    {
        var path = Path.Combine(repoRoot, "governance", "trace", "changes", $"{changeId}.yaml");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"未找到变更单 {path}");
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<ChangeRecord>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"无法解析 {path}");
    }
}

public static class TraceTestIndex
{
    public static HashSet<string> FindRegisteredTestIds(string repoRoot)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var testsRoot = Path.Combine(repoRoot, "tests");
        if (!Directory.Exists(testsRoot))
        {
            return ids;
        }

        foreach (var file in Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(
                         text,
                         "Trait\\(\"TraceId\",\\s*\"(TEST-\\d+)\"\\)"))
            {
                ids.Add(match.Groups[1].Value);
            }
        }

        return ids;
    }
}

public static class ChangeValidator
{
    public static IReadOnlyList<string> Validate(string repoRoot, string changeId)
    {
        var errors = new List<string>();
        ChangeRecord change;
        try
        {
            change = ChangeRecordLoader.Load(repoRoot, changeId);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            return errors;
        }

        if (!string.Equals(change.Id, changeId, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"变更单文件 id 为 {change.Id}，与参数 {changeId} 不一致。");
        }

        foreach (var req in change.Requirements)
        {
            var reqPath = Path.Combine(repoRoot, "governance", "trace", "requirements", $"{req}.yaml");
            if (!File.Exists(reqPath))
            {
                errors.Add($"变更 {changeId} 引用的需求 {req} 不存在。");
            }
        }

        var testIds = TraceTestIndex.FindRegisteredTestIds(repoRoot);
        foreach (var testId in change.Verification.Tests)
        {
            if (!testIds.Contains(testId))
            {
                errors.Add($"变更 {changeId} 的 verification.tests 含 {testId}，但 tests/ 中未找到 Trait(\"TraceId\", \"{testId}\")。");
            }
        }

        foreach (var docPath in change.Documentation)
        {
            var full = Path.IsPathRooted(docPath) ? docPath : Path.Combine(repoRoot, docPath);
            if (!File.Exists(full))
            {
                errors.Add($"变更 {changeId} 的 documentation 路径不存在：{docPath}");
            }
        }

        foreach (var item in change.Impact.ManualReview)
        {
            if (!item.Confirmed)
            {
                errors.Add($"变更 {changeId} 的 manual_review 未确认：{item.Description}");
            }
        }

        return errors;
    }
}

public static class ImpactAnalyzer
{
    public static IReadOnlyList<string> Analyze(string repoRoot, string changeId)
    {
        var lines = new List<string>();
        var change = ChangeRecordLoader.Load(repoRoot, changeId);
        var manifest = ModuleManifest.Load(repoRoot);
        var byId = ModuleManifest.ById(manifest);

        lines.Add($"=== 影响分析 {changeId} ===");

        var downstream = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var moduleId in change.Impact.Modules)
        {
            foreach (var other in manifest.Modules)
            {
                if (other.DependsOn.Contains(moduleId, StringComparer.OrdinalIgnoreCase))
                {
                    downstream.Add(other.Id);
                }
            }
        }

        if (downstream.Count > 0)
        {
            lines.Add("下游模块（depends_on 指向 scope 模块）：" + string.Join(", ", downstream));
        }

        foreach (var apiId in change.Impact.Apis)
        {
            var contractPath = Path.Combine(repoRoot, "governance", "registry", "contracts", $"{apiId}.yaml");
            if (!File.Exists(contractPath))
            {
                lines.Add($"契约 {apiId} 文件不存在。");
                continue;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var contract = deserializer.Deserialize<ApiContractFile>(File.ReadAllText(contractPath));
            if (contract is not null && contract.Consumers.Count > 0)
            {
                lines.Add($"契约 {apiId} 消费者：" + string.Join(", ", contract.Consumers));
            }
        }

        var validationErrors = ChangeValidator.Validate(repoRoot, changeId);
        if (validationErrors.Count > 0)
        {
            lines.Add("校验问题：");
            lines.AddRange(validationErrors.Select(e => "  - " + e));
        }

        return lines;
    }

    public static bool HasBlockingIssues(string repoRoot, string changeId)
    {
        return ChangeValidator.Validate(repoRoot, changeId).Count > 0;
    }
}
