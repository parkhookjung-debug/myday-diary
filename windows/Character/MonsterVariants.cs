using System;

namespace MyDay.Windows.Character
{
    public enum MonsterVariant { Original, Puffy, Winged, Speedy, Dazed, Spiky, Horned, Mini, Droplet, Puddle, Pill, Cube, Cloud, Sprout, Twin, Ribbon }
    public static class MonsterVariants
    {
        public static readonly MonsterVariant[] All=(MonsterVariant[])Enum.GetValues(typeof(MonsterVariant));
        private static readonly string[] Ids={"original","puffy","winged","speedy","dazed","spiky","horned","mini","droplet","puddle","pill","cube","cloud","sprout","twin","ribbon"};
        private static readonly string[] Names={"기본 상몬","빵빵 상몬","날개 상몬","쌩쌩 상몬","멍한 상몬","삐죽 상몬","뿔 상몬","꼬마 상몬","물방울 상몬","납작 상몬","길쭉 상몬","네모 상몬","구름 상몬","새싹 상몬","쌍둥이 상몬","꼬불 상몬"};
        public static string Id(MonsterVariant variant) { return Ids[(int)variant]; }
        public static string Name(MonsterVariant variant) { return Names[(int)variant]; }
        public static float SpeedFactor(MonsterVariant variant)
        {
            switch(variant) {
                case MonsterVariant.Speedy: return 1.6f;
                case MonsterVariant.Puffy: return .8f;
                case MonsterVariant.Winged: return 1.15f;
                case MonsterVariant.Puddle: return .65f;
                case MonsterVariant.Ribbon: return .85f;
                case MonsterVariant.Cube: return .9f;
                default: return 1;
            }
        }
        public static MonsterVariant FromId(string id)
        {
            int index=Array.IndexOf(Ids,id); return index<0?MonsterVariant.Original:(MonsterVariant)index;
        }
    }
}
