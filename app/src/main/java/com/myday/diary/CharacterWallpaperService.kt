package com.myday.diary

import android.graphics.Canvas
import android.os.Handler
import android.os.Looper
import android.os.SystemClock
import android.service.wallpaper.WallpaperService
import android.view.MotionEvent
import android.view.SurfaceHolder
import com.myday.diary.data.DiaryStore
import com.myday.diary.data.DEFAULT_CHARACTER
import com.myday.diary.ui.wallpaper.CharacterWallpaperRenderer
import java.time.LocalDate
import kotlin.math.abs
import kotlin.math.hypot
import kotlin.math.min

/** Draw only while the launcher or system wallpaper preview is visible. */
class CharacterWallpaperService : WallpaperService() {
    override fun onCreateEngine(): Engine = CharacterEngine()

    inner class CharacterEngine : Engine() {
        private val handler = Handler(Looper.getMainLooper())
        private val renderer = CharacterWallpaperRenderer(this@CharacterWallpaperService)
        private val density = resources.displayMetrics.density
        private val store by lazy { DiaryStore(this@CharacterWallpaperService) }
        private var character = DEFAULT_CHARACTER
        private var theme = 0
        private var width = 0
        private var height = 0
        private var x = 0f
        private var y = 0f
        private var vx = 22f * density
        private var vy = 15f * density
        private var visible = false
        private var ready = false
        private var dragging = false
        private var touchMoved = false
        private var dragDx = 0f
        private var dragDy = 0f
        private var downX = 0f
        private var downY = 0f
        private var previousFrame = 0L
        private var nextRead = 0L
        private var happyUntil = 0L
        private val radius get() = min(renderer.characterRadius, min(width, height) / 4f).coerceAtLeast(1f)
        private val frame = object : Runnable {
            override fun run() {
                if (!visible || !ready) return
                drawFrame()
                handler.postDelayed(this, 33L)
            }
        }

        override fun onCreate(holder: SurfaceHolder) {
            super.onCreate(holder)
            setTouchEventsEnabled(true)
        }

        override fun onSurfaceChanged(holder: SurfaceHolder, format: Int, w: Int, h: Int) {
            super.onSurfaceChanged(holder, format, w, h)
            width = w
            height = h
            if (!ready) { x = w * 0.5f; y = h * 0.45f }
            ready = w > 0 && h > 0
            constrainPosition()
            startFrames()
        }

        override fun onVisibilityChanged(isVisible: Boolean) {
            visible = isVisible
            dragging = false
            nextRead = 0L
            startFrames()
        }

        override fun onSurfaceDestroyed(holder: SurfaceHolder) {
            ready = false
            handler.removeCallbacks(frame)
            super.onSurfaceDestroyed(holder)
        }

        override fun onDestroy() {
            visible = false
            handler.removeCallbacks(frame)
            super.onDestroy()
        }

        private fun startFrames() {
            handler.removeCallbacks(frame)
            previousFrame = SystemClock.uptimeMillis()
            if (visible && ready) handler.post(frame)
        }

        private fun constrainPosition() {
            x = x.coerceIn(radius, (width - radius).coerceAtLeast(radius))
            y = y.coerceIn(radius, (height - radius).coerceAtLeast(radius))
        }

        override fun onTouchEvent(event: MotionEvent) {
            when (event.actionMasked) {
                MotionEvent.ACTION_DOWN -> {
                    dragging = hypot(event.x - x, event.y - y) <= radius * 1.3f
                    touchMoved = false
                    downX = event.x; downY = event.y
                    dragDx = x - event.x; dragDy = y - event.y
                }
                MotionEvent.ACTION_MOVE -> if (dragging) {
                    touchMoved = touchMoved || hypot(event.x - downX, event.y - downY) > 8f * density
                    x = event.x + dragDx; y = event.y + dragDy
                    constrainPosition()
                }
                MotionEvent.ACTION_UP -> {
                    if (dragging && !touchMoved) happyUntil = SystemClock.uptimeMillis() + 1800L
                    dragging = false
                }
                MotionEvent.ACTION_CANCEL -> dragging = false
            }
            super.onTouchEvent(event)
        }

        private fun drawFrame() {
            val now = SystemClock.uptimeMillis()
            val dt = ((now - previousFrame).coerceIn(0L, 50L)) / 1000f
            previousFrame = now
            if (now >= nextRead) {
                val today = store.read(LocalDate.now().toString())
                character = today.character
                theme = today.theme
                nextRead = now + 3000L
            }
            if (!dragging) {
                x += vx * dt; y += vy * dt
                if (x < radius) vx = abs(vx)
                if (x > width - radius) vx = -abs(vx)
                if (y < radius) vy = abs(vy)
                if (y > height - radius) vy = -abs(vy)
                constrainPosition()
            }
            val holder = surfaceHolder
            val canvas: Canvas = try { holder.lockCanvas() ?: return } catch (_: IllegalStateException) { return }
            try {
                renderer.draw(canvas, theme, character, x, y, radius, now < happyUntil,
                    timeMillis = now, moving = !dragging, dragging = dragging, facingLeft = vx < 0f)
            } finally { holder.unlockCanvasAndPost(canvas) }
        }
    }
}
