package com.myday.diary.ui.diary

import android.app.DatePickerDialog
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.platform.LocalContext
import com.myday.diary.data.DiaryStore
import com.myday.diary.diary.DiaryController
import com.myday.diary.platform.HomeScreenActions
import com.myday.diary.ui.design.MyDayTheme
import java.time.LocalDate

@Composable
fun DiaryRoute() {
    val context = LocalContext.current
    val controller = remember { DiaryController(DiaryStore(context)) }
    val homeActions = remember(context) { HomeScreenActions(context) }
    MyDayTheme {
        DiaryScreen(controller.date, controller.entry, controller.saved, DiaryScreenActions(
            onPreviousDay = { controller.selectDate(controller.date.minusDays(1)) },
            onNextDay = { controller.selectDate(controller.date.plusDays(1)) },
            onChooseDate = {
                val date = controller.date
                DatePickerDialog(context, { _, year, month, day -> controller.selectDate(LocalDate.of(year, month + 1, day)) }, date.year, date.monthValue - 1, date.dayOfMonth).show()
            },
            onRetrySave = controller::retrySave,
            onThemeChange = controller::selectTheme,
            onCharacterChange = controller::selectCharacter,
            onAddBlock = controller::addBlock,
            onEditBlock = controller::editBlock,
            onMoveBlock = controller::moveBlock,
            onDeleteBlock = controller::deleteBlock,
            onPinWidget = homeActions::pinWidget,
            onSetWallpaper = homeActions::setWallpaper
        ))
    }
}
