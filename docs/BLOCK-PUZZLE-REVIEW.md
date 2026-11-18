# 상몬 블록 퍼즐 · Windows 검토판

블록을 빈칸에 놓고 가로·세로 줄을 지우는 퍼즐을 Windows 플레이룸의 **일곱 번째 게임**으로 추가합니다. 기존 낙하형 상몬 블록 쌓기와 별개의 게임입니다. 외부 게임의 이미지·코드·상표 로고는 사용하지 않고 기존 MyDay 색상과 상몬 렌더러를 사용합니다.

## 검토용 게임 실행

`SangmonBlockPuzzle.exe`를 더블클릭합니다. 설치나 로그인 없이 이 게임 창만 열립니다. Windows 10/11과 .NET Framework 4.8 이상을 사용합니다. 소스를 빌드하거나 이 변경의 Windows CI 실행에서 Artifacts → `myday-windows-실행번호`를 받아 압축을 풀면 게임 실행 파일을 찾을 수 있습니다. 공개 0.15.1 Release와는 별도의 개발 기능입니다.

[실제 게임 화면과 플레이룸](../previews/block-puzzle-20261118/README.md)

검토판의 최고기록은 `%LOCALAPPDATA%/MyDay/BlockPuzzleReview/scores.json`에 저장합니다. 기존 일기 앱을 시작하거나 일기·사진·캐릭터 성장 기록을 읽지 않습니다. 진행 중인 보드는 저장하지 않으며, 게임이 끝난 판만 최고기록과 완료 횟수에 반영합니다.

## 조작과 규칙

- 아래 블록을 보드로 드래그해서 놓습니다. 먼저 블록을 클릭하고 빈칸을 클릭하는 방법도 됩니다.
- 키보드는 **1·2·3**으로 블록 선택, **방향키**로 위치 조정, **Enter / Space**로 놓기, **Esc**로 선택 취소입니다.
- 보드는 8×8이며, 블록은 회전하지 않습니다. 세 블록을 모두 사용하면 새 블록 셋이 나옵니다.
- 가로나 세로 한 줄을 채우면 그 줄을 지웁니다. 두 방향이 동시에 완성되면 한꺼번에 지우고 교차 칸은 한 번만 지웁니다. 다른 칸은 아래로 내려오지 않습니다.
- 놓은 칸마다 1점, 지운 줄 수를 L이라 할 때 `10 × L² × 연속 줄 삭제 횟수`를 더합니다. 줄을 지우지 않은 수는 연속 횟수를 초기화합니다. 보드를 비우면 50점 보너스입니다.
- 남아 있는 블록을 어디에도 놓을 수 없으면 끝납니다. 시간 제한은 없습니다.
- 게임 종료 후 결과 카드를 PNG로 저장할 수 있습니다. 진행 중 ‘새 게임’은 한 번 확인한 뒤 시작합니다.

## 앱에 연결된 위치

- `windows/Core/BlockPuzzle.cs`: 모양·배치·동시 줄 삭제·점수·콤보·게임 종료
- `windows/UI/BlockPuzzleWindow.cs`: 드래그·클릭·키보드·미리보기·결과 화면
- `windows/UI/GameHubWindow.cs`: 일곱 번째 카드
- `windows/UI/DiaryWindow.Game.cs`: ‘상몬 게임’ 선택 메뉴
- `windows/Core/GameRecords.cs`: 기존 점수 파일과 분리한 퍼즐 기록
- `windows/Program.cs`: `--block-puzzle`, `--block-puzzle-preview`, 단독 EXE 진입점
- `windows/Tests/BlockPuzzleTests.cs`: 규칙·1,800회 배치·입력·레이아웃·메뉴 연결 검사

앱 플레이룸에서 실행할 때는 기존 `Games/scores.json`을 바꾸지 않고 같은 폴더의 **block-puzzle.json**에 저장합니다. 따라서 퍼즐을 모르는 이전 앱이 여섯 게임 점수 파일을 읽을 때 영향을 받지 않습니다. 검토용 단독 실행의 기록과도 분리됩니다.

## 빌드와 검증

```powershell
./windows/build.ps1
windows/bin/MyDay.exe --self-test
windows/bin/MyDay.exe --block-puzzle-preview windows/test-output/puzzle
```

빌드하면 `MyDay.exe`와 게임만 여는 `SangmonBlockPuzzle.exe`가 함께 생성됩니다. 앱 전체로 열면 상몬 게임 메뉴와 플레이룸의 상몬 블록 퍼즐에서 진입합니다. 자동 Windows 화면 검사에도 퍼즐 조작 검사를 포함하며 CI 산출물에 게임 EXE와 검사 화면을 담습니다.

미리보기 명령은 개인 기록 대신 메모리 안의 기록을 사용합니다. 실제 WinForms 창에서 재현 가능한 테스트 배치를 만들고 드래그·클릭·키보드 배치, 잘못 놓기, 줄 삭제, 작은 창 배치, 결과 한 번 저장, 메뉴 진입을 확인하고 PNG를 만듭니다.

이 변경은 Windows 검토판입니다. Android·Mac 게임 화면이나 공개 배포 파일은 바꾸지 않습니다.
