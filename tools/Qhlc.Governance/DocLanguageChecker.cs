using System.Text.RegularExpressions;

namespace Qhlc.Governance;

public static class DocLanguageChecker
{
    private static readonly string[] DocRoots =
    [
        "docs/governance",
        "governance/policies",
        "governance/trace/adrs",
    ];

    public static IReadOnlyList<string> Validate(string repoRoot)
    {
        var errors = new List<string>();

        foreach (var relativeRoot in DocRoots)
        {
            var absolute = Path.Combine(repoRoot, relativeRoot);
            if (!Directory.Exists(absolute))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(absolute, "*.md", SearchOption.AllDirectories))
            {
                errors.AddRange(ValidateFrontMatter(file));
            }
        }

        return errors;
    }

    private static IEnumerable<string> ValidateFrontMatter(string filePath)
    {
        var errors = new List<string>();
        var text = File.ReadAllText(filePath);
        if (!text.StartsWith("---", StringComparison.Ordinal))
        {
            errors.Add($"{filePath} 缺少 YAML front matter（需以 --- 开头）。");
            return errors;
        }

        var end = text.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            errors.Add($"{filePath} front matter 未闭合。");
            return errors;
        }

        var block = text[3..end];
        var status = MatchValue(block, "status");
        var module = MatchValue(block, "module");
        var appliesFrom = MatchValue(block, "applies_from");
        var replaces = MatchValue(block, "replaces");

        if (string.IsNullOrWhiteSpace(status))
        {
            errors.Add($"{filePath} front matter 缺少 status。");
        }

        if (string.IsNullOrWhiteSpace(module))
        {
            errors.Add($"{filePath} front matter 缺少 module。");
        }

        if (string.IsNullOrWhiteSpace(appliesFrom))
        {
            errors.Add($"{filePath} front matter 缺少 applies_from。");
        }

        if (string.Equals(status, "deprecated", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(replaces) || string.Equals(replaces, "null", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{filePath} 已 deprecated 但未设置 replaces。");
            }
        }

        return errors;
    }

    private static string? MatchValue(string block, string key)
    {
        var match = Regex.Match(block, $"^{key}:\\s*(.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
