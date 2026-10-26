# 0.12.0 — 안정성·사용 경험 정리

일기 → 꾸미기 → 상몬 성장 → 보관 흐름을 처음 사용하는 사람도 찾을 수 있게 정리했습니다. 새 외부 서비스·결제·공개 서버는 추가하지 않습니다.

## 1. 저장과 복구

세 앱은 기존 원자적 저장을 유지하고 검증된 **직전 저장본**으로의 명시적 복구를 제공합니다. 현재 기록이 손상되거나 직전 저장본만 남아 있으면 빈 일기로 덮어쓰지 않고 시작 오류를 표시합니다. 복구를 누르고 확인해야 되돌리며 현재 파일은 별도 이름으로 보존합니다. 복구 후보가 잘못되면 실행하지 않습니다. 직전 저장 이후 수정은 되돌아갑니다.

| 운영체제 | 직전 저장본 | 복구 위치 |
| --- | --- | --- |
| Windows | `diary.json.bak` | 시작 시 오류 안내 → 복구 확인 |
| Android | `notebook.previous.json` | 시작 오류 화면 → 직전 저장본으로 복구 |
| Mac | 날짜별 일기·상몬·공통 메타데이터의 `.previous` 파일 | 시작 오류 화면 → 직전 저장본으로 복구 |

Mac은 여러 파일을 사용하므로 **손상된 파일만 개별적으로** 되돌립니다. 전체 앱을 한 시점으로 되돌리는 트랜잭션은 아닙니다. 날짜별 파일이 한 번만 저장된 경우 직전 세대가 없을 수 있습니다. 직전 저장본은 개인 기록이므로 화면 PIN으로 암호화되지 않으며, 기존 자동 백업·이력·수동 백업과 목적이 다릅니다. 추가 보관 공간은 Windows·Android에서 전체 기록 한 세대, Mac은 해당 파일 한 세대입니다. 손상 원본 보관본은 자동 삭제하지 않습니다.

Windows의 수동 백업도 임시 파일을 완전히 쓴 뒤 교체합니다. 검증·쓰기 실패가 선택한 기존 백업 파일을 비우지 않습니다. 일기 꾸미기를 처음 사용한 새 기록을 버전 9로 내보낼 때 소품 소유권 초기화도 함께 처리합니다. 기존 백업 버전 1~9, 성장·뽑기 원장 규칙을 유지합니다.

## 2. 세 기기의 같은 기능 위치

기본 5개 메뉴를 유지하고 **설정·백업 → 처음 시작하기 · 사용 안내 / 하루 꾸미기 / 기록 도구** 경로를 맞췄습니다. 컴퓨터는 사이드바, Android는 하단 탭입니다.

사용 안내는 새 빈 사용자에게 처음 표시하고 이후 설정에서 다시 열 수 있습니다. 안내의 **글 일기 쓰기 / 사진 추가 / 하루 꾸미기**로 이동합니다. 완료 상태는 기기 내부에만 저장하고 백업에 넣지 않습니다. Mac 사진 바로가기는 사진 조각을 만든 뒤 그 조각의 사진 선택을 사용합니다.

## 3. 디자인 정리

기존 크림·올리브·라벤더 분위기를 유지하고 보조 글자 색을 `#70695E`로 맞췄습니다. 주요 6개 바탕에서 보조 글자 대비 4.5:1 이상을 자동 검사합니다. 안내 화면은 같은 제목·내용·정보 순서와 OS별 스크롤·입력 방식을 사용합니다. 전체 앱의 접근성 인증이나 전면 디자인 교체를 의미하지 않습니다.

## 4. 성장·뽑기 가시성

사용 안내의 **성장과 선물**에서 현재 레벨·XP·만렙까지 남은 XP·클로버·뽑을 수 있는 횟수·다음 1회에 부족한 양·미수집 보장까지 횟수를 보여줍니다. 기존 뽑기 화면에도 가능한 횟수·부족한 양을 표시합니다.

일상 65% / 반짝 25% / 별빛 9% / 꿈빛 1%, 하루 기본 최대 40 XP, 연속 7일 +10 XP, 만렙 3,590 XP를 유지합니다. 10번째 미수집 보장은 남은 등급에 따라 확률을 재분배하며 전부 수집한 뒤에는 일반 뽑기입니다. 클로버는 시작 40개 + 누적 XP + 성장 단계마다 5개 + 연속 7일마다 10개입니다. 만렙 뒤에도 기존 규칙대로 50 XP마다 성장 단계 재화 +5가 이어집니다.

아직 실제 사용자 기록을 수집하지 않았으므로 경험치·확률·보상을 임의로 재조정하지 않았습니다. 기존 시뮬레이션의 99일 / 200일 / 295일은 가정한 활동 패턴의 결과이며 실제 효과를 측정한 값이 아닙니다. 팀이 일주일 사용한 후 기록 부담·하루 뽑기 횟수·중복 빈도·원하는 소품 획득 경로를 확인해 변경 여부를 정합니다. 일기 내용이나 원장 파일을 제출할 필요는 없습니다.

## 5. 설치와 업데이트

