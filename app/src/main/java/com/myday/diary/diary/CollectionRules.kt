package com.myday.diary.diary

import com.myday.diary.data.*
import java.security.SecureRandom
import java.util.UUID

data class CollectionState(val owned: Set<String>,val draws: Int,val petals: Int,val coins: Int,val closet: Boolean=false)

object CollectionRules {
    const val DRAW_COST=20;const val EXCHANGE_COST=15;const val DUPLICATE_PETALS=3;const val MAX_ACTIONS=50301
    val starters=EquipmentCatalog.starters.toSet()
    private const val CLOSET_ID="00000000000000000000000000000007"
    private fun economic(a: CollectionAction)=a.kind=="draw" || a.kind=="exchange"
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
    fun earned(book: DiaryBook)=40+NotebookRules.xp(book)+(NotebookRules.level(book)-1)*5+NotebookRules.attendanceWeeks(book)*10
    fun state(book: DiaryBook): CollectionState {
        var coins=earned(book);var petals=0;var draws=0;var closet=false;val owned=mutableSetOf<String>();val ids=mutableSetOf<String>()
        val actions=book.collection?.actions ?: emptyList();require(actions.size<=MAX_ACTIONS)
        actions.forEach {a->
            require(a.id.matches(Regex("[0-9a-fA-F]{32}")) && ids.add(a.id.lowercase())) {"수집 내역의 ID나 소품이 올바르지 않아요."}
            if(a.kind=="closet") {
                require(!closet && a.id==CLOSET_ID && a.itemId.isEmpty()) {"기본 소품 지급 기록이 올바르지 않아요."}
                closet=true;owned+=starters;return@forEach
            }
            require(a.itemId in EquipmentCatalog.items) {"알 수 없는 소품이에요."}
            val duplicate=a.itemId in owned
            if(a.kind=="legacy") {require(closet && !duplicate);owned+=a.itemId;return@forEach}
            when(a.kind) {
                "draw"->{require(!closet || a.itemId !in starters);require(coins>=DRAW_COST) {"클로버가 부족한 수집 기록이에요."};draws++
                    require(draws%10!=0 || owned.size==EquipmentCatalog.items.size || !duplicate) {"10회 새 소품 보장 기록이 올바르지 않아요."}
                    coins-=DRAW_COST;if(duplicate)petals+=DUPLICATE_PETALS}
                "exchange"->{require(!duplicate && petals>=EXCHANGE_COST) {"꽃잎 교환 기록이 올바르지 않아요."};petals-=EXCHANGE_COST}
                else->error("알 수 없는 수집 기록이에요.")
            };owned+=a.itemId
        }
        return CollectionState(owned,draws,petals,coins,closet)
    }
    fun draw(book: DiaryBook,choose: (Int)->Int={random.nextInt(it)}): DiaryBook {
        val prepared=book.copy(collection=ensureCloset(book.collection),version=7)
        val state=state(prepared);require(state.coins>=DRAW_COST) {"클로버 20개가 필요해요. 출석하거나 일기를 기록해 보세요."}
        val pool=EquipmentCatalog.items.keys.filter {it !in starters}.filter {(state.draws+1)%10!=0 || state.owned.size==EquipmentCatalog.items.size || it !in state.owned}
        val index=choose(pool.size);require(index in pool.indices)
        return append(prepared,"draw",pool[index])
    }
    fun exchange(book: DiaryBook,itemId: String)=append(book.copy(collection=ensureCloset(book.collection),version=7),"exchange",itemId)
    private fun append(book: DiaryBook,kind: String,itemId: String): DiaryBook {
        val next=book.copy(collection=CollectionBook((book.collection?.actions ?: emptyList())+CollectionAction(UUID.randomUUID().toString().replace("-",""),kind,itemId)),version=7)
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
