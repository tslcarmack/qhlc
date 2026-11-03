using Qhlc.Core.Public;

namespace Qhlc.Core.Nts.Tables;

public static class SkillTable
{
    public const int RecordSize = 86;

    public static IReadOnlyList<SkillRecord> Load(string assetsRoot)
    {
        var path = Path.Combine(NtsPaths.DataDirectory(assetsRoot), "Skill.Dat");
        var file = NtsTableFile.Open(path, RecordSize);
        var list = new List<SkillRecord>();
        foreach (var rec in file.EnumerateDataRows())
        {
            if (rec.Length < 86)
                continue;

            var name = NtsText.DecodeSkillName(rec);
            var id = (int)NtsRecordFields.ReadScrambled(
                rec, 22, 2, NtsRecordFields.SkillU16Key, NtsRecordFields.SkillBias);
            if (id <= 0)
                continue;

            var kind = (int)NtsRecordFields.ReadScrambled(
                rec, 21, 1, NtsRecordFields.SkillU8Key, NtsRecordFields.SkillBias);
            var mp = (int)NtsRecordFields.ReadScrambled(
                rec, 24, 2, NtsRecordFields.SkillU16Key, NtsRecordFields.SkillBias);
            var p32 = (int)NtsRecordFields.ReadScrambled(
                rec, 32, 1, NtsRecordFields.SkillU8Key, NtsRecordFields.SkillBias);
            var targetParam = (int)NtsRecordFields.ReadScrambled(
                rec, 34, 1, NtsRecordFields.SkillU8Key, NtsRecordFields.SkillBias);

            list.Add(new SkillRecord(id, name, kind, mp, p32, targetParam));
        }

        return list;
    }
}
