using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace MyDay.Windows.Core
{
    [DataContract] public sealed class CollectionAction
    {
        [DataMember] public string Id;
        [DataMember] public string Kind;
        [DataMember] public string ItemId;
    }
    [DataContract] public sealed class CollectionBook
    {
        [DataMember] public List<CollectionAction> Actions=new List<CollectionAction>();
    }
    public sealed class CollectionState
    {
        public readonly HashSet<string> Owned=new HashSet<string>(StringComparer.Ordinal);
        public int Draws,Petals,Coins;
        public bool Closet;
    }
    public static class SangmonCollection
    {
        public static readonly string[] Starters={"frost-sword","astral-orb","solar-crown","frost-glasses","solar-scarf","solar-wings","solar-ring","frost-cloud"};
        private const string ClosetId="00000000000000000000000000000007";
        public static bool CanUse(DiaryBook book,string id) {return id=="none" || Starters.Contains(id) || State(book.Collection,book.Progress).Owned.Contains(id);}
        private static bool Economic(CollectionAction a) {return a.Kind=="draw" || a.Kind=="exchange";}
        // Keep v6 paid events before the ownership marker to preserve their original rewards.
        public static void Migrate(DiaryBook book) {
            if(book.Version>=7)return;
            book.Collection=PreserveLegacy(book.Collection,book.Progress,book.Equipment);book.Version=7;
        }
        public static CollectionBook PreserveLegacy(CollectionBook current,SangmonProgress progress,SangmonEquipment gear) {
            var result=EnsureCloset(current);var owned=State(result,progress).Owned;
            foreach(string slot in SangmonItems.Slots) {string id=SangmonItems.Get(gear,slot);if(id!="none" && owned.Add(id))result.Actions.Add(new CollectionAction {Id=Guid.NewGuid().ToString("N"),Kind="legacy",ItemId=id});}
            Validate(result,progress);return result;
        }
        private static CollectionBook EnsureCloset(CollectionBook current) {
            var result=new CollectionBook {Actions=current==null?new List<CollectionAction>():current.Actions.ToList()};
            if(!result.Actions.Any(a=>a.Kind=="closet"))result.Actions.Add(new CollectionAction {Id=ClosetId,Kind="closet",ItemId=""});return result;
        }
        public const int DrawCost=20,ExchangeCost=15,DuplicatePetals=3,MaxActions=50301;
        public static int Earned(SangmonProgress progress) {int xp=SangmonGrowth.XP(progress);return 40+xp+(SangmonGrowth.CurrencyLevelFromXP(xp)-1)*5+SangmonGrowth.AttendanceWeeks(progress)*10;}
        // Replay the ledger instead of trusting a mutable balance in the backup.
        public static CollectionState State(CollectionBook collection,SangmonProgress progress)
        {
            var result=new CollectionState {Coins=Earned(progress)};
            if(collection==null)return result;
            if(collection.Actions==null || collection.Actions.Count>MaxActions)throw new InvalidDataException("수집 기록이 너무 크거나 올바르지 않아요.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var action in collection.Actions) {
                Guid token;
                if(action==null || action.Id==null || action.Id.Length!=32 || !Guid.TryParseExact(action.Id,"N",out token) || !ids.Add(action.Id.ToLowerInvariant()))
                    throw new InvalidDataException("수집 내역의 ID나 소품이 올바르지 않아요.");
                if(action.Kind=="closet") {
                    if(result.Closet || action.Id!=ClosetId || action.ItemId!="")throw new InvalidDataException("기본 소품 지급 기록이 올바르지 않아요.");
                    result.Closet=true;result.Owned.UnionWith(Starters);continue;
                }
                if(SangmonItems.Find(action.ItemId)==null)throw new InvalidDataException("알 수 없는 소품이에요.");
                bool duplicate=result.Owned.Contains(action.ItemId);
                if(action.Kind=="legacy") {
                    if(!result.Closet || duplicate)throw new InvalidDataException("이전 착용 소품 기록이 올바르지 않아요.");
                    result.Owned.Add(action.ItemId);continue;
                }
                if(action.Kind=="draw") {
                    if(result.Closet && Starters.Contains(action.ItemId))throw new InvalidDataException("기본 소품은 뽑기 대상이 아니에요.");
                    if(result.Coins<DrawCost)throw new InvalidDataException("클로버가 부족한 수집 기록이에요.");
                    result.Draws++;
                    if(result.Draws%10==0 && result.Owned.Count<SangmonItems.All.Length && duplicate)throw new InvalidDataException("10회 새 소품 보장 기록이 올바르지 않아요.");
                    result.Coins-=DrawCost;if(duplicate)result.Petals+=DuplicatePetals;
                } else if(action.Kind=="exchange") {
                    if(duplicate || result.Petals<ExchangeCost)throw new InvalidDataException("꽃잎 교환 기록이 올바르지 않아요.");
                    result.Petals-=ExchangeCost;
                } else throw new InvalidDataException("알 수 없는 수집 기록이에요.");
                result.Owned.Add(action.ItemId);
            }
            return result;
        }
        public static void Validate(CollectionBook collection,SangmonProgress progress) {State(collection,progress);}
        private static int RandomIndex(int count)
        {
            using(var random=RandomNumberGenerator.Create()) {
                var bytes=new byte[4];uint value,limit=uint.MaxValue-uint.MaxValue%(uint)count;
                do {random.GetBytes(bytes);value=BitConverter.ToUInt32(bytes,0);}while(value>=limit);
                return (int)(value%(uint)count);
            }
        }
        public static CollectionBook Draw(CollectionBook current,SangmonProgress progress,Func<int,int> choose=null)
        {
            current=EnsureCloset(current);var state=State(current,progress);
            if(state.Coins<DrawCost)throw new InvalidOperationException("클로버 20개가 필요해요. 출석하거나 일기를 기록해 보세요.");
            var pool=SangmonItems.All.Where(i=>!Starters.Contains(i.Id)).Where(i=>(state.Draws+1)%10!=0 || state.Owned.Count==SangmonItems.All.Length || !state.Owned.Contains(i.Id)).ToArray();
            int index=(choose??RandomIndex)(pool.Length);
            if(index<0 || index>=pool.Length)throw new ArgumentOutOfRangeException("choose");
            return Append(current,progress,"draw",pool[index].Id);
        }
        public static CollectionBook Exchange(CollectionBook current,SangmonProgress progress,string itemId) {return Append(EnsureCloset(current),progress,"exchange",itemId);}
        private static CollectionBook Append(CollectionBook current,SangmonProgress progress,string kind,string itemId)
        {
            var result=new CollectionBook {Actions=current==null?new List<CollectionAction>():current.Actions.ToList()};
            result.Actions.Add(new CollectionAction {Id=Guid.NewGuid().ToString("N"),Kind=kind,ItemId=itemId});
            Validate(result,progress);return result;
        }
        public static CollectionBook Merge(CollectionBook current,CollectionBook incoming)
        {
            var a=current==null?new List<CollectionAction>():current.Actions;var b=incoming==null?new List<CollectionAction>():incoming.Actions;
            // Compare paid events; append missing ownership grants after the chosen history.
            var left=a.Where(Economic).ToArray();var right=b.Where(Economic).ToArray();
            for(int i=0;i<Math.Min(left.Length,right.Length);i++)if(left[i].Id!=right[i].Id || left[i].Kind!=right[i].Kind || left[i].ItemId!=right[i].ItemId)
                throw new InvalidDataException("두 기기에서 따로 뽑은 내역은 합칠 수 없어요. 같은 수집 기록에서 이어서 사용해 주세요.");
            if(current==null && incoming==null)return null;
            var chosen=left.Length>=right.Length?a:b;
            var result=new CollectionBook {Actions=chosen.Select(x=>new CollectionAction {Id=x.Id,Kind=x.Kind,ItemId=x.ItemId}).ToList()};
            if(a.Concat(b).Any(x=>x.Kind=="closet")) {
                result=EnsureCloset(result);var owned=new HashSet<string>(Starters.Concat(result.Actions.Where(x=>x.Kind!="closet").Select(x=>x.ItemId)),StringComparer.Ordinal);
                foreach(var grant in a.Concat(b).Where(x=>x.Kind=="legacy"))if(owned.Add(grant.ItemId))result.Actions.Add(new CollectionAction {Id=Guid.NewGuid().ToString("N"),Kind="legacy",ItemId=grant.ItemId});
            }
            return result;
        }
    }
}
