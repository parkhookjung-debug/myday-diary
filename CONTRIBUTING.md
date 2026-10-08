# 함께 개발하기

MyDay는 Kotlin과 Jetpack Compose로 만드는 Android 일기 앱입니다.
기능 제안, 오류 제보, 문서 개선도 환영합니다.

## 첫 실행

1. 저장소를 clone하고 Android Studio에서 엽니다.
2. Android SDK 35와 JDK 17 이상을 준비합니다.
3. Android Studio의 Gradle Sync를 실행합니다. `local.properties`는 각자의 SDK 경로를 사용하고 커밋하지 않습니다.
4. `app`을 실행하고 README의 확인 흐름을 따라갑니다.

## 작업 순서

1. Issues에서 작업을 정하고 담당자를 지정합니다. 먼저 이슈에 작업 시작을 남겨 중복을 피합니다.
2. 최신 `main`에서 작업 브랜치를 만듭니다.
3. 한 가지 기능이나 오류 수정에 집중하고 작은 커밋으로 저장합니다.
4. GitHub에서 `main`을 대상으로 Pull Request(PR)를 만듭니다.
5. 팀원 한 명의 승인과 Android 자동 검사가 통과하면 squash merge합니다.

```sh
git switch main
git pull --ff-only
git switch -c feat/photo-block
# 코드 수정 후
git add <수정한 파일>
git commit -m "Add photo diary block"
git push -u origin feat/photo-block
```

브랜치 예: `feat/photo-block`, `fix/widget-date`, `docs/setup-guide`.
Codex 작업 브랜치는 `codex/`로 시작합니다.
공동 작업자는 초대를 수락한 뒤 브랜치를 push할 수 있습니다. 외부 기여자는 fork해서 PR을 보냅니다.
`main`에 직접 push하지 않고 PR로 반영합니다. 다른 사람의 브랜치에 강제 push하지 않습니다.

## 리뷰와 검증

- PR에 변경 이유, 확인 방법, 남은 한계를 적습니다.
- 화면 변경은 스크린샷을, 위젯·배경화면 변경은 기기 모델과 Android 버전도 첨부합니다.
- 일기 저장·날짜 이동에 영향을 주면 앱 재실행 후에도 기록이 유지되는지 확인합니다.
- 실제 사용자 일기, 사진, 이메일, 토큰, 서명 키를 예시나 커밋에 넣지 않습니다.
- SDK 및 Gradle 버전 변경은 팀원과 먼저 논의합니다.

```sh
./gradlew :app:assembleDebug :app:lintDebug :app:testDebugUnitTest
```

Windows PowerShell에서는 `./gradlew.bat`를 사용합니다.
저장 실패·날짜 이동·블록 재정렬·삭제 분리를 확인하는 단위 테스트가 있습니다.
저장·날짜·레이아웃 등 동작 로직을 확장할 때 관련 테스트를 추가하면 CI에서 자동 실행합니다.
자동 검사만으로 기기의 위젯·배경화면 동작이 검증되는 것은 아닙니다.
디자인 담당자는 [팀 디자인 적용 안내](docs/DESIGN.md)에서 수정 파일과 미리보기 방법을 확인합니다.

## 5명 팀의 담당 영역 제안

추천 배정과 담당 파일·첫 작업·리뷰 순서는 [5명 작업 분담안](docs/TEAM.md)에 정리했습니다.
실제 팀원이 정해지면 이슈의 Assignee로 관리합니다.

| 영역 | 작업 예시 |
| --- | --- |
| 통합·일정 | 이슈 우선순위, PR 조율, 배포 준비 |
| 일기 화면 | 블록 구성, 레이아웃 편집, 테마 |
| 기록·미디어 | 날짜별 저장, 사진 첨부, 데이터 보존 |
| 홈 화면 | 위젯, 캐릭터, 라이브 배경화면 |
| 테스트·문서 | 갤럭시 동작 확인, 접근성, 사용 안내 |

담당자는 자기 PR을 승인할 수 없으므로 서로 리뷰합니다.
새 작업자는 처음에 범위가 작은 이슈부터 진행합니다.

## 소통과 라이선스

리뷰는 코드와 동작을 대상으로 하고, 다른 기여자를 존중합니다.
진행 중인 작업의 변경이나 중단은 해당 이슈에 남깁니다.
이 프로젝트의 코드는 MIT 라이선스입니다. 기여 코드는 같은 라이선스로 제공하며,
외부 코드·이미지·폰트·캐릭터를 추가할 때 원 출처와 사용 조건을 확인하고 기록합니다.
