namespace Qhlc.Core.Nts;

public static class NtsPaths
{
    public static string ResolveAssetsRoot(string? overridePath = null)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
            return Path.GetFullPath(overridePath);

        foreach (var start in CandidateStartDirectories())
        {
            var dir = start;
            for (var depth = 0; depth < 32 && dir is not null; depth++)
            {
                if (HasAssetsData(dir))
                    return dir;

                dir = Directory.GetParent(dir)?.FullName;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    public static string DataDirectory(string assetsRoot) =>
        Path.Combine(assetsRoot, "assets", "data");

    public static string PicDirectory(string assetsRoot) =>
        Path.Combine(assetsRoot, "assets", "pic");

    public static string JmaDirectory(string assetsRoot) =>
        Path.Combine(assetsRoot, "assets", "jma");

    private static IEnumerable<string> CandidateStartDirectories()
    {
        yield return AppContext.BaseDirectory;

        var cwd = Directory.GetCurrentDirectory();
        if (!string.IsNullOrEmpty(cwd))
            yield return cwd;

        var asm = typeof(NtsPaths).Assembly.Location;
        if (!string.IsNullOrEmpty(asm))
        {
            var asmDir = Path.GetDirectoryName(asm);
            if (!string.IsNullOrEmpty(asmDir))
                yield return asmDir;
        }
    }

    private static bool HasAssetsData(string root)
    {
        var data = Path.Combine(root, "assets", "data");
        return File.Exists(Path.Combine(data, "Npc.Dat"))
            || File.Exists(Path.Combine(data, "Item.Dat"));
    }
}
