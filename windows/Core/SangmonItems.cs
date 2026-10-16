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
        private static readonly SangmonItem[] Originals={
            new SangmonItem("solar-sword","태양 성검","weapon",ItemElement.Solar,"황금 가드 · 빛의 칼날"),
            new SangmonItem("ember-sword","화염 대검","weapon",ItemElement.Ember,"용의 날개 · 붉은 균열"),
            new SangmonItem("frost-sword","빙결 장검","weapon",ItemElement.Frost,"얼음 결정 · 푸른 광채"),
            new SangmonItem("storm-sword","번개 쌍날검","weapon",ItemElement.Storm,"갈라진 칼날 · 전격 문양"),
            new SangmonItem("shadow-sword","그림자 곡검","weapon",ItemElement.Shadow,"가시 장식 · 보랏빛 잔광"),
            new SangmonItem("astral-sword","별빛 룬검","weapon",ItemElement.Astral,"별의 파편 · 마법 룬"),
            new SangmonItem("solar-orb","태양의 성환","charm",ItemElement.Solar,"황금 고리 안에 담은 빛"),
            new SangmonItem("ember-orb","화염 심장","charm",ItemElement.Ember,"작은 불꽃을 품은 보주"),
            new SangmonItem("frost-orb","빙결 수정","charm",ItemElement.Frost,"눈송이 형태의 얼음 결정"),
            new SangmonItem("storm-orb","번개 핵","charm",ItemElement.Storm,"전류가 흐르는 마법 핵"),
            new SangmonItem("shadow-orb","그림자 보주","charm",ItemElement.Shadow,"가시 고리와 밤의 구슬"),
            new SangmonItem("astral-orb","별빛 구슬","charm",ItemElement.Astral,"작은 별과 궤도를 담은 구슬")
        };
        public static readonly string[] Slots={"weapon","charm","head","face","neck","back","aura","ground"};
        public static readonly string[] SlotNames={"무기","보주·소품","머리","얼굴","목","등","오라","발밑"};
        // Add forms here to extend the wardrobe; no renderer/UI cap at 300.
        public static readonly string[] Forms={
            "sword|weapon|장검","dagger|weapon|단검","axe|weapon|전투 도끼","hammer|weapon|망치","staff|weapon|지팡이","spear|weapon|창",
            "bow|weapon|활","wand|weapon|요술봉","scythe|weapon|낫","book|weapon|마도서","fan|weapon|부채","umbrella|weapon|우산",
            "orb|charm|보주","crystal|charm|수정 조각","lantern|charm|랜턴","compass|charm|나침반","bell|charm|방울","bottle|charm|물약병",
            "crown|head|왕관","hood|head|후드","wizardhat|head|마법 모자","helmet|head|투구","beret|head|베레모","flower|head|꽃 장식","halo|head|빛 고리","antlers|head|사슴뿔",
            "glasses|face|안경","monocle|face|외눈 안경","mask|face|가면","visor|face|고글","starpatch|face|별 안대","cheeks|face|볼 스티커",
            "scarf|neck|목도리","bowtie|neck|나비넥타이","tie|neck|넥타이","pendant|neck|펜던트","collar|neck|칼라","headphones|neck|헤드폰",
            "wings|back|날개","cape|back|망토","backpack|back|가방","rocket|back|로켓","leaf|back|잎사귀","banner|back|깃발",
            "ring|aura|마법진","runes|aura|룬 문자","stars|aura|별무리","bubbles|aura|거품",
            "cloud|ground|구름 발판","board|ground|호버 보드"
        };
        public static readonly SangmonItem[] All=Create();
        private static readonly Dictionary<string,SangmonItem> Index=All.ToDictionary(i=>i.Id,StringComparer.Ordinal);
        private static SangmonItem[] Create()
        {
            var result=new List<SangmonItem>(Originals);
            string[] prefixes={"태양","화염","빙결","번개","그림자","별빛"};
            foreach(string form in Forms) {
                string[] parts=form.Split('|');
                for(int e=0;e<6;e++) {
                    string id=((ItemElement)e).ToString().ToLowerInvariant()+"-"+parts[0];
                    if(result.Any(i=>i.Id==id))continue;
                    result.Add(new SangmonItem(id,prefixes[e]+" "+parts[2],parts[1],(ItemElement)e,prefixes[e]+" 재질 · "+SlotName(parts[1])+" · 자유 조합"));
                }
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
        public static bool Expanded(SangmonEquipment e) {return e!=null && Slots.Any(s=>Get(e,s)!="none" && !Originals.Any(i=>i.Id==Get(e,s)));}
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
