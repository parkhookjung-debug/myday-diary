# 상몬 게임 · 간식 산책

**2026-11-13 업데이트:** [상몬 플레이룸](SANGMON-PLAYROOM.md)에 콤보·최고점수·오늘의 도전·결과 카드와 슬링샷·낚시를 추가했습니다. 아래는 첫 초안의 기록이며 최신 사용법은 플레이룸 안내를 확인합니다.

2026-11-11 Windows 미니게임 초안입니다. 현재 0.15.0 공개 다운로드와 별도로 작업 브랜치에서 개발합니다. Mac·Android 게임 화면은 아직 구현하지 않았습니다.

## 플레이

- 일기 앱 왼쪽 **상몬 게임 · 간식 산책**, 또는 바탕화면 상몬의 우클릭 메뉴에서 엽니다.
- 개발용 단독 실행은 `MyDay.exe --snack-game`입니다. 같은 실행 파일을 `SangmonGame.exe`로 복사하면 더블클릭만으로 게임을 엽니다. 단독 모드는 일기 저장소를 읽거나 쓰지 않습니다.
- **게임 시작 / Space**로 시작하고, 마우스 좌우 또는 **← → / A D**로 상몬을 움직입니다. 상몬이 따라오는 속도에 상한이 있어 순간이동하지 않습니다.
- **사과·딸기·쿠키 +10점**. 놓친 간식은 감점하지 않습니다. **돌·가시는 피하기**. 부딪히면 목숨이 1개 줄고 잠깐 깜박입니다. 1.2초 동안 연속 피해를 막지만 간식은 계속 먹을 수 있습니다.
- 한 판 **60초**, 목숨 **3개**입니다. 시간이 끝나거나 목숨을 모두 쓰면 점수와 먹은 간식 수를 보여줍니다. 조금씩 낙하 속도와 빈도를 높입니다.
- **Space / 일시정지**로 멈추고 다시 누르면 계속합니다. **Esc**도 일시정지합니다. 다른 창으로 이동하거나 최소화하면 자동으로 멈추며 자동으로 재개하지 않습니다.
- **R / 처음부터 / 다시 도전**은 새 판입니다. 창을 닫으면 게임만 닫습니다.

초안에서는 점수를 현재 판에만 유지합니다. 점수를 일기 경험치·출석·재화·뽑기와 연결하지 않습니다. 기존 일기·사진·성장·채팅·백업 형식을 변경하지 않으며 새 패키지·게임 엔진·서버를 추가하지 않습니다.

## 파일별 수정 위치

| 수정할 것 | 파일 |
| --- | --- |
| 시간·목숨·점수·이동 속도·무적 시간·장애물 비율 | [shared/snack-game.json](../shared/snack-game.json) |
| 규칙 생성·검사 | [tools/sync-snack-game.py](../tools/sync-snack-game.py) → `python tools/sync-snack-game.py`, 검사는 `--check` |
| 낙하·충돌·점수·상태 전환 | [windows/Core/SnackGame.cs](../windows/Core/SnackGame.cs) |
| 게임 창·그림·키보드·마우스·자동 정지 | [windows/UI/SnackGameWindow.cs](../windows/UI/SnackGameWindow.cs) |
| 일기 앱 연결 | [windows/UI/DiaryWindow.Game.cs](../windows/UI/DiaryWindow.Game.cs), [DiaryWindow.Shell.cs](../windows/UI/DiaryWindow.Shell.cs) |
| 바탕화면 상몬 메뉴 | [windows/Character/DesktopPet.cs](../windows/Character/DesktopPet.cs) |
| 모델 검사·네이티브 화면 | [windows/Tests/SnackGameTests.cs](../windows/Tests/SnackGameTests.cs) |

생성 파일 `SnackRules.g.cs`를 직접 수정하지 않습니다. 앞으로 다른 OS로 옮길 때도 공통 JSON의 규칙을 기준으로 구현합니다. 그림과 입력은 운영체제별 화면에서 유지합니다.

## 개발과 검증

`windows/build.ps1`로 빌드합니다. 실행 중인 앱을 교체하지 않고 빌드하려면 `windows/build.ps1 -OutputDirectory build/snack-game-validation`을 사용합니다. 새 출력 폴더의 `MyDay.exe --self-test`로 검증합니다.

단위 검사는 시작 전 정지, 간식 세 종류·중복 점수 방지, 동시 장애물·무적·무적 중 먹기, 시간 정지·재개, 마우스 경계·잘못된 입력, 놓친 간식, 30/120FPS 일치, 게임 종료·재시작·60초 완료와 오브젝트 정리를 확인합니다. 기존 저장·성장·바탕화면 상몬 검사도 함께 실행합니다.

`--snack-game-preview <출력 폴더>`는 실제 게임 창의 시작·멈춤·재개·재시작·Space 길게 누르기·방향키를 놓았을 때 정지·작은 화면을 확인하고 PNG를 출력합니다. 전체 `--smoke-test`에도 포함합니다. 실제 Windows 게임 창에서 Space 재개와 방향키 이동을 확인했습니다.

[실제 렌더링 화면](../previews/snack-game-20261111/README.md)

키 처리 참고: [Microsoft WinForms 키보드 이벤트](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/input-keyboard/events). `SuppressKeyPress`로 KeyUp을 없애지 않고 `Handled`와 KeyPress 처리를 분리합니다.

최종 검증 완료: [Windows CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43037274582)에서 363개 assertion, 네이티브 일기·게임·키 이벤트·그림, 로컬 서버/Windows 채팅이 통과했습니다. [실행 파일의 출처·SHA256](builds/windows-snack-game-2026-11-11.json).
