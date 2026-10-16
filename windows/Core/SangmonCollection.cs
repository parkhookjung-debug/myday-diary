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
    }
    public static class SangmonCollection
    {
        public const int DrawCost=20,ExchangeCost=15,DuplicatePetals=3,MaxActions=50000;
        public static int Earned(SangmonProgress progress) {return 40+SangmonGrowth.XP(progress)+(SangmonGrowth.Level(progress)-1)*5+SangmonGrowth.AttendanceWeeks(progress)*10;}
        // Replay the ledger instead of trusting a mutable balance in the backup.
        public static CollectionState State(CollectionBook collection,SangmonProgress progress)
        {
            var result=new CollectionState {Coins=Earned(progress)};
            if(collection==null)return result;
            if(collection.Actions==null || collection.Actions.Count>MaxActions)throw new InvalidDataException("수집 기록이 너무 크거나 올바르지 않아요.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var action in collection.Actions) {
                Guid token;
                if(action==null || action.Id==null || action.Id.Length!=32 || !Guid.TryParseExact(action.Id,"N",out token) || !ids.Add(action.Id.ToLowerInvariant()) || SangmonItems.Find(action.ItemId)==null)
                    throw new InvalidDataException("수집 내역의 ID나 소품이 올바르지 않아요.");
                bool duplicate=result.Owned.Contains(action.ItemId);
                if(action.Kind=="draw") {
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
            var state=State(current,progress);
            if(state.Coins<DrawCost)throw new InvalidOperationException("클로버 20개가 필요해요. 출석하거나 일기를 기록해 보세요.");
            var pool=SangmonItems.All.Where(i=>(state.Draws+1)%10!=0 || state.Owned.Count==SangmonItems.All.Length || !state.Owned.Contains(i.Id)).ToArray();
            int index=(choose??RandomIndex)(pool.Length);
            if(index<0 || index>=pool.Length)throw new ArgumentOutOfRangeException("choose");
            return Append(current,progress,"draw",pool[index].Id);
        }
        public static CollectionBook Exchange(CollectionBook current,SangmonProgress progress,string itemId) {return Append(current,progress,"exchange",itemId);}
        private static CollectionBook Append(CollectionBook current,SangmonProgress progress,string kind,string itemId)
        {
            var result=new CollectionBook {Actions=current==null?new List<CollectionAction>():current.Actions.ToList()};
            result.Actions.Add(new CollectionAction {Id=Guid.NewGuid().ToString("N"),Kind=kind,ItemId=itemId});
            Validate(result,progress);return result;
        }
        public static CollectionBook Merge(CollectionBook current,CollectionBook incoming)
        {
            var a=current==null?new List<CollectionAction>():current.Actions;var b=incoming==null?new List<CollectionAction>():incoming.Actions;
            for(int i=0;i<Math.Min(a.Count,b.Count);i++)if(a[i].Id!=b[i].Id || a[i].Kind!=b[i].Kind || a[i].ItemId!=b[i].ItemId)
                throw new InvalidDataException("두 기기에서 따로 뽑은 내역은 합칠 수 없어요. 현재 기록을 백업하고 같은 수집 기록에서 이어서 사용해 주세요.");
            return current==null && incoming==null?null:new CollectionBook {Actions=(a.Count>=b.Count?a:b).Select(x=>new CollectionAction {Id=x.Id,Kind=x.Kind,ItemId=x.ItemId}).ToList()};
        }
    }
}
