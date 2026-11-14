# 0.15 일기와 성장 중심 간소화

2026년 11월 11일 사용자 요청을 반영합니다. 세 운영체제의 핵심 기능을 유지하면서 캐릭터 수집·꾸미기 화면과 배포 이미지 용량을 줄입니다.

| 유지 | 제거 |
| --- | --- |
| 일기·사진·할 일·습관·감정 기록 | 상몬 변신 64종 선택과 외형 해금 화면 |
| 자유 배치·일기 형식·내 레이아웃 | 소품 300종 코디·옷장·장착 화면 |
| 달력·검색·앨범·태그·기록 도구 | 뽑기·재화 표시·수집 도감·합성·교환·제작 화면 |
| 출석·기록 경험치·Lv.100 성장 | 배포 앱의 다른 캐릭터·소품·장착 이미지 |
| 기본 상몬 애니메이션·바탕화면·홈 위젯·햅틱 | 상몬·공방 메뉴 → 상몬 성장으로 교체 |
| 백업·복구·기기 이동·로컬 채팅 | 첫 실행 안내의 뽑기·재화 홍보 |
| 카카오 SDK·인증 및 친구 공유 연결 원본 | 공개 서버는 새로 배포하지 않음 |

## 실제 동작

상몬 성장 메뉴에서 기본 상몬 한 마리와 레벨·누적 경험치·다음 레벨까지 남은 경험치를 봅니다. 출석과 기록 활동을 저장하면 경험치를 얻습니다. 출석은 하루 한 번이며 저장 실패 시 경험치를 확정하지 않습니다. 기본 하루 최대 40 XP, 연속 7일마다 +10 XP, Lv.100 누적 3,590 XP 규칙을 유지합니다. 100·200·300일은 기존 활동 패턴에 따른 예상치입니다.

로컬 채팅과 카카오 연동은 사용자 최종 요청에 따라 유지합니다. 카카오 SDK와 콜백·인증·공유 원본은 삭제하지 않습니다. 공개 서버 연결은 이전 통합판과 동일하게 꺼져 있으므로 카카오 실제 로그인·업로드가 현재 운영 중이라는 뜻은 아닙니다. 기존 선택 공유 미리보기와 로컬 채팅을 사용할 수 있습니다.

## 이전 데이터와 호환

기존 백업 버전 1~9를 유지합니다. 예전 외형 ID·장비·수집 이력은 호환을 위해 모델과 검증기에 보관하지만 화면에서 선택하거나 변경하지 않습니다. 예전 백업을 열어도 기본 상몬만 표시합니다. 렌더링은 저장된 외형이나 경험치를 바꾸지 않습니다. 일기·사진·좌표·이력·성장과 이전 메타데이터를 새 저장·백업에서도 보존합니다.

카탈로그의 과거 ID 목록과 수집 검증 코드는 이 호환성을 위해 남깁니다. 원본 캐릭터·소품 그림과 개발용 내보내기 코드는 저장소의 이력을 유지하며, Android·Mac 배포 이미지에는 포함하지 않습니다. 개발자가 예전 자산을 다시 생성하더라도 패키징 허용 목록이 불필요한 이미지를 제외합니다.

## 수정할 파일

| 내용 | 위치 |
| --- | --- |
| 유지·제거 범위와 이미지 허용 목록 | [shared/product.json](../shared/product.json) |
| 공통 메뉴 | [shared/app-shell.json](../shared/app-shell.json) → sync-desktop.py |
| 공통 안내와 버전 | [experience.json](../shared/experience.json)·[journal-tools.json](../shared/journal-tools.json) → 해당 sync 도구 |
| Windows 성장 화면 | [GrowthWindow.cs](../windows/UI/GrowthWindow.cs)·[DiaryWindow.Growth.cs](../windows/UI/DiaryWindow.Growth.cs) |
| Windows 진입·트레이·바탕화면 | [DiaryWindow.Shell.cs](../windows/UI/DiaryWindow.Shell.cs)·[DesktopPet.cs](../windows/Character/DesktopPet.cs) |
| Android 성장 화면 | [PhoneCompanion.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneCompanion.kt) |
| Android 고정 캐릭터와 12MB 캐시 | [SangmonSprites.kt](../app/src/main/java/com/myday/diary/ui/character/SangmonSprites.kt) |
| Android 배포 이미지 | [app/build.gradle.kts](../app/build.gradle.kts)의 prepareFocusedAssets |
| Mac 성장·캐릭터 화면 | [UnifiedViews.swift](../macos/Sources/MyDay/UnifiedViews.swift) |
| Mac 배포 이미지 | [prepare-mac-assets.py](../tools/prepare-mac-assets.py) |
| 백업 호환·포장 검사 | [FocusedTests.cs](../windows/Tests/FocusedTests.cs)·[test_focused_product.py](../tools/test_focused_product.py)·Android 기기 검사 |

과거 0.14 안내와 발표 기초 자료는 당시 기능의 기록입니다. 현재 시연·발표에서는 이 문서의 제거 범위를 함께 반영하세요.

## 검증과 용량

Windows 328개 assertion·실제 화면 smoke·로컬 서버 및 두 Windows 클라이언트 통신 검사를 통과했습니다. Android APK·JVM 단위 테스트 56개·Lint 오류 0개·Android 35 기기 검사 19개·실제 Windows 백업 교환을 통과했습니다. Mac Apple Silicon 네이티브 빌드·화면 렌더링·로컬 채팅·Windows 백업 교환과 공통 계약 검사를 통과했습니다. 테스트의 세는 단위가 다르므로 합산한 총점으로 표현하지 않습니다. 실제 Galaxy 햅틱 느낌과 Intel Mac은 별도 실기기 확인 범위입니다.

로컬 한글 경로에서 발생한 JVM 클래스 로딩 문제는 동일 소스를 ASCII 경로에 복사해 검증했습니다. GitHub의 깨끗한 환경에서도 전체 검사를 통과했습니다. Mac·Android의 이전 꾸미기 자산과 안내를 검사하던 항목은 기본 상몬의 실제 렌더링·포장·호환성 검사로 교체했습니다.

| 배포물 | 이전 0.14 | 검증된 0.15 | 감소 |
| --- | --- | --- | --- |
| MyDay-Windows.exe | 614,400 bytes | 567,808 bytes | 7.6% |
| MyDay-macOS-arm64.zip | 17,407,263 bytes | 1,931,281 bytes | 88.9% |
| MyDay-Android.apk | 32,295,158 bytes | 15,974,676 bytes | 50.5% |

Android 그림 원본은 16,827,038 → 754,257 bytes입니다. 삭제한 자산이 APK 안에 남지 않도록 허용 목록으로 새 패키지를 구성합니다. [세 앱 실제 화면](../previews/focused-20261111/README.md)과 [소스·CI·SHA256](builds/myday-0.15.0.json)을 확인할 수 있습니다.

- [검증 실행 43023459628](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43023459628)
- [검증 실행 43026084065](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43026084065)
- [검증 실행 43025057825](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43025057825)
