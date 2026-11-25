# 할 일 목록 통합 보완 — 2026-11-25

## 출처와 범위

GitHub 계정 **mrsandwith-76**이 `codex/windows-app`에 올린 다음 두 변경이 기준입니다.

- [14aa3f2: Update Todo list UI](https://github.com/parkhookjung-debug/myday-diary/commit/14aa3f213f5bb44916e684ac7b3fd07dcd6644e1)
- [b408c81: Add files via upload](https://github.com/parkhookjung-debug/myday-diary/commit/b408c81c02d1b07573e76ba836d2e32d6cc92e13)

새 할 일 기능은 유지하며 간소화 이전의 변신·코디·옷장 메뉴와 삭제된 API 참조를 제거했습니다. 삭제된 구현을 재생성하거나 과거 기능 전체를 되돌리지 않았습니다. 백업 호환을 위해 원래 남겨 둔 메타데이터와 관련 검사는 유지합니다.

원본 변경에서 빠졌던 일곱 게임 메뉴·상몬의 게임 열기 연결·게임 창 종료 처리·기념일 편집 저장 콜백도 보존했습니다. 후속 배포 요청에 따라 [0.15.3 시험판](https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.15.3-preview)으로 Windows·Mac·Android를 다시 빌드해 배포합니다. `main`에는 다운로드 안내 문서만 갱신하며 앱 코드 전체는 자동 병합하지 않습니다.

## 사용법

1. 일기에서 할 일 블록을 추가합니다. 새 목록은 넓은 카드로 시작합니다.
2. 입력창에 할 일을 쓰고 **추가** 또는 Enter를 누릅니다.
3. 각 행에서 내용을 고치고 체크박스로 완료를 표시합니다.
4. 중요도는 **상·중·하**로 선택합니다. 새 항목은 중입니다.
5. 행 오른쪽 ×로 해당 할 일만 삭제합니다. 카드 상단의 ×는 블록 전체 삭제입니다.
6. **목록 모두 완료**로 모든 항목을 한꺼번에 체크하거나 해제합니다.

내용과 체크 상태는 기존 자동 저장 흐름을 사용합니다. 오른쪽 실천 요약은 할 일 항목 수를 기준으로 계산합니다. 할 일 한 개를 완료하면 기존 하루 할 일 목표 경험치를 지급하며, 같은 날 여러 번 체크해도 보상을 중복 지급하지 않습니다. 중요도만 변경한 것은 새 기록 작성으로 계산하지 않습니다. 자유 배치·크기 조절은 기존 방식 그대로 사용할 수 있습니다.

목록은 최대 100개, 전체 내용은 기존 블록 한도인 UTF-16 100,000자입니다. 잘못된 중요도나 한도를 넘긴 가져오기 데이터는 저장 전 거부해 정상 파일을 덮어쓰지 않습니다.

## 저장과 호환

각 할 일 블록은 기존 `Text`·`Checked`를 유지하고 선택 필드 `TodoItems`·`TodoLayoutVersion`을 추가합니다. 항목의 필드는 `Text`, `Checked`, `Importance`이며 중요도 값은 중=0, 상=1, 하=2입니다. 기존 백업 버전 1~9를 유지합니다.

- 이전 문장형 할 일은 줄 단위 항목으로 변환하며 전체 체크 상태를 이어받습니다. 새 블록 생성 후 문장을 넣는 기존 호출도 저장 중 지워지지 않습니다.
- Windows는 항목 목록과 기존 평문·전체 체크 값을 함께 저장합니다. 이전 형식의 클라이언트가 평문을 수정하면 같은 문장을 찾아 체크·중요도를 보존하고 새 항목은 미완료로 만듭니다. 전체 체크 값만 바꾸면 모든 항목에 적용합니다.
- Android는 선택 필드를 읽고 다시 저장해 항목별 정보가 백업을 거치는 동안 사라지지 않도록 합니다. 화면은 기존 Android 편집기를 유지합니다.
- Mac의 기존 확장 필드 보존 방식을 사용합니다. 이번 작업에서 Mac 실행 검사는 하지 않았습니다.
- 저장용 레이아웃에는 실제 할 일 내용·체크 상태를 복사하지 않습니다. 기존 일기·사진·성장·로컬 채팅 데이터의 위치도 바꾸지 않습니다.
- 공통 기본 높이 420과 항목 한도 100은 `shared/desktop.json`에서 관리하고 C#·Swift·Kotlin으로 생성합니다. 이미 저장한 명시적 높이·위치는 유지합니다. 최초 배포 검사에서 발견한 OS 간 기본 높이 불일치를 공통 원본으로 해결했습니다.

## 수정할 파일

| 작업 | 파일 |
| --- | --- |
| Windows 할 일 목록 화면 | `windows/UI/BlockCard.cs` |
| 요약·자동 저장·기존 메뉴 연결 | `windows/UI/DiaryWindow.cs`, `windows/UI/DiaryWindow.Shell.cs` |
| 할 일 데이터와 이전 평문 호환 | `windows/Core/DiaryData.cs`, `windows/Core/DiaryTodos.cs` |
| 할 일 완료 경험치 | `windows/Core/SangmonGrowth.cs` |
| Android 백업 보존 | `app/src/main/java/com/myday/diary/data/DiaryModels.kt`, `NotebookJson.kt`, `diary/NotebookRules.kt` |
| 회귀 검사 | `windows/Tests/TodoTests.cs`, Android `TodoCompatibilityTest.kt` |
| 공통 높이·항목 한도 | `shared/desktop.json` → `python tools/sync-desktop.py` |

## 검증

- Windows 경고를 오류로 처리하는 빌드 통과.
- Windows 자체 검사 522개 통과. 기존 백업 호환 검사와 새 할 일 저장·거부·보상 검사 포함.
- 격리된 데이터로 네이티브 할 일 추가·편집·중요도·개별/전체 체크·삭제·렌더링 검사 통과.
- 기존 전체 화면·출석/성장·기념일·일곱 게임 네이티브 검사 통과.
- 실제 Windows 채팅 창과 임시 루프백 서버의 전송·재접속 검사 통과. 공개 서버는 사용하지 않음.
- 공통 생성 원본 일치 검사와 관련 테스트 9개, 간소화 제품 검사 3개 통과.
- Android 앱·단위 테스트 소스 컴파일 통과. `TodoCompatibilityTest` 3개 통과. 로컬 Gradle 실행기는 한글 경로에서 테스트 클래스를 찾지 못해, 동일한 컴파일 결과와 캐시 의존성을 영문 경로로 복사한 뒤 JUnit으로 실행했습니다. 전체 Android 패키징·실기기 검사 및 Mac 실행 검사는 이번 작업에서 수행하지 않았습니다.

## 0.15.3 배포 검증 완료

같은 빌드 소스 `49acf5b5b149ffc014eb75848432a41259056ebf`로 최종 CI를 모두 통과했습니다. 위 로컬 검사 제한과 별도로 다음 원격 검사를 완료했습니다.

- [Windows](https://github.com/parkhookjung-debug/myday-diary/actions/runs/45563569615): 522개 자체 검사·실제 네이티브 화면/입력·일곱 게임·기념일·로컬 채팅.
- [Android](https://github.com/parkhookjung-debug/myday-diary/actions/runs/45563573621): APK 빌드·JVM 60개·API35 에뮬레이터 기기 검사 19개·Lint 오류 0·Windows 백업 교환.
- [Mac·공통](https://github.com/parkhookjung-debug/myday-diary/actions/runs/45563577400): Apple Silicon 앱 빌드·화면 렌더링·공통 계약·Windows 백업 교환·로컬 채팅.

각 파일의 실제 소스·검사·크기·SHA256은 [배포 기록](builds/release-0.15.3-2026-11-25.json)에 보관합니다. 실제 Galaxy의 햅틱 느낌과 개별 사용자 Mac의 수동 사용 평가는 별도입니다. 발표 자료는 [최신 발표 원문](presentation/PRESENTATION-0.15.3.md)에서 확인합니다.
