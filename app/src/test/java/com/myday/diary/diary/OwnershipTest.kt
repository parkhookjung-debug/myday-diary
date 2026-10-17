package com.myday.diary.diary

import com.myday.diary.data.*
import org.junit.Assert.*
import org.junit.Test
import java.util.UUID
import java.io.File

class OwnershipTest {
    @Test fun freeStartersLockedEquipAndAcquiredEquipUseTheSameRules() {
        class Memory: NotebookRepository {var value=DiaryBook();override fun loadBook()=value;override fun saveBook(book: DiaryBook){value=book}}
        val memory=Memory();val c=NotebookController(memory)
        assertEquals(8,CollectionRules.state(c.book).owned.size);assertEquals(8,CollectionRules.starters.map {EquipmentCatalog.items.getValue(it)}.distinct().size)
        assertFalse(c.equip("weapon","solar-sword"));assertEquals("none",c.book.equipment.weaponId);assertTrue(c.equip("weapon","frost-sword"));assertTrue(c.equip("weapon","none"))
        assertTrue(c.draw());val id=c.book.collection!!.actions.last().itemId
        assertFalse(id in CollectionRules.starters);assertTrue(c.equip(EquipmentCatalog.items.getValue(id),id))
        assertEquals(c.book,NotebookJson.decode(NotebookJson.encode(c.book)))
        assertThrows(IllegalArgumentException::class.java){NotebookRules.validate(CollectionRules.migrate(DiaryBook()).copy(equipment=Equipment("solar-sword")))}
    }
    @Test fun migrationKeepsLegacyRewardsWornItemsAndOwnershipAfterUnequip() {
        val actions=(0..1).map {CollectionAction(UUID.randomUUID().toString().replace("-",""),"draw","frost-sword")}
        val old=DiaryBook(version=6,collection=CollectionBook(actions),equipment=Equipment("ember-book",headId="shadow-beret"));val before=CollectionRules.state(old)
        val migrated=CollectionRules.migrate(old);val after=CollectionRules.state(migrated)
        assertEquals(before.coins,after.coins);assertEquals(before.petals,after.petals);assertEquals(3,after.petals);assertEquals(before.draws,after.draws)
        assertTrue(CollectionRules.canUse(migrated,"ember-book"));assertTrue(CollectionRules.canUse(migrated.copy(equipment=Equipment()),"shadow-beret"))
        assertEquals(migrated,CollectionRules.migrate(migrated))
        val current=NotebookRules.merge(migrated,old);assertEquals(after,CollectionRules.state(current))
        val fresh=NotebookRules.merge(CollectionRules.migrate(DiaryBook()),old);assertEquals(after,CollectionRules.state(fresh))
    }
    @Test fun exchangeUnlocksNonStarterAndPityNeverReturnsAnAlreadyUsableItem() {
        var book=DiaryBook(awardedDays=(0..29).map {java.time.LocalDate.of(2026,1,1).plusDays(it.toLong()).toString()}.toSet())
        repeat(9){book=CollectionRules.draw(book){0}}
        val before=CollectionRules.state(book);val exchanged=EquipmentCatalog.items.keys.first {it !in before.owned}
        book=CollectionRules.exchange(book,exchanged);assertTrue(CollectionRules.canUse(book,exchanged))
        val owned=CollectionRules.state(book).owned;book=CollectionRules.draw(book){0}
        assertFalse(book.collection!!.actions.last().itemId in owned)
        assertThrows(IllegalArgumentException::class.java){CollectionRules.exchange(book,CollectionRules.starters.first())}
    }
    @Test fun v7FixtureRoundTripsToActualWindowsSerializer() {
        val raw=javaClass.classLoader!!.getResourceAsStream("windows-v7.json")!!.bufferedReader().use {it.readText()}
        val book=NotebookJson.decode(raw);val state=CollectionRules.state(book)
        assertEquals(7,book.version);assertEquals(2,state.draws);assertEquals(3,state.petals);assertEquals(90,state.coins);assertTrue(state.closet)
        assertTrue(EquipmentCatalog.slots.all {CollectionRules.canUse(book,book.equipment.get(it))})
        val entry=book.days.getValue("2026-10-08");val changed=book.copy(days=book.days+("2026-10-08" to entry.copy(blocks=entry.blocks.map {when(it.type){"text"->it.copy(text="안드로이드에서 수정한 글");"todo"->it.copy(checked=false);else->it}})))
        val encoded=NotebookJson.encode(changed);assertEquals(changed,NotebookJson.decode(encoded))
        val output=File("build/interop/android-v7-to-windows.json");output.parentFile?.mkdirs();output.writeText(encoded)
    }
}
