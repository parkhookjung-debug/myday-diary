package com.myday.diary

import android.content.Context
import android.content.ContextWrapper
import android.content.SharedPreferences
import android.graphics.*
import android.media.ExifInterface
import android.net.Uri
import android.util.Base64
import androidx.test.platform.app.InstrumentationRegistry
import com.myday.diary.data.*
import org.junit.*
import org.junit.Assert.*
import java.io.File
import java.util.UUID

class NotebookStorageDeviceTest {
    private val target=InstrumentationRegistry.getInstrumentation().targetContext
    private val identifier="myday-device-test-${UUID.randomUUID()}"
    private val directory=File(target.cacheDir,identifier)
    private val isolated=object: ContextWrapper(target) {
        override fun getApplicationContext(): Context=this
        override fun getFilesDir(): File=directory
        override fun getSharedPreferences(name: String,mode: Int): SharedPreferences=target.getSharedPreferences("$identifier-$name",mode)
    }
    @Before fun create() {directory.mkdirs()}
    @After fun cleanup() {
        directory.setWritable(true,false)
        require(directory.canonicalFile.parentFile==target.cacheDir.canonicalFile && directory.name.startsWith("myday-device-test-"))
        directory.deleteRecursively()
        target.getSharedPreferences("$identifier-diary",Context.MODE_PRIVATE).edit().clear().commit()
        target.getSharedPreferences("$identifier-diary-home",Context.MODE_PRIVATE).edit().clear().commit()
    }
    @Test fun migrationAndAtomicFileFailureKeepPreviousContent() {
        val old="""{"theme":2,"character":"🐱","blocks":[{"id":"legacy","type":"todo","text":"이전 기록","checked":true}]}"""
        isolated.getSharedPreferences("diary",Context.MODE_PRIVATE).edit().putString("2026-10-08",old).commit()
        val store=DiaryStore(isolated);val migrated=store.loadBook()
        assertTrue(migrated.days.getValue("2026-10-08").blocks.single().checked)
        assertEquals(old,isolated.getSharedPreferences("diary",Context.MODE_PRIVATE).getString("2026-10-08",null))
        val previous=File(directory,"notebook.json").readText()
        directory.setWritable(false,false)
        assertThrows(Exception::class.java) {store.saveBook(migrated.copy(haptics=false))}
        directory.setWritable(true,false)
        assertEquals(previous,File(directory,"notebook.json").readText());assertTrue(store.loadBook().haptics)
    }
    @Test fun windowsBackupPhotosValidateAndDamagedPhotoCannotReplaceFile() {
        val raw=InstrumentationRegistry.getInstrumentation().context.assets.open("windows-v4.json").bufferedReader().use {it.readText()}
        val book=NotebookJson.decode(raw);val store=DiaryStore(isolated);store.saveBook(book)
        assertEquals(book,store.loadBook())
        val previous=File(directory,"notebook.json").readText();val entry=book.days.getValue("2026-10-08")
        val bad=book.copy(days=book.days+("2026-10-08" to entry.copy(blocks=entry.blocks.map {if(it.type=="photo") it.copy(photo="broken") else it})))
        assertThrows(Exception::class.java) {store.saveBook(bad)};assertEquals(previous,File(directory,"notebook.json").readText())
    }
    @Test fun phonePhotoCorrectsRotationScalesAndStripsMetadata() {
        val file=File(directory,"source.jpg");val image=Bitmap.createBitmap(2048,1536,Bitmap.Config.ARGB_8888);Canvas(image).drawColor(Color.CYAN)
        file.outputStream().use {image.compress(Bitmap.CompressFormat.JPEG,95,it)};image.recycle()
        ExifInterface(file.path).apply {setAttribute(ExifInterface.TAG_ORIENTATION,ExifInterface.ORIENTATION_ROTATE_90.toString());setAttribute(ExifInterface.TAG_GPS_LONGITUDE,"127/1,0/1,0/1");saveAttributes()}
        val data=NotebookPhoto.read(isolated,Uri.fromFile(file));val bytes=Base64.decode(data,Base64.NO_WRAP)
        val result=BitmapFactory.decodeByteArray(bytes,0,bytes.size)
        assertEquals(1200,result.width);assertEquals(1600,result.height);result.recycle();assertTrue(bytes.size<=NotebookPhoto.MAX_BYTES)
        val exif=ExifInterface(bytes.inputStream());assertNull(exif.getAttribute(ExifInterface.TAG_GPS_LONGITUDE))
    }
    @Test fun catalogAndAnimatedArtworkMatchWindowsCountsAndKeepIdleQuiet() {
        val catalog=NotebookCatalog(target);assertEquals(100,catalog.templates.size);assertEquals(64,catalog.characters.size);assertEquals(12,catalog.items.size)
        assertEquals(100,catalog.templates.map {it.id}.distinct().size);assertEquals(64,catalog.characters.map {it.id}.distinct().size)
        catalog.characters.forEach {skin->val atlas=target.assets.open("sangmon/${skin.id}.png").use {BitmapFactory.decodeStream(it)};assertEquals(1536,atlas.width);assertEquals(1536,atlas.height);atlas.recycle()}
        val original=target.assets.open("sangmon/original.png").use {BitmapFactory.decodeStream(it)}
        fun warm(row: Int,frame: Int): Int {var count=0;for(y in 0 until 192) for(x in 0 until 192) {val p=original.getPixel(frame*192+x,row*192+y);if(Color.alpha(p)>0 && Color.red(p)>240 && Color.green(p) in 171..234 && Color.blue(p)<180) count++};return count}
        assertEquals(0,warm(0,0));assertTrue(warm(2,2)>10);original.recycle()
    }
}
