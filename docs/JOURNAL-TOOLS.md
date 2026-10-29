# 0.9.0 · 6단계 기록 도구

**0.10.0 변경:** [앱 내 기기 연결·월별 사진·자동 백업](DEVICE-ALBUM-BACKUP.md)을 추가했습니다. 아래 4번은 이전 Python 도구의 선택적 사용법입니다. 새 앱은 Python 없이 연결합니다. 현재 앱 버전은 0.10.0이며 기존 기록 도구도 유지합니다.

Windows는 사이드바 **기록 도구 · 복구 / 리포트**, Mac은 사이드바 또는 설정의 **기록 도구**, Android는 **설정·백업 → 기록 도구**에서 사용합니다. 세 앱은 같은 공통 백업 버전 1~8을 유지합니다.

| 단계 | 구현 | 사용 위치 |
|---|---|---|
| 1 | 수정 전 글·사진 보관, 삭제 일기·블록 복구, 이력 선택 복원·영구 삭제 | 휴지통·이력 |
| 2 | 월별 기록일·연속 기록·사진·감정 분포·습관 완료율 | 월간 리포트 |
| 3 | 기본 꺼짐, 사용자가 정한 시간의 일일 알림 | 알림 |
| 4 | 같은 Wi-Fi의 임시 QR·주소로 백업 양방향 전달 | Wi-Fi·QR + 컴퓨터 전송 도구 |
| 5 | 글·사진 저장·습관 체크 성공 시 상몬의 작은 점프와 응원 문구 | 일기의 상몬 카드 |
| 6 | 현재 버전·변경 내역·최신 정식 릴리스 확인·백업·설치 안내 | 업데이트 |

## 1. 휴지통과 변경 이력

원본 기록을 덮어쓰기 **전에** `journal-history/`에 이전 날짜의 글·사진·체크·좌표를 원자적으로 저장합니다. 평소 수정은 5분 간격의 체크포인트로, 블록·사진·날짜 삭제는 즉시 보관합니다. 처음 기록하거나 내용이 같으면 중복 사본을 만들지 않습니다. 이력 쓰기가 실패하면 원본 덮어쓰기도 중단합니다.

복원은 날짜의 내용만 바꾸며 XP·출석·재화·뽑기 내역을 되돌리거나 다시 지급하지 않습니다. 복원 직전 내용도 보관합니다. 보관함은 기기당 256MiB·2,000개까지이며 자동 영구 삭제하지 않습니다. 가득 차면 필요 없는 이력을 명시적으로 정리한 뒤 저장할 수 있습니다. 이력 영구 삭제에는 확인창이 있습니다.

일반 JSON 백업에는 현재 일기와 성장 정보가 들어갑니다. 기기별 이력과 알림 설정은 자동으로 다른 기기에 전송하지 않습니다. 앱 삭제·저장 폴더 삭제 전에는 공통 백업뿐 아니라 보관할 이력 폴더도 따로 복사하세요.

## 2. 월간 리포트

본문·사진·체크 중 하나라도 있는 날짜를 기록한 날로 집계합니다. 빈 페이지·빈 습관은 제외합니다. 습관 완료율의 분모는 본문이 있거나 체크한 습관 블록 수입니다. 감정은 사용자가 직접 고른 값만 세며 감정 추정·진단을 하지 않습니다. 윤년·월 경계·기록이 없는 달도 지원합니다. 네트워크를 사용하지 않습니다.

## 3. 사용자가 정한 일기 알림

