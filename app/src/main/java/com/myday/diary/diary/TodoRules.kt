package com.myday.diary.diary

import com.myday.diary.data.DiaryBlock
import com.myday.diary.data.DiaryTodoItem

/** Same flat projection and optional item fields as the Windows checklist. */
object TodoRules {
    fun items(block: DiaryBlock): List<DiaryTodoItem> {
        val previous=block.todoItems ?: return lines(block.text).map {DiaryTodoItem(it,block.checked)}
        val checked=if(previous.isNotEmpty() && block.checked!=previous.all {it.checked}) previous.map {it.copy(checked=block.checked)} else previous
        if(block.text==checked.joinToString("\n") {it.text}) return checked
        val remaining=checked.toMutableList()
        return lines(block.text).map {text ->
            val index=remaining.indexOfFirst {it.text==text}
            if(index>=0) remaining.removeAt(index) else DiaryTodoItem(text,previous.isEmpty() && block.checked)
        }
    }
    private fun lines(text: String)=text.split(Regex("\r?\n")).filter {it.isNotEmpty()}
    fun update(block: DiaryBlock,items: List<DiaryTodoItem>): DiaryBlock {
        require(items.size<=SharedTodo.MaxTodoItems && items.all {it.importance in 0..2})
        val text=items.joinToString("\n") {it.text};require(text.length<=100000)
        return block.copy(text=text,checked=items.isNotEmpty() && items.all {it.checked},todoItems=items,todoLayoutVersion=1,
            wide=block.wide || block.todoLayoutVersion<1)
    }
    fun newlyCompleted(block: DiaryBlock,prior: DiaryBlock?): Boolean {
        val current=items(block);val old=prior?.let(::items).orEmpty()
        if(current.isEmpty())return block.checked && prior?.checked!=true
        return current.withIndex().any {(index,item) -> item.checked && (index>=old.size || old[index].text!=item.text || !old[index].checked)}
    }
}
