using Qhlc.Governance;
using Xunit;

namespace Qhlc.Governance.Tests;

public class RepoPathsTests
{
    [Fact]
    public void FindRepoRoot_finds_governance_folder()
    {
        var root = RepoPaths.FindRepoRoot(AppContext.BaseDirectory);
        Assert.True(File.Exists(Path.Combine(root, "governance", "registry", "modules.yaml")));
    }
}

public class ArchitectureCheckerTests
{
    [Fact]
    public void Validate_succeeds_for_current_repository()
    {
        var root = RepoPaths.FindRepoRoot(AppContext.BaseDirectory);
        var errors = ArchitectureChecker.Validate(root);
        Assert.Empty(errors);
    }
}

public class ChangeValidatorTests
{
    [Fact]
    public void Validate_CHG_0001_succeeds_when_tests_exist()
    {
        var root = RepoPaths.FindRepoRoot(AppContext.BaseDirectory);
        var errors = ChangeValidator.Validate(root, "CHG-0001");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_CHG_0002_succeeds_when_tests_exist()
    {
        var root = RepoPaths.FindRepoRoot(AppContext.BaseDirectory);
        var errors = ChangeValidator.Validate(root, "CHG-0002");
        Assert.Empty(errors);
    }
}
