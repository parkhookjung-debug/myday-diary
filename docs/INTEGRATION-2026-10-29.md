# 0.14 통합 미리보기 안내

2026년 10월 29일 `ceed3927`의 디자인 직접 푸시 `546d154`와 `cms6727`의 [PR 9](https://github.com/parkhookjung-debug/myday-diary/pull/9) 소스 `459cf43`을 현재 `codex/windows-app` 작업에 통합했습니다. 기존 개인 일기·사진·배치·성장·소품·경제 이력과 공통 백업 1~9를 유지합니다. main 병합이나 Releases 게시를 수행하는 작업은 아닙니다.

## 바로 써 보는 순서

1. 기존 앱에서 공통 JSON 백업을 앱 밖에 보관합니다. 앱을 완전히 종료한 뒤 해당 OS의 새 개발 빌드로 교체합니다. Android 앱 ID는 기존 `com.myday.diary`를 유지합니다. 설치 서명이 다르면 먼저 백업을 확인하며 기존 앱을 바로 삭제하지 않습니다.
2. Windows의 **형식 · 블록 추가**, Android의 **일기 형식 · 블록 추가**, Mac의 **일기 레이아웃 · 블록**에서 글·할 일·습관·감정·사진을 추가합니다. 기존 일기 형식 100종을 계속 검색할 수 있고 추천 20종을 별도로 고를 수 있습니다.
3. Windows와 Mac에서는 표정으로 마음을 고릅니다. 기존 감정 문자열과 저장 규칙은 같습니다. Android는 기존 마음 선택과 햅틱을 유지합니다.
4. Android의 **사진 여러 장 추가**에서는 최대 10장까지 한 번에 선택합니다. 각 사진의 제목·장소와 설명을 따로 적습니다. 사진 선택 중 날짜가 바뀌거나 저장이 끝나지 않았다면 추가를 거부하므로 원하는 날짜에서 다시 선택합니다.
5. Android의 **친구에게 공개**에서 아무 항목도 선택되지 않은 상태로 시작합니다. 직접 글·사진을 체크하고 **선택한 내용 전체 미리보기**로 확인합니다. 현재 통합판의 로그인·초대·공개 버튼은 동작하지 않으며 개인 기록을 외부로 보내지 않습니다. 라운지의 **채팅 라운지**는 기존 로컬 서버를 사용합니다.
6. Mac에서 선택한 일기 형식은 이후 새 날짜의 빈 구성을 만드는 기본값으로 기억합니다. 이미 저장된 날짜는 저장한 글·사진·블록 ID·좌표를 그대로 엽니다. 다른 형식을 추가해도 기존 빈 사용자 블록을 지우지 않습니다.

## 플랫폼별 적용 범위

| 기능 | Windows | Mac | Android |
| --- | --- | --- | --- |
| 전체 형식 100종과 추천 20종 | 지원 | 지원 | 지원 |
| 형식과 블록 추가 통합 | 지원 | 지원 | 지원 |
| 감정 표정 디자인 | 네이티브 버튼으로 추가 | 작성자 디자인 반영 | 기존 선택·햅틱 유지 |
| 날짜를 넘겨 쓸 기본 형식 기억 | 기존 내 레이아웃 수동 재사용 | 새 날짜 기본값 추가 | 기존 내 레이아웃 수동 재사용 |
| 사진 여러 장과 사진 제목 입력 | 기존 한 장 추가 유지 | 기존 한 장 추가 유지 | 최대 10장 선택과 제목 입력 |
| 친구 공개 선택 미리보기 | 미포함 | 미포함 | 기기 안에서 미리보기 |
| 공개 서버·카카오 로그인·실제 공유 | 비활성화 | 비활성화 | 비활성화 |

Mac의 영상 블록은 이 Mac의 로컬 원본 파일 경로를 참조하는 실험 기능입니다. 영상 자체는 복사·백업·전송하지 않습니다. 다른 OS에서는 설명 글로 표시될 수 있고 Mac 전용 경로 정보가 다시 저장할 때 유지된다고 보장하지 않습니다. 공통 사진 기능과 구분하며 중요한 영상은 원본을 별도 보관합니다.

## 기존 기능을 보존하며 조정한 부분

- 원본의 20종 축소 대신 전체 100종을 복원했습니다. 추천 ID만 `shared/desktop.json`의 `featuredTemplateIds`에서 공통 관리합니다.
- Mac이 과거 일기를 열 때마다 새 형식을 적용하던 처리를 새 날짜에만 적용하도록 바꿨습니다. 기존 블록과 ID·좌표는 자동 재구성하지 않습니다.
- 친구 브랜치의 별도 디버그 앱 ID 대신 기존 앱 ID를 유지했습니다. 앱 데이터 경로를 임의로 변경하지 않습니다.
- 공개 서버 URL·공개 키·카카오 키는 현재 APK에 빈 값으로 들어갑니다. `ONLINE_FRIENDS_ENABLED=false`이며 SDK 초기화와 콜백도 꺼집니다. 로컬 설정에 키가 남아 있어도 이 빌드에는 들어가지 않습니다.
- Supabase 함수·SQL·카카오 연결 코드는 추후 검토용 소스로 보존합니다. 이번 통합은 외부 프로젝트의 상태나 비용을 확인·변경하거나 새로 배포하지 않습니다. 이전 설정 문서의 배포 내용은 원 작성자의 실험 기록이며 현재 통합판의 동작 설명이 아닙니다.

## 수정할 파일

| 바꾸려는 것 | 파일 |
| --- | --- |
| 전체 형식과 추천 목록 | `shared/catalog/templates.json`, `shared/desktop.json` |
| 공통 형식·앱 버전 생성 | `tools/sync-desktop.py`, `shared/journal-tools.json`, `tools/sync-journal.py` |
| Windows 표정과 형식 창 | `windows/UI/MoodStrip.cs`, `TemplateGallery.cs`, `DiaryWindow.cs` |
| Mac 디자인·즐겨찾기·기본 형식 | `macos/Sources/MyDay/UnifiedViews.swift`, `MoodStickers.swift`, `Templates.swift`, `DiaryFormatPreset.swift` |
| Android 여러 사진·사진 제목 | `PhoneNotebookRoute.kt`, `PhoneBlockEditor.kt` |
| Android 친구 미리보기·연결 차단 | `PhoneCommunity.kt`, `PhoneFriendsSpace.kt`, `CommunityConnection.kt`, `app/build.gradle.kts` |
| 외부 연결 원본과 오프라인 정책 검사 | `supabase/`, `supabase/tests/`, `server/friends/` |

## 검사 상태

Windows 저장·성장·캐릭터 검사 325개와 실제 WinForms 화면 검사가 통과했습니다. 공통 생성·대비 검사 14개, 이전 로컬 친구 서버 모의 검사 10개, 격리된 PostgreSQL 정책 검사 30개와 모의 카카오 인증 검사 13개가 통과했습니다. 공개 서버 접속이나 실제 사용자 계정은 검사에 사용하지 않았습니다. Android·Mac의 통합 빌드와 기기 검사는 아래 결과가 확인되는 대로 기록합니다.

[실제 Windows 화면](../previews/integration-20261029/README.md) · [0.12 상세 사용 설명서](USER-MANUAL.md) · [통합 PR 7](https://github.com/parkhookjung-debug/myday-diary/pull/7)