- Windows: 앱이 트레이에서 실행 중일 때 선택한 시간에 한 번 표시합니다. 10분 넘게 지난 알림은 몰아서 보내지 않습니다. 알림을 누르면 일기창을 엽니다.
- Mac: macOS 알림 허용 후 매일 같은 시각의 시스템 알림을 예약합니다. 앱 종료 후에도 OS가 처리하며 끄면 예약을 취소합니다.
- Android: Android 13 이상에서는 켤 때 알림 권한을 요청합니다. 정확한 알람 특별 권한 없이 일일 알림을 예약합니다. 절전 상태에 따라 늦어질 수 있습니다. 재부팅·시간대 변경 시 다시 예약하고 끄면 취소합니다. [Android 공식 알람 안내](https://developer.android.com/develop/background-work/services/alarms).

알림은 기본 꺼짐이며 일기를 쓰지 않은 날에도 성장 점수를 차감하지 않습니다.

## 4. Wi-Fi / QR 기록 이동

이번 단계는 **컴퓨터에서 실행하는 별도 로컬 도구 + 각 앱의 기존 공통 백업 가져오기** 방식입니다. 앱만 설치한 상태에서 서버를 자동 실행하는 기능은 아직 없습니다. 컴퓨터에 Python 3.11 이상과 이 저장소가 필요하며 외부 패키지 설치·계정·결제는 없습니다. QR은 포함된 MIT 라이브러리로 컴퓨터 안에서 생성합니다.

1. 보낼 앱에서 공통 백업 JSON을 저장합니다. 휴대폰에서 컴퓨터로만 보낼 때는 `--receive`를 사용합니다.
2. 컴퓨터의 Wi-Fi 사설 IPv4 주소를 확인합니다. Windows는 `ipconfig`, Mac은 시스템 설정 → Wi-Fi → 세부사항에서 확인합니다.
3. 저장소 폴더에서 실행합니다.

```powershell
python tools/lan-transfer.py --host 192.168.0.10 --file "MyDay-backup.json"
# 휴대폰 → 컴퓨터만 받을 때:
python tools/lan-transfer.py --host 192.168.0.10 --receive
```

Mac에서 `python3`을 사용해도 됩니다. `--file`을 생략하면 파일 선택기를 엽니다. 기본 포트는 8766이며 `--port`로 변경합니다. 방화벽에서 해당 Python 실행의 사설 네트워크 연결만 허용해야 다른 기기에서 열 수 있습니다. 공용 네트워크나 라우터 포트 포워딩은 사용하지 마세요.

4. 컴퓨터 관리 화면의 QR을 휴대폰 카메라로 스캔하거나 표시된 초대 주소를 다른 기기 브라우저에서 엽니다.
5. **백업 다운로드**로 받거나 **선택한 백업 전송**으로 컴퓨터에 보냅니다. 컴퓨터 관리 화면의 **컴퓨터로 받기**는 받은 업로드만 다운로드합니다.
6. 받은 기기의 앱에서 **설정·백업 → 백업 복원**을 선택합니다. 기존 확인·형식 검증·충돌 처리가 적용됩니다. 경제 기록이 갈라진 두 기기의 재화를 자동 병합하지 않습니다.
7. 10분 후 서버가 종료됩니다. 터미널에서 Ctrl+C로 즉시 닫을 수 있습니다. 업로드는 메모리에만 있고 자동 저장·자동 가져오기하지 않습니다.

초대는 256비트 난수, 관리용 자격은 별도로 생성합니다. 잘못된 토큰·다른 Origin/Host·128MB 초과·공개 바인딩을 차단합니다. 토큰은 URL fragment에 두어 HTTP 요청 로그·Referer에 실리지 않습니다. 로컬 HTTP 전송이므로 신뢰하는 같은 Wi-Fi에서만 사용하세요. 클라우드·중계 서버·외부 QR 생성 서버를 사용하지 않습니다.

## 5. 일상에 반응하는 상몬

저장 성공 후에만 작은 점프와 응원 문구를 표시합니다. 타이핑 중에는 4초 간격으로 반응을 제한합니다. 습관 체크는 별도 문구입니다. 날짜 이동·가져오기·복원에는 보상을 추가하지 않습니다. 불꽃은 기존대로 직접 건드렸을 때만 뿜고 하품 애니메이션은 유지합니다.

## 6. 업데이트

각 앱에서 현재 버전 `0.9.0`, 변경 내역, GitHub 정식 릴리스 확인과 백업·설치 페이지 연결을 제공합니다. 확인 버튼을 눌렀을 때만 GitHub API를 호출하며 새 릴리스가 없거나 연결이 실패한 경우를 구분합니다. 새 Releases 게시나 main 자동 병합은 하지 않습니다.

이번 버전은 **백업 후 OS에서 직접 설치하는 흐름**입니다. 앱 실행 중의 자동 바이너리 교체·무인 설치는 하지 않습니다.

- Windows: 공통 백업을 저장하고 MyDay를 트레이의 모두 종료로 끝낸 뒤 새 EXE를 앱 폴더에 교체합니다. `%LOCALAPPDATA%/MyDay/Windows`의 기록·이력·알림 설정을 유지합니다.
- Mac: 백업 후 앱을 종료하고 새 MyDay.app을 Applications에 교체합니다. `~/Library/Application Support/MyDay`를 유지합니다. CPU 종류를 확인합니다.
- Android: 같은 applicationId·서명이고 versionCode가 높은 APK로 덮어 설치합니다. 서명이 다르면 먼저 백업하고 기존 앱을 삭제하지 마세요. CI debug 서명이 매번 같다는 보장은 없습니다.

개발 빌드는 GitHub Actions의 Artifacts에 있습니다. 이전 Releases의 파일을 자동 교체하지 않습니다.

## 어디를 수정하나요?

| 변경할 내용 | 파일 |
|---|---|
| 버전·보관 한도·응원 문구 | `shared/journal-tools.json` → `python tools/sync-journal.py` |
| Windows 이력·리포트 | `windows/Core/JournalHistory.cs` |
| Windows 도구 화면·알림·반응 | `windows/UI/JournalToolsWindow.cs`, `DiaryWindow.JournalTools.cs` |
| Mac 이력·리포트·도구 화면·알림 | `macos/Sources/MyDay/JournalTools.swift` |
| Android 이력·리포트 | `app/src/main/java/com/myday/diary/data/JournalTools.kt` |
| Android 도구 화면·알림 | `ui/diary/PhoneJournalTools.kt`, `platform/JournalReminder.kt` |
| Wi-Fi 전송·QR | `tools/lan-transfer.py`, `tools/vendor/` |
| 검증 | `windows/Tests/JournalToolsTests.cs`, `macos/Sources/MyDay/Verification/JournalToolsVerification.swift`, Android `JournalToolsTest.kt`, `tools/test_lan_transfer.py` |

공통 백업은 계속 버전 8이며 앱 버전 0.9.0과 별개입니다. 파일명·상몬 ID·기존 경제 규칙을 바꾸지 않습니다.

## 최종 검증 · 2026-10-23

| 대상 | 검증 결과 | 실제 빌드 |
|---|---|---|
| Windows | 253개 검사·사진 삭제 전 보관과 복구·XP 미지급·쓰기 실패 시 원본 유지·실제 5개 도구 화면·기존 기능 검사 성공 | [`4d2d1cd` · Windows CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/39940071544), `myday-windows-55` |
| Mac | 네이티브 빌드·이력·사진·복구·월간 집계·쓰기 실패 보호·기존 성장/경제·실제 메뉴/리포트 렌더링·Windows 백업 검사 성공 | [`eda4506` · 공통 CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/39940531857), `myday-chat-macOS-ARM64` |
| Android | APK·lint·JVM 검사, Android 35 기기 검사 **16개 / 실패 0**, 사진 데이터·설명·배치 복구, 도구 메뉴·리포트·알림·업데이트 화면, 실제 Windows 백업 검사 성공 | [`4d2d1cd` · Android CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/39940071701), `myday-debug-58` |
| 공통·로컬 도구 | 기존 생성기 8개·기록 정책 생성기 2개·실제 HTTP 전송 5개 검사 성공. QR 관리 화면 브라우저 검토 완료 | Windows CI에도 기록 도구 검사를 포함 |

[실제 화면](../previews/journal-tools/README.md) · [파일별 SHA-256와 빌드 출처](builds/myday-0.9.0.json). Windows EXE 파일 버전 `0.9.0.0`, Mac 앱 버전 `0.9.0`, Android `versionCode=9 / versionName=0.9.0`을 확인했습니다.

알림 시간 도래·절전/재부팅 동작·실물 Galaxy 햅틱·실제 두 기기의 같은 Wi-Fi 연결은 사용자 기기에서 추가 확인할 항목입니다. 현재 검증은 네이티브 앱·CI 기기·루프백 전송 기준입니다. 새 정식 릴리스가 없는 GitHub API 404 응답도 확인했으며 앱에서 개발 빌드 안내로 처리합니다.
