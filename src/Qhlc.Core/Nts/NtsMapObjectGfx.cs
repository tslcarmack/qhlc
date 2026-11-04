using System.Text.RegularExpressions;
using Qhlc.Core.Public;

namespace Qhlc.Core.Nts;

/// <summary>
/// Load map object BMP overlays from Images*.BMg (tsdocs §05.2 step 4).
/// </summary>
public sealed class NtsMapObjectGfx
{
    private static readonly Regex ImagesArchivePattern = new(
        @"^images.*\.bmg$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly string _picDir;
    private readonly Dictionary<string, NtsArchive> _openArchives = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, NtsRaster> _cache = new();
    private readonly HashSet<int> _missing = new();

    public NtsMapObjectGfx(string assetsRoot)
    {
        _picDir = NtsPaths.PicDirectory(assetsRoot);
    }

    /// <summary>
    /// Try load object <paramref name="objectId"/> as green-keyed first frame.
    /// </summary>
    public bool TryLoad(int objectId, out NtsRaster raster)
    {
        if (_cache.TryGetValue(objectId, out raster!))
            return true;

        raster = null!;
        if (_missing.Contains(objectId) || !Directory.Exists(_picDir))
            return false;

        var entryName = $"{objectId}.bmp";
        foreach (var path in Directory.EnumerateFiles(_picDir))
        {
            if (!ImagesArchivePattern.IsMatch(Path.GetFileName(path)))
                continue;

            if (!_openArchives.TryGetValue(path, out var archive))
            {
                try
                {
                    archive = NtsArchive.Open(path);
                }
                catch
                {
                    continue;
                }

                _openArchives[path] = archive;
            }

            if (!archive.TryReadEntry(entryName, out var bmp))
                continue;

            if (!TryDecode(bmp, out raster))
            {
                _missing.Add(objectId);
                return false;
            }

            _cache[objectId] = raster;
            return true;
        }

        _missing.Add(objectId);
        return false;
    }

    /// <summary>Stateless one-shot helper for tests.</summary>
    public static bool TryLoad(string assetsRoot, int objectId, out NtsRaster raster)
    {
        var gfx = new NtsMapObjectGfx(assetsRoot);
        return gfx.TryLoad(objectId, out raster);
    }

    private static bool TryDecode(byte[] bmp, out NtsRaster raster)
    {
        raster = null!;
        try
        {
            var full = NtsBmp.Decode(bmp, NtsBmpKey.PureGreen);
            raster = NtsBmp.TakeFirstFrame(full);
            return raster.Width > 0 && raster.Height > 0;
        }
        catch
        {
            return false;
        }
    }
}
