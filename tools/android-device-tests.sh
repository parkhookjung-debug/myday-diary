#!/usr/bin/env bash
set -euo pipefail
export PATH="$ANDROID_HOME/platform-tools:$ANDROID_HOME/emulator:$PATH"
export ANDROID_USER_HOME="$PWD/.bootstrap/android-user"
export ANDROID_AVD_HOME="$PWD/.bootstrap/avd-home"
mkdir -p "$ANDROID_USER_HOME" "$ANDROID_AVD_HOME"
"$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" "system-images;android-35;google_apis;x86_64"
mkdir -p .bootstrap
echo no | "$ANDROID_HOME/cmdline-tools/latest/bin/avdmanager" create avd --force --name MyDayPhone --package "system-images;android-35;google_apis;x86_64" --device pixel_7 --path "$PWD/.bootstrap/android-avd"
printf 'avd.ini.encoding=UTF-8\npath=%s\ntarget=android-35\n' "$PWD/.bootstrap/android-avd" > "$ANDROID_AVD_HOME/MyDayPhone.ini"
if [[ -e /dev/kvm ]]; then sudo chmod a+rw /dev/kvm; fi
"$ANDROID_HOME/emulator/emulator" -avd MyDayPhone -no-window -no-audio -no-snapshot -no-boot-anim -gpu swiftshader_indirect > app/build/emulator.log 2>&1 &
emulator_pid=$!
trap 'kill "$emulator_pid" 2>/dev/null || true' EXIT
adb start-server
booted=false
for attempt in $(seq 1 120); do
  if [[ "$(adb shell getprop sys.boot_completed | tr -d '\r')" == 1 ]]; then booted=true; break; fi
  if ! kill -0 "$emulator_pid" 2>/dev/null; then cat app/build/emulator.log; exit 1; fi
  sleep 2
done
if [[ "$booted" != true ]]; then cat app/build/emulator.log; exit 1; fi
adb shell settings put global window_animation_scale 0
adb shell settings put global transition_animation_scale 0
adb shell settings put global animator_duration_scale 0
adb shell input keyevent 82
test_status=0
./gradlew --no-daemon :app:connectedDebugAndroidTest || test_status=$?
mkdir -p app/build/device-previews
adb pull /sdcard/Android/data/com.myday.diary/files/previews app/build/device-previews || true
adb logcat -d -t 400 > app/build/device-logcat.txt
exit "$test_status"
