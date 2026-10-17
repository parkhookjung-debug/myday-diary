package com.myday.diary.ui.components

import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.colorResource
import com.myday.diary.R
import com.myday.diary.ui.design.DiaryDimensions

@Composable
fun DiaryCard(modifier: Modifier = Modifier, content: @Composable ColumnScope.() -> Unit) {
    Card(modifier = modifier, shape = RoundedCornerShape(DiaryDimensions.cardCorner), content = content)
}

@Composable
fun DiaryBlockSurface(content: @Composable ColumnScope.() -> Unit) {
    Card(shape = RoundedCornerShape(DiaryDimensions.cardCorner),
        colors = CardDefaults.cardColors(containerColor = colorResource(R.color.myday_card)), content = content)
}

@Composable
fun DiaryActionButton(text: String, onClick: () -> Unit, outlined: Boolean = false) {
    if (outlined) OutlinedButton(onClick = onClick, modifier = Modifier.fillMaxWidth()) { Text(text) }
    else Button(onClick = onClick, modifier = Modifier.fillMaxWidth()) { Text(text) }
}