기록 도구 → 업데이트에 **운영체제별 Actions 빌드로 바로 가는 버튼**을 추가했습니다. 자동 다운로드·자동 설치는 하지 않습니다. 최신 **성공한** 실행을 선택하고 Artifacts를 내려받습니다. GitHub 로그인이 필요할 수 있고 빌드 보관 기간이 지나면 다시 빌드해야 합니다.

| 운영체제 | 들어갈 곳 | 받을 파일 |
| --- | --- | --- |
| Windows | [Windows CI](https://github.com/parkhookjung-debug/myday-diary/actions/workflows/windows.yml) → `myday-windows-*` | `MyDay.exe` |
| Mac | [Desktop shared core and native chat](https://github.com/parkhookjung-debug/myday-diary/actions/workflows/chat-compatibility.yml) → `myday-chat-macOS-ARM64` | `MyDay.app` 배포 ZIP |
| Android | [Android CI](https://github.com/parkhookjung-debug/myday-diary/actions/workflows/android.yml) → `myday-debug-*` | `app-debug.apk` |

**공통 백업 저장 → 앱 완전 종료 → 새 앱으로 교체 → 글·사진·상몬·재화 확인** 순서를 사용합니다. Windows의 `%LOCALAPPDATA%/MyDay/Windows`, Mac의 `~/Library/Application Support/MyDay`, Android 앱 내부 저장소는 삭제하지 않습니다. Android는 앱 ID·서명이 일치해야 덮어 설치할 수 있습니다. 서명 불일치 시 백업 확인 없이 삭제하지 않습니다. Mac 개발 빌드는 Apple Silicon·macOS 13 이상용이며 Apple 공증 배포는 별도 작업입니다.

## 어느 파일을 수정하나요?

| 바꾸려는 것 | 공통 또는 Windows | Android | Mac |
| --- | --- | --- | --- |
| 안내 문구·기능 위치 | [shared/experience.json](../shared/experience.json), [생성 도구](../tools/sync-experience.py) | 같은 원본에서 생성 | 같은 원본에서 생성 |
| 첫 실행·안내 화면 | [WelcomeWindow.cs](../windows/UI/WelcomeWindow.cs), [WelcomeState.cs](../windows/Core/WelcomeState.cs) | [PhoneWelcome.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneWelcome.kt), [PhoneNotebookScreen.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneNotebookScreen.kt) | [ExperienceView.swift](../macos/Sources/MyDay/ExperienceView.swift), [UnifiedViews.swift](../macos/Sources/MyDay/UnifiedViews.swift) |
| 직전 저장본·복구 | [RecoveryStore.cs](../windows/Core/RecoveryStore.cs), [DiaryData.cs](../windows/Core/DiaryData.cs) | [RecoveryStore.kt](../app/src/main/java/com/myday/diary/data/RecoveryStore.kt), [DiaryStore.kt](../app/src/main/java/com/myday/diary/data/DiaryStore.kt) | [SafeStorage.swift](../macos/Sources/MyDay/SafeStorage.swift) |
| 색·메뉴·성장 규칙 | [shared/desktop.json](../shared/desktop.json), [생성 도구](../tools/sync-desktop.py) | 같은 공통 원본 | 같은 공통 원본 |
| 확률·보상 | [shared/collection.json](../shared/collection.json), [SangmonCollection.cs](../windows/Core/SangmonCollection.cs) | [CollectionRules.kt](../app/src/main/java/com/myday/diary/diary/CollectionRules.kt) | [SharedCompanion.swift](../macos/Sources/MyDay/SharedCompanion.swift) |
| 업데이트 버튼 | [JournalToolsWindow.cs](../windows/UI/JournalToolsWindow.cs) | [PhoneJournalTools.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneJournalTools.kt) | [JournalTools.swift](../macos/Sources/MyDay/JournalTools.swift) |

공통 원본을 고친 뒤 `python tools/sync-experience.py`, `python tools/sync-desktop.py`, `python tools/sync-journal.py` 중 해당 도구를 실행하고 생성된 파일도 함께 커밋합니다. 안내의 확률과 실제 확률이 달라지면 생성 검사가 실패합니다.

## 팀 사용성 점검

공동 작업자마다 자기 기기의 **샘플 일기**로 아래 순서대로 확인하고 [사용성 점검 이슈](https://github.com/parkhookjung-debug/myday-diary/issues/new?template=usability.yml)를 작성합니다. 개인 일기·사진·PIN·백업 파일은 올리지 않습니다.

1. 설치 → 첫 안내 닫기 → 설정에서 다시 열기
2. 글·사진·할 일 추가 → 날짜 이동 → 앱 종료·재실행 → 내용 확인
3. 자유 배치·상몬 방 꾸미기 → 재실행 후 위치 확인
4. 샘플 공통 백업 → 다른 OS에서 가져오기 → 사진·태그·소품·재화 확인
5. 실수로 조각 삭제 → 이력 복구 → 보상이 중복되지 않는지 확인
6. 7일 사용 후 찾기 어려운 메뉴·쓰기 부담·중복 소품 느낌과 수정 제안 기록

실제 개인 기록 파일을 고의로 손상시키지 않습니다. 손상·쓰기 실패 검사는 격리된 임시 데이터 자동 테스트에서 수행합니다. 실기기 강제 종료·OS 업데이트·갤럭시 햅틱·Intel Mac은 추가 수동 확인 범위입니다.
