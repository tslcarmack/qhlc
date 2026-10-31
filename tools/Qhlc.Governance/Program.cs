using Qhlc.Governance;

var repoRoot = RepoPaths.FindRepoRoot(AppContext.BaseDirectory);
var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

if (cliArgs.Length == 0)
{
    PrintUsage();
    return 1;
}

try
{
    return cliArgs[0] switch
    {
        "check" => RunCheck(repoRoot, cliArgs.Skip(1).ToArray()),
        "impact" => RunImpact(repoRoot, cliArgs.Skip(1).ToArray()),
        "verify-chg" => RunVerifyChange(repoRoot, cliArgs.Skip(1).ToArray()),
        _ => Unknown(cliArgs[0]),
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static int RunCheck(string repoRoot, string[] rest)
{
    if (rest.Length == 0 || rest[0] == "all")
    {
        return RunAllChecks(repoRoot);
    }

    var errors = rest[0] switch
    {
        "architecture" => ArchitectureChecker.Validate(repoRoot),
        "contracts" => ContractChecker.Validate(repoRoot),
        "doc-language" => DocLanguageChecker.Validate(repoRoot),
        _ => null,
    };

    if (errors is null)
    {
        return Unknown(rest[0]);
    }

    return PrintErrors(errors);
}

static int RunAllChecks(string repoRoot)
{
    var errors = new List<string>();
    errors.AddRange(ArchitectureChecker.Validate(repoRoot));
    errors.AddRange(ContractChecker.Validate(repoRoot));
    errors.AddRange(DocLanguageChecker.Validate(repoRoot));

    var activeChg = Environment.GetEnvironmentVariable("QHLC_ACTIVE_CHG") ?? "CHG-0001";
    errors.AddRange(ChangeValidator.Validate(repoRoot, activeChg));

    return PrintErrors(errors);
}

static int RunImpact(string repoRoot, string[] rest)
{
    if (rest.Length == 0)
    {
        Console.Error.WriteLine("用法: impact CHG-0001");
        return 1;
    }

    foreach (var line in ImpactAnalyzer.Analyze(repoRoot, rest[0]))
    {
        Console.WriteLine(line);
    }

    return ImpactAnalyzer.HasBlockingIssues(repoRoot, rest[0]) ? 1 : 0;
}

static int RunVerifyChange(string repoRoot, string[] rest)
{
    if (rest.Length == 0)
    {
        Console.Error.WriteLine("用法: verify-chg CHG-0001");
        return 1;
    }

    return PrintErrors(ChangeValidator.Validate(repoRoot, rest[0]));
}

static int PrintErrors(IReadOnlyList<string> errors)
{
    if (errors.Count == 0)
    {
        Console.WriteLine("检查通过。");
        return 0;
    }

    Console.Error.WriteLine("检查失败：");
    foreach (var error in errors)
    {
        Console.Error.WriteLine("  - " + error);
    }

    return 1;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"未知命令: {command}");
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("""
        Qhlc 治理 CLI
        用法:
          check architecture|contracts|doc-language|all
          impact CHG-0001
          verify-chg CHG-0001
        环境变量:
          QHLC_ACTIVE_CHG  — check all 时校验的变更单（默认 CHG-0001）
        """);
}
