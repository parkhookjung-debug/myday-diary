package com.myday.diary.data

import java.util.UUID

// Keep these serialized values stable so existing entries continue to load.
data class DiaryBlock(
    val id: String = UUID.randomUUID().toString(),
    val type: String,
    val text: String = "",
    val checked: Boolean = false,
    val title: String = "",
    val prompt: String = "",
    val photo: String? = null,
    val wide: Boolean = false,
    val x: Int = 0,
    val y: Int = 0,
    val width: Int = 0,
    val height: Int = 0
)

const val DEFAULT_CHARACTER = "sketch-monster"

data class DiaryEntry(val theme: Int = 0, val character: String = DEFAULT_CHARACTER, val blocks: List<DiaryBlock> = emptyList(),
    val mood: String = "평온해요", val layoutMode: String = "cards", val pageStyle: String = "plain")

data class Equipment(val weaponId: String = "none", val charmId: String = "none",val headId: String="none",val faceId: String="none",
    val neckId: String="none",val backId: String="none",val auraId: String="none",val groundId: String="none") {
    fun get(slot: String)=when(slot) {"weapon"->weaponId;"charm"->charmId;"head"->headId;"face"->faceId;"neck"->neckId;"back"->backId;"aura"->auraId;"ground"->groundId;else->error("알 수 없는 장비 슬롯")}
    fun equip(slot: String,id: String)=when(slot) {"weapon"->copy(weaponId=id);"charm"->copy(charmId=id);"head"->copy(headId=id);"face"->copy(faceId=id);"neck"->copy(neckId=id);"back"->copy(backId=id);"aura"->copy(auraId=id);"ground"->copy(groundId=id);else->error("알 수 없는 장비 슬롯")}
}
data class SavedLayout(val id: String = UUID.randomUUID().toString(), val name: String, val mode: String,
    val style: String, val theme: Int, val blocks: List<DiaryBlock>)
data class DiaryBook(val version: Int = 4, val days: Map<String, DiaryEntry> = emptyMap(),
    val characterStyle: String = "original", val layouts: List<SavedLayout> = emptyList(),
    val awardedDays: Set<String> = emptySet(), val equipment: Equipment = Equipment(), val haptics: Boolean = true,
    val hasEquipment: Boolean = true,val activities: Map<String,Int> = emptyMap(),val collection: CollectionBook?=null,val studio: StudioData?=null)

interface NotebookRepository {
    fun loadBook(): DiaryBook
    fun saveBook(book: DiaryBook)
}

interface DiaryRepository {
    fun read(date: String): DiaryEntry
    fun write(date: String, entry: DiaryEntry): Boolean
}

// A single ordered ledger makes spending and duplicate conversion reproducible on both platforms.
data class CollectionAction(val id: String,val kind: String,val itemId: String)
data class CollectionBook(val actions: List<CollectionAction> = emptyList())
