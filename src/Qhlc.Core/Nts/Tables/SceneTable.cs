using Qhlc.Core.Public;

namespace Qhlc.Core.Nts.Tables;

public static class SceneTable
{
    public const int RecordSize = 134;

    public static IReadOnlyList<SceneRecord> Load(string assetsRoot)
    {
        var path = Path.Combine(NtsPaths.DataDirectory(assetsRoot), "Scene.Dat");
        var file = NtsTableFile.Open(path, RecordSize);
        var list = new List<SceneRecord>();
        foreach (var rec in file.EnumerateDataRows())
        {
            if (rec.Length < 24)
                continue;

            var areaId = (int)NtsRecordFields.ReadScrambled(
                rec, 0, 2, NtsRecordFields.SceneAreaKey, NtsRecordFields.SceneAreaBias);
            var seq = (byte)NtsRecordFields.ReadPlainU8(rec, 2);
            var mapSet = (byte)NtsRecordFields.ReadPlainU8(rec, 3);
            var name = NtsText.DecodeFixedBig5(rec.AsSpan(4, 20));
            list.Add(new SceneRecord(areaId, seq, mapSet, name));
        }

        return list;
    }
}
