package com.myday.diary.diary

import com.myday.diary.data.*
import java.security.SecureRandom
import java.util.UUID

data class CollectionState(val owned: Set<String>,val draws: Int,val petals: Int,val coins: Int)

object CollectionRules {
    const val DRAW_COST=20;const val EXCHANGE_COST=15;const val DUPLICATE_PETALS=3;const val MAX_ACTIONS=50000
    private val random=SecureRandom()
    fun earned(book: DiaryBook)=40+NotebookRules.xp(book)+(NotebookRules.level(book)-1)*5+NotebookRules.attendanceWeeks(book)*10
    fun state(book: DiaryBook): CollectionState {
        var coins=earned(book);var petals=0;var draws=0;val owned=mutableSetOf<String>();val ids=mutableSetOf<String>()
        val actions=book.collection?.actions ?: emptyList();require(actions.size<=MAX_ACTIONS)
        actions.forEach {a->
            require(a.id.matches(Regex("[0-9a-fA-F]{32}")) && ids.add(a.id.lowercase()) && a.itemId in EquipmentCatalog.items) {"수집 내역의 ID나 소품이 올바르지 않아요."}
            val duplicate=a.itemId in owned
            when(a.kind) {
                "draw"->{require(coins>=DRAW_COST) {"클로버가 부족한 수집 기록이에요."};draws++
                    require(draws%10!=0 || owned.size==EquipmentCatalog.items.size || !duplicate) {"10회 새 소품 보장 기록이 올바르지 않아요."}
                    coins-=DRAW_COST;if(duplicate)petals+=DUPLICATE_PETALS}
                "exchange"->{require(!duplicate && petals>=EXCHANGE_COST) {"꽃잎 교환 기록이 올바르지 않아요."};petals-=EXCHANGE_COST}
                else->error("알 수 없는 수집 기록이에요.")
            };owned+=a.itemId
        }
        return CollectionState(owned,draws,petals,coins)
    }
    fun draw(book: DiaryBook,choose: (Int)->Int={random.nextInt(it)}): DiaryBook {
        val state=state(book);require(state.coins>=DRAW_COST) {"클로버 20개가 필요해요. 출석하거나 일기를 기록해 보세요."}
        val pool=EquipmentCatalog.items.keys.filter {(state.draws+1)%10!=0 || state.owned.size==EquipmentCatalog.items.size || it !in state.owned}
        val index=choose(pool.size);require(index in pool.indices)
        return append(book,"draw",pool[index])
    }
    fun exchange(book: DiaryBook,itemId: String)=append(book,"exchange",itemId)
    private fun append(book: DiaryBook,kind: String,itemId: String): DiaryBook {
        val next=book.copy(collection=CollectionBook((book.collection?.actions ?: emptyList())+CollectionAction(UUID.randomUUID().toString().replace("-",""),kind,itemId)),version=6)
        state(next);return next
    }
    fun merge(a: CollectionBook?,b: CollectionBook?): CollectionBook? {
        val left=a?.actions ?: emptyList();val right=b?.actions ?: emptyList()
        require(left.take(minOf(left.size,right.size))==right.take(minOf(left.size,right.size))) {"두 기기에서 따로 뽑은 내역은 합칠 수 없어요. 현재 기록을 백업하고 같은 수집 기록에서 이어서 사용해 주세요."}
        return if(a==null && b==null)null else CollectionBook(if(left.size>=right.size)left.toList() else right.toList())
    }
}
