package com.myday.diary.data

import java.util.UUID

// Keep these serialized values stable so existing entries continue to load.
data class DiaryBlock(
    val id: String = UUID.randomUUID().toString(),
    val type: String,
    val text: String = "",
    val checked: Boolean = false
)

const val DEFAULT_CHARACTER = "sketch-monster"

data class DiaryEntry(val theme: Int = 0, val character: String = DEFAULT_CHARACTER, val blocks: List<DiaryBlock> = emptyList())

interface DiaryRepository {
    fun read(date: String): DiaryEntry
    fun write(date: String, entry: DiaryEntry): Boolean
}
