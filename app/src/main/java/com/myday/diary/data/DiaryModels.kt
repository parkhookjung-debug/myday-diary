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

data class Equipment(val weaponId: String = "none", val charmId: String = "none")
data class SavedLayout(val id: String = UUID.randomUUID().toString(), val name: String, val mode: String,
    val style: String, val theme: Int, val blocks: List<DiaryBlock>)
data class DiaryBook(val version: Int = 4, val days: Map<String, DiaryEntry> = emptyMap(),
    val characterStyle: String = "original", val layouts: List<SavedLayout> = emptyList(),
    val awardedDays: Set<String> = emptySet(), val equipment: Equipment = Equipment(), val haptics: Boolean = true,
    val hasEquipment: Boolean = true)

interface NotebookRepository {
    fun loadBook(): DiaryBook
    fun saveBook(book: DiaryBook)
}

interface DiaryRepository {
    fun read(date: String): DiaryEntry
    fun write(date: String, entry: DiaryEntry): Boolean
}
