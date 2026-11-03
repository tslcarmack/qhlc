using Qhlc.Core.Public;

namespace Qhlc.Core.Nts.Tables;

public static class NpcTable
{
    public const int RecordSize = 97;

    public static IReadOnlyList<NpcRecord> Load(string assetsRoot)
    {
        var path = Path.Combine(NtsPaths.DataDirectory(assetsRoot), "Npc.Dat");
        var file = NtsTableFile.Open(path, RecordSize);
        var list = new List<NpcRecord>();
        foreach (var rec in file.EnumerateDataRows())
        {
            if (rec.Length < 97)
                continue;

            var name = NtsText.DecodeNpcName(rec);
            if (string.IsNullOrEmpty(name))
                continue;

            var id = (int)NtsRecordFields.ReadScrambled(
                rec, 12, 2, NtsRecordFields.NpcU16Key, NtsRecordFields.NpcBias);
            var modelId = (int)NtsRecordFields.ReadScrambled(
                rec, 14, 2, NtsRecordFields.NpcU16Key, NtsRecordFields.NpcBias);
            var level = (int)NtsRecordFields.ReadScrambled(
                rec, 37, 1, NtsRecordFields.NpcU8Key, NtsRecordFields.NpcBias);
            var hp = (int)NtsRecordFields.ReadScrambled(
                rec, 38, 4, NtsRecordFields.NpcU32Key, NtsRecordFields.NpcBias);
            var sp = (int)NtsRecordFields.ReadScrambled(
                rec, 42, 4, NtsRecordFields.NpcU32Key, NtsRecordFields.NpcBias);
            var atk = (int)NtsRecordFields.ReadScrambled(
                rec, 52, 2, NtsRecordFields.NpcU16Key, NtsRecordFields.NpcBias);
            var def = (int)NtsRecordFields.ReadScrambled(
                rec, 54, 2, NtsRecordFields.NpcU16Key, NtsRecordFields.NpcBias);
            var intel = (int)NtsRecordFields.ReadScrambled(
                rec, 50, 2, NtsRecordFields.NpcU16Key, NtsRecordFields.NpcBias);
            var agi = (int)NtsRecordFields.ReadScrambled(
                rec, 56, 2, NtsRecordFields.NpcU16Key, NtsRecordFields.NpcBias);
            var faction = (int)NtsRecordFields.ReadScrambled(
                rec, 58, 1, NtsRecordFields.NpcU8Key, NtsRecordFields.NpcBias);

            list.Add(new NpcRecord(id, name, modelId, level, hp, sp, atk, def, intel, agi, faction));
        }

        return list;
    }
}
