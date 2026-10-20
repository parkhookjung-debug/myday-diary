package com.myday.diary.diary

import com.myday.diary.data.*
import org.junit.Assert.*
import org.junit.Test
import java.time.LocalDate
import java.io.File

class AtelierTest {
    private fun rich()=DiaryBook(awardedDays=(0 until 1000).map {LocalDate.of(2020,1,1).plusDays(it.toLong()).toString()}.toSet())
    private fun draws(g: Int,n: Int): DiaryBook {var b=rich();val ticket=SharedCollection.weights.take(g).sum();repeat(n){b=CollectionRules.draw(b){size->if(size>=9900)minOf(ticket,size-1)else 0}};return b}
    @Test fun exactWeightedBoundariesAndStableRarities() {
        assertTrue(EquipmentCatalog.items.keys.all {CollectionRules.rarity(it) in 0..3});assertTrue(CollectionRules.starters.all {CollectionRules.rarity(it)==0})
        assertEquals(listOf(0,0,1,1,2,2,3,3),listOf(0,6499,6500,8999,9000,9899,9900,9999).map {t->CollectionRules.rarity(CollectionRules.draw(rich()){if(it==10000)t else 0}.collection!!.actions.last().itemId)})
        val source=draws(0,9);val grants=EquipmentCatalog.items.keys.filter {CollectionRules.rarity(it)==3}.mapIndexed {i,id->CollectionAction("%032x".format(1000+i),"legacy",id)}
        val noTop=source.copy(collection=CollectionBook(source.collection!!.actions+grants));val owned=CollectionRules.state(noTop).owned
        val selected=CollectionRules.draw(noTop){it-1}.collection!!.actions.last().itemId
        assertEquals(2,CollectionRules.rarity(selected));assertFalse(selected in owned)
    }
    @Test fun allThreeSynthesisBoundariesKeepItemsAndRefundOneOnFailure() {
        for(g in 0..3) {
            val book=draws(g,6);val s=CollectionRules.state(book);assertEquals(6,s.materials[g]);assertEquals(5*SharedCollection.duplicatePetals[g],s.petals)
            if(g==3)continue
            val up=CollectionRules.state(CollectionRules.synthesize(book,g){SharedCollection.synthesisPercent[g]-1})
            val stay=CollectionRules.state(CollectionRules.synthesize(book,g){SharedCollection.synthesisPercent[g]})
            assertEquals(3,up.materials[g]);assertEquals(1,up.materials[g+1]);assertEquals(4,stay.materials[g]);assertEquals(0,stay.materials[g+1]);assertEquals(1,stay.syntheses)
            assertEquals(s.owned,up.owned);assertEquals(s.owned,stay.owned);assertEquals(s.coins,up.coins)
        }
        assertThrows(IllegalArgumentException::class.java){CollectionRules.synthesize(rich(),0){error("must reject before RNG")}}
    }
    @Test fun craftingMilestonesV8AndRepeatedMergePreserveTheLedger() {
        val book=draws(0,9);val before=CollectionRules.state(book);val target=EquipmentCatalog.items.keys.first {CollectionRules.rarity(it)==0 && it !in before.owned}
        val crafted=CollectionRules.craft(book,target);val s=CollectionRules.state(crafted)
        assertEquals(listOf(4,2,0,0),s.materials);assertEquals(1,s.crafts);assertEquals(setOf(10),s.milestones);assertTrue(target in s.owned);assertEquals(before.draws,s.draws)
        assertEquals(crafted,NotebookJson.decode(NotebookJson.encode(crafted)));assertEquals(8,crafted.version)
        assertEquals(s,CollectionRules.state(NotebookRules.merge(crafted,crafted)))
        val old=book.copy(collection=CollectionBook(book.collection!!.actions.map {if(it.kind=="draw-tiered")it.copy(kind="draw")else it}),version=7)
        assertEquals(before.coins,CollectionRules.state(old).coins);assertEquals(before.petals,CollectionRules.state(old).petals);assertEquals(listOf(0,0,0,0),CollectionRules.state(old).materials)
        assertThrows(IllegalArgumentException::class.java){CollectionRules.merge(CollectionRules.synthesize(book,0){0}.collection,CollectionRules.synthesize(book,0){99}.collection)}
        assertThrows(IllegalArgumentException::class.java){NotebookRules.validate(crafted.copy(collection=CollectionBook(crafted.collection!!.actions+CollectionAction("00000000000000000000000000000099","synth","2:up"))))}
    }
    @Test fun failedPersistenceKeepsFragmentsAndRetryCommitsOnce() {
        class Memory: NotebookRepository {var value=draws(0,6);var fail=false;override fun loadBook()=value;override fun saveBook(book: DiaryBook){if(fail)error("disk full");value=book}}
        val memory=Memory();val c=NotebookController(memory);val original=c.book;memory.fail=true
        assertFalse(c.synthesize(0));assertEquals(original,c.book);assertEquals(original,memory.value)
        memory.fail=false;assertTrue(c.synthesize(0));assertEquals(1,CollectionRules.state(c.book).syntheses);assertEquals(8,c.book.version)
    }
    @Test fun actualWindowsV8FixtureContinuesOnAndroidAndExportsForWindows() {
        val raw=javaClass.classLoader!!.getResourceAsStream("windows-v8.json")!!.bufferedReader().use {it.readText()}
        val book=NotebookJson.decode(raw);val before=CollectionRules.state(book)
        assertEquals(listOf(1,4,0,0),before.materials);assertEquals(10975,before.coins);assertEquals(24,before.petals)
        val next=CollectionRules.synthesize(book,1){0};val s=CollectionRules.state(next)
        assertEquals(listOf(1,1,1,0),s.materials);assertEquals(2,s.syntheses);assertEquals(1,s.crafts);assertEquals(setOf(10),s.milestones)
        val encoded=NotebookJson.encode(next);assertEquals(next,NotebookJson.decode(encoded))
        val output=File("build/interop/android-v8-to-windows.json");output.parentFile?.mkdirs();output.writeText(encoded)
    }
}
