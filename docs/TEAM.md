# 5명 작업 분담안

현재 분리한 코드 구조를 기준으로 담당 영역을 정했습니다.
담당 역할이 확정되기 전까지는 아래 역할 이름을 사용합니다.
담당 영역은 작업과 리뷰의 기준이며, 다른 사람의 코드를 수정하지 못하게 제한하는 규칙은 아닙니다.

## 추천 배정

| 담당 | 역할 | 주요 결과물 | 작업 이슈 |
| --- | --- | --- | --- |
| 프로젝트 제안자(너) | 기획·통합 | 팀 디자인/기능 기준 확정, 우선순위, 기능 연결, 통합 시연 | [#6](https://github.com/parkhookjung-debug/myday-diary/issues/6) |
| 팀원 A | 디자인·일기 화면 | 팀 디자인 반영, 카드·버튼·화면 배치, 입력 흐름 | [#1](https://github.com/parkhookjung-debug/myday-diary/issues/1) |
| 팀원 B | 기록·사진 | 사진 블록, 기록 보관, 재실행 후 데이터 유지 | [#2](https://github.com/parkhookjung-debug/myday-diary/issues/2) |
| 팀원 C | 위젯·캐릭터 | 홈 위젯, 움직이는 배경화면, 기기별 동작 수정 | [#3](https://github.com/parkhookjung-debug/myday-diary/issues/3) |
| 팀원 D | 테스트·문서 | 자동 테스트, 갤럭시 확인표, 설치·사용 안내 | [#4](https://github.com/parkhookjung-debug/myday-diary/issues/4) |

기능 구현 중 확인은 각 기능 담당자가 하고, D는 전체 동작과 재현 방법을 함께 확인합니다.
너도 통합 연결 코드를 맡으므로 다섯 명 모두 코드 또는 테스트·문서 결과물을 남깁니다.

## 담당 파일

Kotlin 경로의 기준은 `app/src/main/java/com/myday/diary/`입니다.

| 담당 | 먼저 수정할 파일 | 함께 상의할 변경 |
| --- | --- | --- |
| 너 | `MainActivity.kt`, `ui/diary/DiaryRoute.kt`, `app/build.gradle.kts`, `.github/workflows/android.yml` | 콜백 연결, 라이브러리 추가, SDK 변경 |
| A | `ui/design/`, `ui/components/`, `ui/diary/DiaryScreen.kt`, `DiarySections.kt`, `DiaryBlockCard.kt`, `DiaryDialogs.kt`, `app/src/main/res/values/colors.xml` | 저장 모델 변경은 B, 시스템 버튼 동작 변경은 너/C |
| B | `data/DiaryModels.kt`, `data/DiaryStore.kt`, `diary/DiaryController.kt`, 새 사진 저장 코드 | 사진 선택 버튼/블록 표시 방식은 A, 화면 연결은 너 |
| C | `DiaryWidgetProvider.kt`, `CharacterWallpaperService.kt`, `platform/HomeScreenActions.kt`, `ui/wallpaper/`, `ui/character/`, 위젯 layout/drawable/XML, `app/src/main/res/values/dimens.xml` | 캐릭터 그림·앱 표시·공통 색상은 A, 기록 필드 변경은 B |
| D | `app/src/test/`, `app/src/debug/java/com/myday/diary/ui/preview/DiaryPreviews.kt`, `README.md`, `docs/` | 화면 예시 데이터는 A, 기능 테스트 기준은 해당 기능 담당자 |

`DiarySections.kt`·`DiaryBlockCard.kt`·`DiaryDialogs.kt`는 모두 `ui/diary/` 안에 있습니다.
공통 파일을 두 사람이 동시에 크게 바꾸지 말고, 이슈에서 먼저 조율합니다.
여러 영역에 걸친 큰 기능은 표시/UI, 저장 로직, 연결, 테스트 작업으로 나눕니다.

## 첫 작업

| 담당 | 시작할 작업 | 완료 기준 |
| --- | --- | --- |
| 너 | 팀이 원하는 일기 화면 1개와 이번 버전 기능 목록 확정 | 디자인 기준과 완료할 기능이 이슈에 남아 있음 |
| A | 대표 색상·글꼴·카드 모양 적용 후 일기 화면 재배치 | Compose 미리보기와 갤럭시 화면을 팀 디자인과 비교 |
| B | 사진 선택·저장·재실행 조회 구현 | 사진을 다시 열 수 있고 기존 글/체크 기록이 보존됨 |
| C | 현재 위젯과 배경화면을 갤럭시에서 확인하고 오류 수정 | 탭·드래그·화면 전환·날짜 변경 확인 결과와 녹화 |
| D | 현재 기록 동작 확인표와 자동 테스트 보완 | 작성·수정·삭제 취소·날짜 이동·재실행 확인 결과 |

첫 버전에서는 사진까지 구현하고 영상·AI 대화·자유로운 블록 크기 조절은 후속 이슈로 둡니다.
새로운 큰 기능을 추가하려면 완료 기준과 남은 시간을 먼저 같이 확인합니다.

## 작업 순서와 연결

1. [디자인 분리 PR #5](https://github.com/parkhookjung-debug/myday-diary/pull/5)를 팀원이 리뷰하고 `main`에 반영합니다. 이 문서의 파일 구조는 해당 PR을 기준으로 합니다.
2. 너와 A가 기준 화면을 정합니다. 그동안 B는 사진 저장 방식을 정리하고, C/D는 현재 앱을 기기에서 확인할 수 있습니다.
3. B와 A가 사진 블록의 데이터·UI 인터페이스를 합의합니다. 기존 `type`과 `theme` 값은 임의로 바꾸지 않습니다.
4. 각자 최신 `main`에서 브랜치를 만들고 한 가지 변경만 PR로 올립니다.
5. 너는 연결 코드를 정리하고 D는 통합 동작을 확인합니다. 각 PR은 작성자 외 팀원 1명이 리뷰합니다.

## 서로 리뷰하기

- 디자인 변경: D가 작은 화면·큰 글꼴·입력 상태를 확인합니다.
- 저장/사진 변경: 너 또는 D가 데이터 보존과 재실행을 확인합니다.
- 위젯/캐릭터 변경: A가 모양을, D가 갤럭시 동작을 확인합니다.
- 통합/설정 변경: 관련 기능 담당자가 연결이 맞는지 확인합니다.
- D의 테스트·문서 변경: 해당 기능 담당자가 기대 결과와 설명을 확인합니다.

리뷰 담당자가 정해진 코드 소유자는 아닙니다. 시간이 맞지 않으면 다른 팀원이 리뷰합니다.
같은 영역의 기능과 테스트는 서로 협력하되 PR 작성자가 자기 PR을 승인하지 않습니다.

## 실제 담당자 등록

Windows 작업도 같은 역할 구분을 사용합니다. UI 담당은 `windows/UI/`, 기록 담당은 `windows/Core/`, 캐릭터 담당은 `windows/Character/`, 통합 담당은 `windows/Program.cs`·`build.ps1`·Windows CI, 검증 담당은 `windows/Tests.cs`·문서를 맡을 수 있습니다. 플랫폼 전환 시 담당자끼리 먼저 작업 범위를 정합니다.

| GitHub 계정 | 등록 상태 | 담당 역할 |
| --- | --- | --- |
| [parkhookjung-debug](https://github.com/parkhookjung-debug) | 저장소 소유자 | 기획·통합 |
| [ceed3927](https://github.com/ceed3927) | 2026-10-08 쓰기 권한 등록 확인 | 미정 |
| [mrsandwith-76](https://github.com/mrsandwith-76) | 2026-10-08 쓰기 권한 등록 확인 | 미정 |
| [hongmin060115-create](https://github.com/hongmin060115-create) | 2026-10-08 쓰기 권한 초대 발송, 수락 대기 | 미정 |
| 추가 팀원 1명 | GitHub 아이디 확인 대기 | 미정 |

초대한 팀원은 쓰기(Write) 권한으로 브랜치 작업, PR 작성 및 리뷰에 참여합니다.
초대를 수락하고 담당 역할을 정하면 해당 이슈의 Assignee를 지정하고 이 표를 갱신합니다.
파일 경계는 이 문서로 먼저 관리하고, 실제 계정에 대한 자동 리뷰 요청 규칙은 역할 확정 후 설정합니다.
