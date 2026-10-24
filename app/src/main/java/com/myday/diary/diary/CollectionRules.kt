package com.myday.diary.diary

import com.myday.diary.data.*
import java.security.SecureRandom
import java.util.UUID

data class CollectionState(val owned: Set<String>,val draws: Int,val petals: Int,val coins: Int,val closet: Boolean=false,
    val materials: List<Int> = listOf(0,0,0,0),val milestones: Set<Int> = emptySet(),val syntheses: Int=0,val synthesisSuccesses: Int=0,val crafts: Int=0)

object CollectionRules {
    const val DRAW_COST=20;const val EXCHANGE_COST=15;const val DUPLICATE_PETALS=3;const val MAX_ACTIONS=50301
    val starters=EquipmentCatalog.starters.toSet()
    private const val CLOSET_ID="00000000000000000000000000000007"
    private fun economic(a: CollectionAction)=a.kind!="closet" && a.kind!="legacy"
    fun needsV8(kind: String)=kind in setOf("draw-tiered","exchange-tiered","synth","craft")
    fun rarity(id: String): Int {require(id in EquipmentCatalog.items);if(id in starters)return 0;return SharedCollection.forms.indexOfFirst {id.substringAfter('-') in it}.also {require(it>=0)}}
    fun rarityName(id: String)=SharedCollection.names[rarity(id)]
    fun exchangePrice(id: String)=SharedCollection.exchangePetals[rarity(id)]
    fun oddsText()=(0..3).joinToString(" / ") {SharedCollection.names[it]+" "+(SharedCollection.weights[it]/100.0).toString().removeSuffix(".0")+"%"}
    fun canUse(book: DiaryBook,id: String)=id=="none" || id in starters || id in state(book).owned
    private fun ensureCloset(current: CollectionBook?): CollectionBook {
        val actions=current?.actions ?: emptyList()
        return if(actions.any {it.kind=="closet"})CollectionBook(actions.toList()) else CollectionBook(actions+CollectionAction(CLOSET_ID,"closet",""))
    }
    // Replay v6 events before adding ownership so old duplicates and pity stay unchanged.
    fun migrate(book: DiaryBook)=if(book.version>=7)book else book.copy(collection=preserveLegacy(book.collection,book.equipment,book),version=7)
    fun preserveLegacy(current: CollectionBook?,gear: Equipment,book: DiaryBook): CollectionBook {
        var result=ensureCloset(current);val owned=state(book.copy(collection=result)).owned.toMutableSet()
        EquipmentCatalog.slots.forEach {slot->val id=gear.get(slot);if(id!="none" && owned.add(id))result=CollectionBook(result.actions+CollectionAction(UUID.randomUUID().toString().replace("-",""),"legacy",id))}
        state(book.copy(collection=result));return result
    }
    private val random=SecureRandom()
    fun earned(book: DiaryBook): Int {val xp=NotebookRules.xp(book);return 40+xp+(NotebookRules.currencyLevelFromXp(xp)-1)*5+NotebookRules.attendanceWeeks(book)*10}
    fun state(book: DiaryBook): CollectionState {
        var coins=earned(book);var petals=0;var draws=0;var closet=false;val owned=mutableSetOf<String>();val ids=mutableSetOf<String>()
        val materials=IntArray(4);val milestones=mutableSetOf<Int>();var syntheses=0;var successes=0;var crafts=0
        fun awardMilestones() {SharedCollection.milestones.forEach {m->if(owned.size>=m[0] && milestones.add(m[0]))materials[m[1]]+=m[2]}}
        val actions=book.collection?.actions ?: emptyList();require(actions.size<=MAX_ACTIONS)
        actions.forEach {a->
            require(a.id.matches(Regex("[0-9a-fA-F]{32}")) && ids.add(a.id.lowercase())) {"수집 내역의 ID나 소품이 올바르지 않아요."}
            if(a.kind=="closet") {
                require(!closet && a.id==CLOSET_ID && a.itemId.isEmpty()) {"기본 소품 지급 기록이 올바르지 않아요."}
                closet=true;owned+=starters;awardMilestones();return@forEach
            }
            if(a.kind=="synth") {
                val parts=a.itemId.split(':');val grade=parts.firstOrNull()?.toIntOrNull()
                require(closet && parts.size==2 && grade!=null && grade in 0..2 && parts[0]==grade.toString() && parts[1] in setOf("up","stay") && materials[grade]>=SharedCollection.synthesisCost) {"합성 재료나 결과 기록이 올바르지 않아요."}
                materials[grade]-=SharedCollection.synthesisCost;syntheses++
                if(parts[1]=="up"){materials[grade+1]++;successes++}else materials[grade]+=SharedCollection.failureRefund
                return@forEach
            }
            require(a.itemId in EquipmentCatalog.items) {"알 수 없는 소품이에요."}
            val duplicate=a.itemId in owned
            if(a.kind=="legacy") {require(closet && !duplicate);owned+=a.itemId;awardMilestones();return@forEach}
            val grade=rarity(a.itemId);require(!needsV8(a.kind) || closet)
            when(a.kind) {
                "draw","draw-tiered"->{require(!closet || a.itemId !in starters);require(coins>=DRAW_COST) {"클로버가 부족한 수집 기록이에요."};draws++
                    require(draws%10!=0 || owned.size==EquipmentCatalog.items.size || !duplicate) {"10회 새 소품 보장 기록이 올바르지 않아요."}
                    coins-=DRAW_COST;if(a.kind=="draw-tiered")materials[grade]++
                    if(duplicate)petals+=if(a.kind=="draw-tiered")SharedCollection.duplicatePetals[grade] else DUPLICATE_PETALS}
                "exchange","exchange-tiered"->{val cost=if(a.kind=="exchange-tiered")exchangePrice(a.itemId) else EXCHANGE_COST;require(!duplicate && petals>=cost) {"꽃잎 교환 기록이 올바르지 않아요."};petals-=cost}
                "craft"->{require(!duplicate && materials[grade]>=SharedCollection.craftCost) {"같은 등급 조각 5개와 미수집 소품이 필요해요."};materials[grade]-=SharedCollection.craftCost;crafts++}
                else->error("알 수 없는 수집 기록이에요.")
            };owned+=a.itemId;awardMilestones()
        }
        return CollectionState(owned,draws,petals,coins,closet,materials.toList(),milestones,syntheses,successes,crafts)
    }
    fun draw(book: DiaryBook,choose: (Int)->Int={random.nextInt(it)}): DiaryBook {
        val prepared=book.copy(collection=ensureCloset(book.collection),version=7)
        val state=state(prepared);require(state.coins>=DRAW_COST) {"클로버 20개가 필요해요. 출석하거나 일기를 기록해 보세요."}
        val pool=EquipmentCatalog.items.keys.filter {it !in starters}.filter {(state.draws+1)%10!=0 || state.owned.size==EquipmentCatalog.items.size || it !in state.owned}
        val weights=(0..3).map {grade->if(pool.any {rarity(it)==grade})SharedCollection.weights[grade] else 0}
        var roll=choose(weights.sum());require(roll in 0 until weights.sum());var grade=0
        while(roll>=weights[grade]){roll-=weights[grade];grade++}
        val selected=pool.filter {rarity(it)==grade};val index=choose(selected.size);require(index in selected.indices)
        return append(prepared,"draw-tiered",selected[index])
    }
    fun exchange(book: DiaryBook,itemId: String)=append(book.copy(collection=ensureCloset(book.collection),version=maxOf(7,book.version)),"exchange-tiered",itemId)
    fun craft(book: DiaryBook,itemId: String)=append(book.copy(collection=ensureCloset(book.collection),version=maxOf(7,book.version)),"craft",itemId)
    fun synthesize(book: DiaryBook,grade: Int,choose: (Int)->Int={random.nextInt(it)}): DiaryBook {
        require(grade in 0..2);require(state(book).materials[grade]>=SharedCollection.synthesisCost) {"같은 등급 조각 3개가 필요해요."}
        val roll=choose(100);require(roll in 0..99)
        return append(book.copy(collection=ensureCloset(book.collection)),"synth","$grade:"+if(roll<SharedCollection.synthesisPercent[grade])"up" else "stay")
    }
    private fun append(book: DiaryBook,kind: String,itemId: String): DiaryBook {
        val next=book.copy(collection=CollectionBook((book.collection?.actions ?: emptyList())+CollectionAction(UUID.randomUUID().toString().replace("-",""),kind,itemId)),version=if(needsV8(kind))8 else maxOf(7,book.version))
        state(next);return next
    }
    fun merge(a: CollectionBook?,b: CollectionBook?): CollectionBook? {
        val allLeft=a?.actions ?: emptyList();val allRight=b?.actions ?: emptyList()
        val left=allLeft.filter(::economic);val right=allRight.filter(::economic)
        require(left.take(minOf(left.size,right.size))==right.take(minOf(left.size,right.size))) {"두 기기에서 따로 뽑은 내역은 합칠 수 없어요. 같은 수집 기록에서 이어서 사용해 주세요."}
        if(a==null && b==null)return null
        var result=CollectionBook(if(left.size>=right.size)allLeft.toList() else allRight.toList())
        if((allLeft+allRight).any {it.kind=="closet"}) {
            result=ensureCloset(result);val owned=(starters+result.actions.filter {it.kind!="closet"}.map {it.itemId}).toMutableSet()
            (allLeft+allRight).filter {it.kind=="legacy"}.forEach {grant->if(owned.add(grant.itemId))result=CollectionBook(result.actions+CollectionAction(UUID.randomUUID().toString().replace("-",""),"legacy",grant.itemId))}
        }
        return result
    }
}
