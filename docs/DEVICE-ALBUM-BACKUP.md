# 0.10.0 · 기기 연결 / 사진 모아보기 / 작은 자동 백업

Windows·Mac·Android에서 **설정·백업 또는 사이드바 → 기록 도구**를 엽니다. 기존 일기·사진·상몬·성장·소품·버전 1~8 백업을 보존합니다. 이번 기능도 계정·결제·클라우드 서버 없이 사용합니다.

## 1. 앱 안에서 기기 연결

1. 두 기기를 같은 Wi-Fi에 연결합니다.
2. 보낼 기기에서 **기기 연결 / Wi-Fi·QR → 이 기기의 연결 QR 만들기**를 누릅니다. 여러 네트워크 주소가 보이면 같은 Wi-Fi의 사설 IPv4 주소를 선택합니다.
3. 받을 기기에서 QR을 스캔하거나 연결 주소를 복사합니다. 앱의 **다른 기기의 연결 주소 → 기록 받기**를 이용하면 파일 선택기 없이 내용을 확인할 수 있습니다. Mac·Android는 QR 이미지 읽기도 지원합니다. 휴대폰 카메라로 열리는 브라우저에서는 JSON을 받은 뒤 앱의 백업 복원으로 가져올 수도 있습니다.
4. 날짜·사진 수·겹치는 날짜를 확인하고 **가져오기**를 선택합니다. 기존 날짜별 병합·재화 충돌 검사와 복구 보관이 적용됩니다. 백업은 연결을 연 시점의 스냅샷이며 자동 동기화하지 않습니다.
5. 창을 닫거나 10분이 지나면 서버와 초대가 종료됩니다. Android는 앱을 배경으로 보내도 종료합니다. 다시 연결할 때 새 QR을 만듭니다.

Python이나 별도 서버 실행은 필요 없습니다. 네이티브 앱이 사설 IPv4 한 주소의 임의 포트에서만 일시적으로 수신합니다. 방화벽·OS의 로컬 네트워크 허용은 사용자 기기에서 확인합니다. 공유기 포트 포워딩·공개 서버 배포를 추가하지 않습니다. HTTP 전송이므로 신뢰하는 같은 Wi-Fi에서만 사용합니다. 초대 토큰은 난수로 생성하고 로그·백업에 저장하지 않습니다. 공개 주소·임의 경로·리디렉션·잘못된 Host/Origin·128MB 초과 백업을 차단합니다.

기존 `tools/lan-transfer.py`는 선택적으로 사용할 수 있지만 새 앱의 연결에는 필요하지 않습니다.

## 2. 월별 사진 모아보기

**기록 도구 → 사진 모아보기**에서 일기에 첨부한 사진만 월별로 모아 봅니다. 날짜와 설명을 표시하고 사진을 누르면 원래 날짜의 일기로 이동합니다. 빈 사진 자리·휴지통 속 사진은 앨범에 섞지 않습니다. 원본 사진과 자유 배치 좌표는 바꾸지 않고 썸네일만 화면에 준비합니다. 사진은 외부로 업로드하지 않습니다.

## 3. 자동 백업은 작게 시작합니다

- 앱이 정상적으로 기록을 저장한 날 하루 한 번, 기기 내부 `automatic-backups/`에 전체 공통 JSON을 보관합니다.
- 기본 켜짐이며 **자동 백업** 화면에서 끌 수 있습니다. 끄더라도 기존 백업은 남깁니다.
- 최대 최근 **3일분·256MiB**로 제한합니다. 새 백업 저장에 성공한 뒤 오래된 앱 생성 사본만 정리합니다. 큰 백업은 용량 한도 때문에 3개보다 적게 남을 수 있습니다.
- 마지막 성공 시각·실패 내용·보관 수를 표시하고 **지금 전체 백업**으로 같은 날의 사본을 갱신할 수 있습니다.
- 백업 실패는 일기 저장을 실패 처리하지 않습니다. 앱을 종료한 동안의 예약 작업이나 클라우드 동기화는 추가하지 않습니다.
- 이 사본은 기기 내부에 있으므로 기기 밖 보관에는 기존 **백업 내보내기**도 사용합니다. 가져오기에는 기존 확인·병합 규칙을 적용하고 XP·재화를 중복 지급하지 않습니다.

## 수정할 파일

| 수정 목적 | 원본 |
|---|---|
| 공통 버전·백업 한도 | `shared/journal-tools.json` → `python tools/sync-journal.py` |
| 브라우저에서 보이는 공통 전송 화면 | `shared/device-link.html` → `python tools/sync-device-link.py` |
| Windows 네트워크·QR·일일 백업 | `windows/Core/DeviceLink.cs`, `DeviceQr.cs`, `AutomaticBackup.cs` |
| Windows 연결·앨범 화면 | `windows/UI/DeviceLinkWindow.cs`, `PhotoAlbumWindow.cs`, `JournalToolsWindow.cs` |
| Mac 네트워크·앨범·백업 | `macos/Sources/MyDay/DeviceLink.swift`, `DeviceViews.swift`, `AutomaticBackup.swift` |
| Android 네트워크·백업 | `app/src/main/java/com/myday/diary/device/DeviceLink.kt`, `data/AutomaticBackup.kt` |
| Android 화면 | `ui/diary/PhoneDeviceLink.kt`, `PhonePhotoAlbum.kt`, `PhoneAutomaticBackup.kt` |

Windows QR은 Project Nayuki MIT 알고리즘의 제한된 v6-L 포트이며 독립 기준 행렬과 비교합니다. Android QR은 [ZXing 3.5.3](https://github.com/zxing/zxing/releases/tag/zxing-3.5.3) Apache 2.0 라이브러리, Mac은 OS의 Core Image를 사용합니다. 세 플랫폼 모두 QR을 기기 안에서 생성·해석합니다.
