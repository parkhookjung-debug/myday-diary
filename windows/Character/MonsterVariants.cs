using System;

namespace MyDay.Windows.Character
{
    public enum MonsterVariant { Original, Puffy, Winged, Speedy, Dazed, Spiky, Horned, Mini }
    public static class MonsterVariants
    {
        public static readonly MonsterVariant[] All=(MonsterVariant[])Enum.GetValues(typeof(MonsterVariant));
        private static readonly string[] Ids={"original","puffy","winged","speedy","dazed","spiky","horned","mini"};
        private static readonly string[] Names={"기본 몬스터","빵빵 몬스터","날개 몬스터","쌩쌩 몬스터","멍한 몬스터","삐죽 몬스터","뿔 몬스터","꼬마 몬스터"};
        public static string Id(MonsterVariant variant) { return Ids[(int)variant]; }
        public static string Name(MonsterVariant variant) { return Names[(int)variant]; }
        public static MonsterVariant FromId(string id)
        {
            int index=Array.IndexOf(Ids,id); return index<0?MonsterVariant.Original:(MonsterVariant)index;
        }
    }
}
