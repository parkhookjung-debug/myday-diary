# 함께 개발하기

MyDay는 Android의 Kotlin / Jetpack Compose 앱과 Windows의 C# / Windows Forms 앱으로 구성합니다. 수정할 플랫폼과 기능을 고른 뒤 [기능별 수정 가이드](docs/EDITING-GUIDE.md)에서 파일을 찾습니다. 버그 제보·디자인·문서 개선도 같은 방식으로 참여합니다.

[Windows 개발 안내](windows/README.md) · [Android 안내](docs/ANDROID.md) · [폴더 구조](docs/REPOSITORY-STRUCTURE.md) · [5명 역할·등록된 공동 작업자](docs/TEAM.md)

## 어떤 브랜치에서 시작하나요?

현재 통합 작업은 `codex/windows-app`에 있고 [PR #7](https://github.com/parkhookjung-debug/myday-diary/pull/7)에서 `main` 반영을 기다립니다. 이 기간에는 `origin/codex/windows-app`에서 자기 작업 브랜치를 만들고 PR의 대상도 `codex/windows-app`으로 선택합니다. 통합 PR이 병합된 뒤에는 최신 `origin/main`에서 시작하고 PR 대상으로 `main`을 선택합니다.

```sh
git fetch origin
git switch -c codex/my-change origin/codex/windows-app
# 필요한 파일을 수정합니다.
git add <수정한 파일>
git commit -m "Describe the change"
git push -u origin codex/my-change
```

`codex/my-change`는 예시 이름이므로 작업 내용을 나타내는 자기 브랜치 이름을 사용합니다. 작업을 정할 때 Issues에서 담당자를 지정하고 변경할 파일을 적어 같은 파일의 작업이 겹치지 않게 합니다. 외부 기여자는 fork에서 브랜치를 만들고 PR을 보냅니다.

## 파일을 고르는 기준

| 작업 | 먼저 볼 곳 | 함께 확인할 부분 |
| --- | --- | --- |
| Windows 화면·버튼·배치 | [UI 안내](windows/UI/README.md) | 데이터 필드나 저장 방식이 바뀌면 Core |
| Windows 기록·사진·검색·경험치·장비 | [Core 안내](windows/Core/README.md) | 화면 연결은 DiaryWindow의 기능별 파일 |
| 상몬·아이템 그림·움직임 | [Character 안내](windows/Character/README.md) | 이름·ID·장착 규칙과 화면 미리보기 |
| 분야별 일기 질문 | [Templates 안내](windows/Core/Templates/README.md) | 전체 목록·개수 검증·문서 |
| 갤럭시 화면·저장·위젯·배경화면 | [Android 패키지 안내](app/src/main/java/com/myday/diary/README.md) | 실제 갤럭시 동작과 Preview |
| 설치·사용·파일 안내 | [문서 목록](docs/README.md) | 실제 파일 링크와 현재 구현 상태 |

공통 파일의 큰 변경은 담당자끼리 먼저 범위를 맞춥니다. 실제 역할은 [팀 문서](docs/TEAM.md)에서 정하고, 담당 구분은 협업 기준으로 사용합니다.

## Windows 수정 후 확인

기존 앱을 알림 영역의 **모두 종료**로 끝내고 저장소 루트에서 실행합니다.

```powershell
./windows/build.ps1
$result = Start-Process windows/bin/MyDay.exe -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
if ($result.ExitCode -ne 0) { throw 'Windows 검사 실패' }
```

`windows/실행.cmd`는 이미 있는 EXE를 재사용하므로 코드 변경 뒤에는 다시 빌드해야 합니다. 새 `windows/bin/MyDay.exe`로 수정한 기능을 확인합니다. 화면 흐름 검증은 `--smoke-test windows/test-output`으로 격리된 기록을 사용합니다.

저장·사진·날짜·레이아웃·성장·장비 변경은 해당 기능 검증과 재실행·백업을 확인합니다. 그림·움직임 변경은 양쪽 방향과 작은 투명 창에서 잘리지 않는지 확인합니다. 관련 파일은 [수정 가이드](docs/EDITING-GUIDE.md)에 연결했습니다.

## Android 수정 후 확인

Android Studio에서 저장소 루트를 열고 JDK 17 이상과 Android SDK 35로 Gradle Sync를 합니다. Windows PowerShell에서는 다음 명령을 사용합니다.

```powershell
./gradlew.bat :app:assembleDebug :app:lintDebug :app:testDebugUnitTest
```

다른 운영체제에서는 `./gradlew`를 사용합니다. 화면은 [debug Preview](app/src/debug/java/com/myday/diary/ui/preview/DiaryPreviews.kt)로 먼저 보고, 홈 위젯·라이브 배경화면은 갤럭시에서 확인합니다. SDK·Gradle·라이브러리 변경은 통합 담당자와 범위를 맞춥니다.

## PR과 리뷰

PR에는 **어떤 동작이 바뀌는지, 수정한 파일, 확인 방법**을 적습니다. 화면 변경은 예시 화면을, 위젯·배경화면 변경은 기기와 Android 버전을 함께 적습니다. 관련 GitHub 자동 검사 결과를 확인하고 다른 팀원이 리뷰합니다. 작성자는 자기 PR을 승인하지 않습니다.

`main` 보호 설정은 팀원 리뷰 승인 1개와 Android 검사를 요구합니다. Windows 기능을 바꾸는 PR은 Windows 검사도 확인합니다. `main`에 직접 push하거나 다른 사람의 브랜치에 강제 push하지 않습니다.

## 파일과 배포

소스·문서·검증용 화면은 Git에 올립니다. 실행 파일은 [GitHub Releases](https://github.com/parkhookjung-debug/myday-diary/releases)에 플랫폼별로 올립니다. 빌드 출력·실제 일기·개인 사진·토큰·서명 키·기기별 `local.properties`는 커밋하지 않습니다.

문서 수정에는 새 실행 파일 배포가 필요하지 않습니다. 파일 이름을 바꾸거나 옮겼다면 README와 관련 가이드의 링크도 갱신합니다. 문서만 바꾼 경우에는 링크와 내용의 정확성을 확인합니다.

프로젝트는 MIT 라이선스입니다. 외부 코드·이미지·폰트·캐릭터를 추가하면 출처와 사용 조건을 함께 기록합니다. 리뷰는 코드와 동작을 대상으로 하며 변경 중인 작업의 범위는 이슈에서 공유합니다.
