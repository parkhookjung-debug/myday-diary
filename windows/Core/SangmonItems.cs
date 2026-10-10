using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyDay.Windows.Core
{
    public enum ItemElement { Solar, Ember, Frost, Storm, Shadow, Astral }
    public sealed class SangmonItem
    {
        public readonly string Id,Name,Slot,Description;
        public readonly ItemElement Element;
        public SangmonItem(string id,string name,string slot,ItemElement element,string description)
        { Id=id; Name=name; Slot=slot; Element=element; Description=description; }
    }
    [DataContract]
    public sealed class SangmonEquipment
    {
        [DataMember] public string WeaponId="none";
        [DataMember] public string CharmId="none";
    }
    public static class SangmonItems
    {
        public static readonly SangmonItem[] All={
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
        public static SangmonItem Find(string id) { return All.FirstOrDefault(i=>i.Id==id); }
        public static string Name(string id) { var item=Find(id); return item==null?"없음":item.Name; }
        public static void Validate(SangmonEquipment equipment)
        {
            if(equipment==null) return;
            var weapon=Find(equipment.WeaponId); var charm=Find(equipment.CharmId);
            equipment.WeaponId=weapon!=null && weapon.Slot=="weapon"?weapon.Id:"none";
            equipment.CharmId=charm!=null && charm.Slot=="charm"?charm.Id:"none";
        }
        public static SangmonEquipment Copy(SangmonEquipment equipment)
        { return equipment==null?null:new SangmonEquipment {WeaponId=equipment.WeaponId,CharmId=equipment.CharmId}; }
        public static SangmonEquipment Select(SangmonEquipment previous,string slot,string id)
        {
            if(slot!="weapon" && slot!="charm") throw new ArgumentException("알 수 없는 장비 자리입니다.");
            var item=Find(id);
            if(id!="none" && (item==null || item.Slot!=slot)) throw new ArgumentException("이 자리에 장착할 수 없는 아이템입니다.");
            var result=Copy(previous)??new SangmonEquipment();
            if(slot=="weapon") result.WeaponId=id; else result.CharmId=id;
            return result;
        }
        public static SangmonEquipment Import(SangmonEquipment current,SangmonEquipment incoming) { return Copy(incoming??current); }
    }
}
