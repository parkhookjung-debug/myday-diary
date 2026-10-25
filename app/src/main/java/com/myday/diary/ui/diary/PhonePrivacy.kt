package com.myday.diary.ui.diary
import android.app.Activity
import android.view.WindowManager
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.myday.diary.data.PinVault
import kotlinx.coroutines.*
class PhonePrivacy(val vault:PinVault){var enabled by mutableStateOf(false);var locked by mutableStateOf(false);var error by mutableStateOf("");var touched=android.os.SystemClock.elapsedRealtime();init{runCatching{enabled=vault.enabled();locked=enabled}.onFailure{locked=true;error="잠금 설정을 읽지 못했어요. 기록을 열지 않았어요."}};fun touch(){touched=android.os.SystemClock.elapsedRealtime()};fun refresh(){enabled=vault.enabled();touch()}}
@Composable fun PhonePrivacyGate(privacy:PhonePrivacy):Boolean {
 val owner=LocalLifecycleOwner.current;val activity=LocalContext.current as? Activity
 DisposableEffect(owner,privacy.enabled){if(privacy.enabled)activity?.window?.addFlags(WindowManager.LayoutParams.FLAG_SECURE) else activity?.window?.clearFlags(WindowManager.LayoutParams.FLAG_SECURE);val observer=LifecycleEventObserver{_,e->if(e==Lifecycle.Event.ON_STOP&&privacy.enabled)privacy.locked=true};owner.lifecycle.addObserver(observer);onDispose{owner.lifecycle.removeObserver(observer)}}
 LaunchedEffect(privacy){while(true){delay(1000);if(privacy.enabled&&android.os.SystemClock.elapsedRealtime()-privacy.touched>=300000)privacy.locked=true}}
 if(!privacy.locked)return false
 var pin by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};val scope=rememberCoroutineScope()
 Surface(Modifier.fillMaxSize()){Column(Modifier.padding(28.dp),verticalArrangement=Arrangement.spacedBy(18.dp)){Text("나만의 하루를 열어 볼까요?",style=MaterialTheme.typography.headlineSmall);Text("이 기기의 숫자 PIN을 입력해 주세요.");OutlinedTextField(pin,{pin=it.take(12)},label={Text("PIN")},visualTransformation=PasswordVisualTransformation(),singleLine=true);Text(privacy.error);Button({busy=true;scope.launch{runCatching{withContext(Dispatchers.Default){check(privacy.vault.verify(pin)){"PIN이 맞지 않아요."}}}.onSuccess{privacy.locked=false;privacy.error="";privacy.touch();pin=""}.onFailure{privacy.error=it.message ?: "잠금 해제 실패"};busy=false}},enabled=!busy){Text(if(busy)"확인 중…" else "잠금 해제")}}}
 return true
}
