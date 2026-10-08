using System;

namespace MyDay.Windows.Character
{
    public enum MonsterVariant {
        Original, Puffy, Winged, Speedy, Dazed, Spiky, Horned, Mini,
        Fin, Shell, Tailed, Crystal, Furry, Sprout, Flower, Ribbon,
        Angel, Devil, Crown, Wizard, Pirate, Astronaut, Headphones, Glasses,
        Scarf, Backpack, SleepCap, Raincoat, Winter, Star, Moon, Heart,
        Cat, Rabbit, Fox, Puppy, Bear, Ram, Deer, Dragon,
        Butterfly, Axolotl, Shark, Peacock, Turtle, Octopus, Robot, Cactus,
        Ember, Frost, Volt, Venom, Shadow, Golem, Ocean, Astral
    }
    public static class MonsterVariants
    {
        public static readonly MonsterVariant[] All=(MonsterVariant[])Enum.GetValues(typeof(MonsterVariant));
        private static readonly string[] Ids={
            "original","puffy","winged","speedy","dazed","spiky","horned","mini",
            "fin","shell","tailed","crystal","furry","sprout","flower","ribbon",
            "angel","devil","crown","wizard","pirate","astronaut","headphones","glasses",
            "scarf","backpack","sleep-cap","raincoat","winter","star","moon","heart",
            "cat","rabbit","fox","puppy","bear","ram","deer","dragon",
            "butterfly","axolotl","shark","peacock","turtle","octopus","robot","cactus",
            "ember","frost","volt","venom","shadow","golem","ocean","astral"
        };
        private static readonly string[] Names={
            "기본 상몬","빵빵 상몬","날개 상몬","쌩쌩 상몬","멍한 상몬","삐죽 상몬","뿔 상몬","꼬마 상몬",
            "지느러미 상몬","등껍질 상몬","꼬리 상몬","수정 상몬","복슬 상몬","새싹 상몬","꽃 상몬","리본 상몬",
            "천사 상몬","악마 상몬","왕관 상몬","마법사 상몬","해적 상몬","우주 상몬","헤드폰 상몬","안경 상몬",
            "목도리 상몬","배낭 상몬","잠옷 상몬","우비 상몬","겨울 상몬","별 상몬","달 상몬","하트 상몬",
            "고양이 상몬","토끼 상몬","여우 상몬","강아지 상몬","곰 상몬","양 상몬","사슴 상몬","용 상몬",
            "나비 상몬","아홀로틀 상몬","상어 상몬","공작 상몬","거북 상몬","문어 상몬","로봇 상몬","선인장 상몬",
            "화염 상몬","빙결 상몬","전격 상몬","맹독 상몬","그림자 상몬","암석 상몬","해류 상몬","비전 상몬"
        };
        public static string Id(MonsterVariant variant) { return Ids[(int)variant]; }
        public static string Name(MonsterVariant variant) { return Names[(int)variant]; }
        public static float SpeedFactor(MonsterVariant variant)
        {
            switch(variant) {
                case MonsterVariant.Speedy: return 1.6f;
                case MonsterVariant.Puffy: return .8f;
                case MonsterVariant.Winged: return 1.15f;
                case MonsterVariant.Shell: return .8f;
                case MonsterVariant.Backpack: return .85f;
                case MonsterVariant.Astronaut: return .8f;
                case MonsterVariant.Angel: return 1.1f;
                default: return 1;
            }
        }
        public static MonsterVariant FromId(string id)
        {
            // Read the brief slime-shaped release as corrected Sangmon appearances.
            switch(id) {
                case "droplet": return MonsterVariant.Fin;
                case "puddle": return MonsterVariant.Shell;
                case "pill": return MonsterVariant.Tailed;
                case "cube": return MonsterVariant.Crystal;
                case "cloud": return MonsterVariant.Furry;
                case "twin": return MonsterVariant.Flower;
            }
            int index=Array.IndexOf(Ids,id); return index<0?MonsterVariant.Original:(MonsterVariant)index;
        }
    }
}
