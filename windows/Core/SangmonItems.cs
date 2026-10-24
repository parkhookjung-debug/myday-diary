using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MyDay.Windows.Core
{
    public enum ItemElement { Solar, Ember, Frost, Storm, Shadow, Astral }
    public sealed class SangmonItem
    {
        public readonly string Id,Name,Slot,Description,Form;
        public readonly ItemElement Element;
        public SangmonItem(string id,string name,string slot,ItemElement element,string description)
        { Id=id; Name=name; Slot=slot; Element=element; Description=description; Form=id.Substring(id.IndexOf('-')+1); }
    }
    [DataContract]
    public sealed class SangmonEquipment
    {
        [DataMember] public string WeaponId="none";
        [DataMember] public string CharmId="none";
        [DataMember(EmitDefaultValue=false)] public string HeadId="none";
        [DataMember(EmitDefaultValue=false)] public string FaceId="none";
        [DataMember(EmitDefaultValue=false)] public string NeckId="none";
        [DataMember(EmitDefaultValue=false)] public string BackId="none";
        [DataMember(EmitDefaultValue=false)] public string AuraId="none";
        [DataMember(EmitDefaultValue=false)] public string GroundId="none";
    }
    public static class SangmonItems
    {
        // Stable legacy keys: appearances and display names may change without losing a save.
        private static readonly string[] LegacyIds={"solar-sword","ember-sword","frost-sword","storm-sword","shadow-sword","astral-sword",
            "solar-orb","ember-orb","frost-orb","storm-orb","shadow-orb","astral-orb"};
        public static readonly string[] Slots={"weapon","charm","head","face","neck","back","aura","ground"};
        public static readonly string[] SlotNames={"손 소품","작은 소품","머리","얼굴","목","등","주변 장식","발밑"};
        public static readonly string[] PaletteNames={"크림","살구","하늘","민트","라벤더","딸기"};
        // Keys remain stable for backups. The third column is the current cozy appearance.
        public static readonly string[] Forms={
            "sword|weapon|색연필","dagger|weapon|크레용","axe|weapon|꽃 한 송이","hammer|weapon|미니 카메라","staff|weapon|풍선","spear|weapon|막대 사탕",
            "bow|weapon|머그컵","wand|weapon|비눗방울 막대","scythe|weapon|초승달 쿠션","book|weapon|작은 노트","fan|weapon|조개 부채","umbrella|weapon|도트 우산",
            "orb|charm|동그란 참","crystal|charm|편지 봉투","lantern|charm|반딧불 병","compass|charm|작은 시계","bell|charm|고양이 방울","bottle|charm|우유병",
            "crown|head|리본 머리띠","hood|head|보닛","wizardhat|head|니트 비니","helmet|head|버킷햇","beret|head|베레모","flower|head|데이지 핀","halo|head|도넛 머리띠","antlers|head|토끼 귀",
            "glasses|face|동그란 안경","monocle|face|하트 안경","mask|face|수면 안대","visor|face|도트 선글라스","starpatch|face|별 스티커","cheeks|face|볼터치",
            "scarf|neck|니트 목도리","bowtie|neck|리본","tie|neck|삼각 스카프","pendant|neck|하트 목걸이","collar|neck|레이스 칼라","headphones|neck|폭신 헤드폰",
            "wings|back|나비 날개","cape|back|체크 담요","backpack|back|작은 배낭","rocket|back|곰 인형","leaf|back|잎사귀 가방","banner|back|미니 크로스백",
            "ring|aura|작은 하트","runes|aura|꽃잎 바람","stars|aura|반짝 별","bubbles|aura|동글 비눗방울",
            "cloud|ground|폭신 구름","board|ground|체크 쿠션"
        };
        public static readonly SangmonItem[] All=Create();
        private static readonly Dictionary<string,SangmonItem> Index=All.ToDictionary(i=>i.Id,StringComparer.Ordinal);
        private static SangmonItem[] Create()
        {
            var result=new List<SangmonItem>();
            var ids=LegacyIds.Concat(Forms.SelectMany(f=>Enumerable.Range(0,6).Select(e=>((ItemElement)e).ToString().ToLowerInvariant()+"-"+f.Split('|')[0])));
            foreach(string id in ids.Distinct(StringComparer.Ordinal)) {
                int dash=id.IndexOf('-');string key=id.Substring(dash+1);
                var parts=Forms.First(f=>f.Split('|')[0]==key).Split('|');
                var element=(ItemElement)Enum.Parse(typeof(ItemElement),id.Substring(0,dash),true);
                string color=PaletteNames[(int)element];
                result.Add(new SangmonItem(id,color+" "+parts[2],parts[1],element,color+"빛 · "+SlotName(parts[1])+" · 포근한 작은 소품"));
            }
            return result.ToArray();
        }
        public static string SlotName(string slot) {int i=Array.IndexOf(Slots,slot);return i<0?slot:SlotNames[i];}
        public static SangmonItem Find(string id) {SangmonItem item;return id!=null && Index.TryGetValue(id,out item)?item:null;}
        public static string Name(string id) { var item=Find(id); return item==null?"없음":item.Name; }
        public static void Validate(SangmonEquipment equipment)
        {
            if(equipment==null) return;
            foreach(string slot in Slots) {var item=Find(Get(equipment,slot));Set(equipment,slot,item!=null && item.Slot==slot?item.Id:"none");}
        }
        public static string Get(SangmonEquipment equipment,string slot)
        {
            if(equipment==null)return "none";
            string id;
            switch(slot) {case "weapon":id=equipment.WeaponId;break;case "charm":id=equipment.CharmId;break;case "head":id=equipment.HeadId;break;case "face":id=equipment.FaceId;break;case "neck":id=equipment.NeckId;break;case "back":id=equipment.BackId;break;case "aura":id=equipment.AuraId;break;case "ground":id=equipment.GroundId;break;default:throw new ArgumentException("알 수 없는 장비 자리입니다.");}
            return string.IsNullOrEmpty(id)?"none":id;
        }
        private static void Set(SangmonEquipment e,string slot,string id)
        {
            switch(slot) {case "weapon":e.WeaponId=id;break;case "charm":e.CharmId=id;break;case "head":e.HeadId=id;break;case "face":e.FaceId=id;break;case "neck":e.NeckId=id;break;case "back":e.BackId=id;break;case "aura":e.AuraId=id;break;case "ground":e.GroundId=id;break;}
        }
        public static bool Expanded(SangmonEquipment e) {return e!=null && Slots.Any(s=>Get(e,s)!="none" && !LegacyIds.Contains(Get(e,s)));}
        public static SangmonEquipment Copy(SangmonEquipment equipment)
        { if(equipment==null)return null;var result=new SangmonEquipment();foreach(string slot in Slots)Set(result,slot,Get(equipment,slot));return result; }
        public static SangmonEquipment Select(SangmonEquipment previous,string slot,string id)
        {
            if(!Slots.Contains(slot)) throw new ArgumentException("알 수 없는 장비 자리입니다.");
            var item=Find(id);
            if(id!="none" && (item==null || item.Slot!=slot)) throw new ArgumentException("이 자리에 장착할 수 없는 아이템입니다.");
            var result=Copy(previous)??new SangmonEquipment();
            Set(result,slot,id);
            return result;
        }
        public static SangmonEquipment Import(SangmonEquipment current,SangmonEquipment incoming,bool expanded=true)
        {
            var result=Copy(incoming??current);
            if(result!=null && incoming!=null && !expanded)foreach(string slot in Slots.Skip(2))Set(result,slot,Get(current,slot));
            return result;
        }
    }
}
