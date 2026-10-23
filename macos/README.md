# MyDay macOS 0.8.0 · 세 플랫폼 공통 앱

Windows·Android와 같은 메뉴, 일기 100종, 상몬 64종·300소품·8슬롯, Lv.100 성장·출석·등급 선물 공방을 제공하는 SwiftUI 앱입니다. [전체 기능·공통 원본·수정할 파일·기기별 차이](../docs/THREE-PLATFORMS.md).

## 실행 파일 받기

GitHub **Actions → 해당 소스 커밋의 Mac·Windows 검사 → Artifacts → myday-chat-macOS-ARM64**에서 ZIP을 받습니다. 안의 `MyDay-macOS-arm64-v0.8.0-preview.zip`을 풀고 `MyDay.app`을 응용 프로그램 폴더로 복사합니다. 기존 Releases의 이전 Mac ZIP에는 최신 기능이 포함되지 않습니다.

macOS 13 이상을 지원합니다. CI 파일은 Apple Silicon용입니다. Intel Mac은 해당 Mac에서 빌드하세요. 로컬 개발 서명이며 Apple 공증은 적용하지 않았습니다. 기존 일기는 앱을 종료한 뒤 백업하고 새 앱을 실행합니다.

## 소스에서 빌드

Command Line Tools가 설치된 Mac에서 저장소 루트 기준:

```sh
zsh macos/build.command
# 또는 빌드 후 앱 열기
zsh macos/run.command
```

빌드 시 `tools/prepare-mac-assets.py`가 Android와 같은 Windows 원본 그림을 Mac 리소스 폴더에 준비합니다. Python 3은 개발 빌드에만 필요하고 완성한 앱 실행에는 필요하지 않습니다. `swift build`만 직접 쓰려면 먼저 이 준비 도구를 실행하세요.

## 같은 화면과 기능

메뉴는 **일기 → 달력·검색 → 상몬·공방 → 라운지 → 설정·백업**입니다. 넓은 창은 좌측 메뉴·가운데 일기·오른쪽 상몬 카드, 좁은 창은 상몬 카드를 일기 위로 옮깁니다.

- 날짜별 글·사진·할 일·습관·감정, 자동 저장·저장 실패 재시도, 달력·전체 본문·사진 설명 검색.
- 질문과 배치를 미리 보는 일기 형식 100종. 자유 배치의 제목 드래그·↘ 크기 조절·확대·가로 스크롤.
- **내 레이아웃:** 현재 빈 배치 저장·검색·이름 변경·삭제·다른 날 적용. 글·사진·체크 상태를 복사하지 않습니다.
- Windows에서 만든 같은 상몬 64종·소품 300종을 표시하고 8개 슬롯에 독립 장착합니다. 터치 때 잠깐 불꽃, 평소 숨쉬기·눈 깜박임·휴식·입 없는 하품 프레임을 사용합니다.
- [성장 점수표](../docs/SANGMON-GROWTH.md): Lv.100·3,590 XP, 출석 5·첫 기록 10·글 20자 10·사진 5·할 일 5·습관 5, 연속 7일 출석 보너스 10 XP.
- [공방](../docs/SANGMON-ATELIER.md): 같은 등급 확률·10회 새 소품 보장·꽃잎 교환·조각 합성·제작·10~300종 성취 보상.
- 바탕화면 친구는 메뉴 막대에서 표시·숨기기, 마우스로 이동하며 일기 열기 버튼을 제공합니다.

## 백업과 기존 기록 보존

기록 위치는 `~/Library/Application Support/MyDay/`입니다. 날짜별 일기 JSON·사진과 기존 `companion.json`, 공통 성장·장비·경제를 보관하는 `windows-metadata.json`을 로컬에 저장합니다. 서버에 일기·사진을 자동 전송하지 않습니다.

**설정·백업 → 공통 백업 다운로드 / 백업 복원**으로 Windows·Android와 같은 버전 1~8 JSON을 읽고 최신 필드를 보존합니다. 동일 날짜는 가져온 일기로 교체하고 다른 날짜는 유지합니다. 성장 지급 날짜·미션은 합집합으로 처리하며, 따로 진행한 경제 내역은 병합을 거부합니다. 가져오기 전 확인과 `MyDay-previous-…` 복구 사본을 유지합니다.

기존 Mac 전용 백업 버전 1~2와 예전 일기도 계속 읽습니다. **Mac 전용 백업 복원은 전체 교체**, 공통 백업은 날짜별 병합입니다. 새 공통 기능을 저장할 때 기존 XP 지급 날짜와 착용 소품을 이전하며 기본 소품 8종을 제공합니다. 최신 버전 8 백업은 구버전 Mac 앱에서 열 수 없습니다.

사진은 긴 변 1,600px 이하 JPEG로 저장합니다. 공통 백업은 사진당 2MB, Base64 총합 64Mi 문자, 전체 JSON 128MB, 날짜당 200블록을 검사합니다. 원본의 추가 필드·블록 ID·자유 좌표·사진 설명·내 레이아웃도 유지합니다.

## 라운지

기존 로컬 서버 주소·닉네임·초대코드로 접속합니다. [같은 Wi-Fi와 서버 실행](../docs/FREE-CHAT.md). Windows·Android 사용자와 같은 API를 쓰고 ⌘+Enter로 전송합니다. 초대코드·세션 토큰은 백업과 디스크에 저장하지 않습니다. 새 공개 서버나 유료 서비스를 배포하지 않습니다.

## 수정 위치와 검증

- 공통 메뉴·인사말: `shared/app-shell.json`, 색상·제한·성장 상수: `shared/desktop.json`, 경제 정책: `shared/collection.json` → `python tools/sync-desktop.py`.
- Mac 화면: `Sources/MyDay/UnifiedViews.swift`, `SavedLayouts.swift`. 그림 패키징: `tools/prepare-mac-assets.py`.
- Mac 성장·소품·경제: `SharedCompanion.swift`, 저장 연결: `CompanionStore.swift`.
- 공통 백업: `WindowsCompatibility.swift`. 실제 버전 8 왕복·쓰기 실패·이전 검증: `UnifiedVerification.swift`.

`build/MyDay.app/Contents/MacOS/MyDay --self-test`는 개인 일기를 변경하지 않는 임시 자료로 검사합니다. `--render-preview`는 넓은/좁은 일기와 상몬·형식·자유 배치 PNG를 만듭니다. Mac CI는 실제 앱을 빌드하고 Windows의 실제 직렬화 코드와 최신 백업 교환을 검사합니다. 실기기 창 조작·Intel 빌드·접근성은 별도 기기 검증 대상입니다.
