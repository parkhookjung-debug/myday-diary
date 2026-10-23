# Windows · Mac · Android 공통 앱

세 앱은 같은 크림·숲·라벤더 테마, 올리브 메뉴, 종이 카드와 상몬 그림을 사용합니다. 메뉴 순서는 **일기 → 달력·검색 → 상몬·공방 → 라운지 → 설정·백업**입니다. 컴퓨터는 사이드바, 휴대폰은 하단 탭으로 표시합니다. OS의 파일 선택기·키보드·창·진동은 네이티브 기능을 유지합니다.

| 공통 기능 | Windows | Mac | Android |
|---|---|---|---|
| 일기·사진·할 일·습관·감정·달력·본문 검색 | ✓ | ✓ | ✓ |
| 일기 형식 100종·자유 배치·내 레이아웃 | ✓ | ✓ | ✓ |
| 같은 상몬 64종·소품 300종·8슬롯 | ✓ | ✓ | ✓ |
| Lv.100·100/200/300일 성장 패턴·출석 | ✓ | ✓ | ✓ |
| 등급 뽑기·꽃잎·조각·합성·제작·성취 보상 | ✓ | ✓ | ✓ |
| 버전 1~8 공통 JSON 읽기·버전 8 기록 보존 | ✓ | ✓ | ✓ |
| 같은 초대형 로컬 채팅 서버 | ✓ | ✓ | ✓ |
| OS 전용 기능 | 바탕화면 상몬·트레이 | 바탕화면 상몬·메뉴 막대 | 햅틱·홈 위젯·라이브 배경화면 |

이 표는 현재 소스의 구현 범위입니다. 빌드·기기 검증 결과는 해당 커밋의 Actions를 확인합니다. 기존 Releases의 EXE/APK/ZIP은 자동 교체되지 않습니다. 공통 백업을 파일로 옮겨 가져오며 클라우드 자동 동기화는 하지 않습니다. 두 기기에서 같은 백업으로 따로 뽑기·교환을 진행한 내역은 합칠 수 없다는 안내로 차단하여 재화 중복을 막습니다.

## 여기부터 수정하세요

| 원하는 변경 | 수정할 원본 |
|---|---|
| 메뉴 이름·순서·인사말 | `shared/app-shell.json` |
| 배경·카드·채팅·강조 색 | `shared/desktop.json`의 `design` |
| 성장 점수·기간·검증 패턴 | `shared/desktop.json`, Windows `SangmonGrowth.cs`, Android `NotebookRules.kt`, Mac `SharedCompanion.swift` |
| 뽑기 등급·확률·합성·성취 | `shared/collection.json` |
| 일기 형식과 질문 100종 | `shared/catalog/templates.json` |
| Windows 화면 | `windows/UI/DiaryWindow.Shell.cs`, `PreferencesWindow.cs`, `CompanionHubWindow.cs` |
| Mac 화면 | `macos/Sources/MyDay/UnifiedViews.swift`, `SavedLayouts.swift` |
| Android 화면·햅틱 | `app/src/main/java/com/myday/diary/ui/diary/PhoneNotebookScreen.kt`, `PhoneCompanion.kt`, `PhoneLounge.kt`, `platform/` |
| 같은 캐릭터·소품 그림 | Windows `Character/` 원본 → `tools/export-android-assets.ps1` → Android assets. Mac 빌드 시 `tools/prepare-mac-assets.py`가 같은 파일을 묶음에 넣음 |

공통 JSON을 고친 뒤 `python tools/sync-desktop.py`를 실행하고 생성 파일도 커밋합니다. `.g.cs`, `.g.kt`, `.g.swift`를 직접 편집하지 않습니다. Mac용 그림 staging 폴더는 Git에서 제외해 원본 이미지가 두 벌로 어긋나지 않게 합니다. Mac 소스는 Command Line Tools가 설치된 Mac에서 `zsh macos/build.command`로 빌드합니다.

## 기록 보존

기존 Mac의 일기·사진·메타데이터 파일과 Mac 전용 백업을 계속 읽습니다. 첫 공통 기능 저장 시 이전 XP 지급 날짜와 착용 중인 소품을 보존하고, 기본 소품 8개를 지급합니다. 이후 성장과 경제 기록은 `windows-metadata.json`의 공통 필드에서 관리합니다. 현재 소품·재료는 쓰기에 성공했을 때만 변경합니다. 가져오기 전 복구 사본도 계속 남깁니다.

공통 백업은 각 앱의 **설정·백업 → 백업 다운로드 / 백업 복원**을 사용합니다. Mac의 기존 전용 백업은 전체 복원이고 공통 백업은 날짜별 병합입니다. 구버전 Mac 앱으로 새 버전 8 백업을 열지 마세요.

## 채팅과 비용

서버는 기존 로컬 서버만 사용하며 신규 공개 배포나 결제 서비스를 추가하지 않습니다. [서버 실행·초대코드 안내](FREE-CHAT.md)를 따릅니다. 세 기기가 같은 Wi-Fi에 있으면 PC의 사설 IP(예: `http://192.168.0.10:8765/`)를 모두 입력합니다. Android 에뮬레이터에서만 `10.0.2.2`가 개발 PC를 가리킵니다. 휴대폰의 `127.0.0.1`은 휴대폰 자신입니다.

초대코드·세션 토큰은 메모리에만 두고 일기·백업에 넣지 않습니다. Android는 라운지 이탈·앱이 배경으로 이동할 때 연결을 정리하며 재입장해야 합니다. 채팅 API만 요청하고 일기·사진은 전송하지 않습니다.

## 검증과 실행 파일

- Windows: `windows/build.ps1`, `MyDay.exe --self-test`, `--smoke-test 출력폴더`.
- Mac: `macos/build.command`에 기존 데이터 검사·공통 성장·경제·버전 8 왕복·저장 실패 검사를 포함합니다. CI에서 넓은 창·좁은 창·상몬 화면을 렌더링합니다.
- Android: APK 빌드·lint·JVM 검사, Android 35 에뮬레이터 화면 검사, 실제 Windows 직렬화 코드로 공통 백업 검사.
- 공통: `python -m unittest discover -s tools -p test_sync_desktop.py -v`.

GitHub **Actions → 해당 커밋의 완료된 실행 → Artifacts**에서 Windows EXE, Mac 앱 ZIP, Android APK를 받습니다. Mac ZIP은 CPU 종류(arm64/x86_64)를 확인하세요. CI에서 제공하는 Mac은 Apple Silicon이며 Intel용은 Intel Mac에서 소스로 빌드해야 합니다. 별도 서명 키·Apple 공증·스토어 출시는 이번 변경에 포함하지 않습니다.
