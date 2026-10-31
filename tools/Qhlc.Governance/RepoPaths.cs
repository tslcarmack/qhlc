namespace Qhlc.Governance;

public static class RepoPaths
{
    public static string FindRepoRoot(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            var marker = Path.Combine(dir.FullName, "governance", "registry", "modules.yaml");
            if (File.Exists(marker))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("未找到 governance/registry/modules.yaml，请在仓库根目录执行。");
    }
}
