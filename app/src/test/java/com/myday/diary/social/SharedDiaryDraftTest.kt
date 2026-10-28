package com.myday.diary.social

import com.myday.diary.data.DiaryBlock
import com.myday.diary.data.DiaryEntry
import org.junit.Assert.*
import org.junit.Test

class SharedDiaryDraftTest {
    @Test fun onlySelectedContentLeavesTheDeviceAndMoodRequiresItsOwnChoice() {
        val public=DiaryBlock(id="public",type="text",text="친구에게 보여줄 하루",title="산책")
        val secret=DiaryBlock(id="secret",type="emotion",text="공개하지 않은 감정")
        val photo=DiaryBlock(id="private-photo",type="photo",photo="private-photo-data",text="개인 사진")
        val entry=DiaryEntry(mood="속상해요",blocks=listOf(public,secret,photo),theme=2,layoutMode="free")
        val encoded=SharedDiaryDraft.select(entry,setOf("public"),"오늘의 산책").json()
        assertEquals("",encoded.getString("mood"))
        assertEquals(1,encoded.getJSONArray("blocks").length())
        assertEquals("public",encoded.getJSONArray("blocks").getJSONObject(0).getString("id"))
        assertFalse(encoded.toString().contains("공개하지 않은 감정"))
        assertFalse(encoded.toString().contains("private-photo"))
        assertFalse(encoded.has("theme"));assertFalse(encoded.has("layoutMode"))
        assertEquals("속상해요",SharedDiaryDraft.select(entry,setOf("public"),"산책",true).mood)
        assertEquals(3,entry.blocks.size)
    }
    @Test fun publishedSnapshotDoesNotChangeWhenLocalDiaryIsEdited() {
        val old=DiaryBlock(id="entry",type="text",text="처음 공개한 내용")
        val entry=DiaryEntry(blocks=listOf(old))
        val shared=SharedDiaryDraft.select(entry,setOf(old.id),"하루").json().toString()
        val edited=entry.copy(blocks=listOf(old.copy(text="기기에만 남길 수정")))
        assertTrue(shared.contains("처음 공개한 내용"));assertFalse(shared.contains(edited.blocks.first().text))
    }
    @Test(expected=IllegalArgumentException::class) fun emptySelectionCannotPublish() {
        SharedDiaryDraft.select(DiaryEntry(),emptySet(),"제목")
    }
    @Test(expected=IllegalArgumentException::class) fun unknownSelectionCannotPublish() {
        SharedDiaryDraft.select(DiaryEntry(blocks=listOf(DiaryBlock(id="ok",type="text",text="내용"))),setOf("ok","missing"),"제목")
    }
    @Test(expected=IllegalArgumentException::class) fun aPhotoSlotIsNotAnUploadedPhoto() {
        SharedDiaryDraft.select(DiaryEntry(blocks=listOf(DiaryBlock(id="slot",type="photo-slot",text="사진 예정"))),setOf("slot"),"제목")
    }
    @Test(expected=IllegalArgumentException::class) fun moreThanSixPhotosCannotPublish() {
        val blocks=(1..7).map{DiaryBlock(id="$it",type="photo",photo="image")}
        SharedDiaryDraft.select(DiaryEntry(blocks=blocks),blocks.map{it.id}.toSet(),"사진")
    }
}
