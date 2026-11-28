package com.myday.diary.diary

object VideoRules {
    fun validReference(path:String):Boolean {
        if(path.isEmpty()||path.length>4096||path.any {it.isISOControl()})return false
        if(path.startsWith("content://"))return runCatching {val uri=java.net.URI(path);uri.scheme=="content"&&!uri.host.isNullOrEmpty()}.getOrDefault(false)
        return path.startsWith("/")&&!path.startsWith("//") || Regex("^[A-Za-z]:[\\\\/].+").containsMatchIn(path)
    }
}
