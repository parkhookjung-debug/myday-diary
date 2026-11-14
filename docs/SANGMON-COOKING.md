# 상몬 불꽃 요리 · Windows 개발 버전

**최신 추가:** [상몬 블록 쌓기와 6종 플레이룸](SANGMON-BLOCKS.md). 공놀이는 플레이룸 아래 버튼으로 옮겼습니다. 아래는 요리 추가 단계의 안내와 검증 기록입니다.

2026-11-14. 상몬의 불 뿜기를 이용해 간식을 굽는 다섯 번째 게임입니다. 간식 산책·슬링샷·낚시·통통 점프와 함께 플레이룸에서 선택하며, 바탕화면 공놀이는 여섯 번째 카드로 옮겼습니다. 현재 공개 `v0.15.0-preview` Release와 별도인 `codex/windows-app` 작업 버전입니다. Mac·Android 게임 화면은 아직 구현하지 않았습니다.

## 실행과 조작

1. 일기 앱의 **상몬 플레이룸 → 불꽃 요리 → 놀러 가기**를 엽니다. 단독 실행은 `MyDay.exe --game-hub`, 바로 요리는 `MyDay.exe --cooking-game`입니다. `SangmonGame.exe` 별칭은 플레이룸을 바로 엽니다.
2. **요리 시작** 또는 첫 Space로 시작합니다. 그 다음 Space를 누르고 있거나 게임 화면/아래 **꾹 눌러 불 뿜기** 버튼을 마우스로 누르고 있으면 굽기가 진행됩니다.
3. 굽기 표시가 **황금색 구간에 들어올 때 놓으면 +30점**, 가운데 **초록색 구간에서 놓으면 +50점**입니다. 덜 익히거나 태우면 0점이며 이미 얻은 점수는 줄지 않습니다.
4. 쿠키·식빵·마시멜로를 순서대로 굽습니다. 주문마다 목표 위치와 굽는 속도가 달라집니다. 한 판은 **60초 또는 주문 8개**이며 최대 400점입니다. 불을 계속 뿜으면 100에서 타고, 자동으로 다음 간식을 굽지 않습니다. 새로 눌러야 합니다.
5. **P / Esc / 일시정지**로 멈추고 **P / 계속하기**로 돌아옵니다. 다른 창으로 이동해도 멈춥니다. 진행 중인 불은 꺼지고 해당 간식은 차감 없이 다시 구울 수 있습니다. 일시정지 시간은 제한 시간에 포함하지 않습니다.
6. **R / 처음부터**는 이번 판을 초기화합니다. 중간에 닫은 판은 최고점수·플레이 횟수에 기록하지 않습니다. 완료하면 **결과 카드 저장**으로 날짜·점수·완성/완벽 개수가 담긴 PNG를 저장할 수 있습니다. 카드에는 개인 일기·사진·바탕화면이 포함되지 않습니다.

키 자동 반복, 키보드로 누른 중 마우스 놓기, 마우스로 누른 중 Space 놓기, 캡처 해제, 다른 창 이동, 재시작 직후 오래된 놓기 입력으로 중복 채점하거나 불이 남지 않도록 처리했습니다. 상몬은 사용자가 누르고 있을 때만 요리 불을 뿜습니다. 기본 상몬 외형과 옆입을 유지하며 코디·변신을 추가하지 않습니다.

## 점수와 데이터

게임 기록은 `%LOCALAPPDATA%/MyDay/Windows/Games/scores.json`의 `bake` 항목입니다. 기존 네 게임과 함께 최고점수·최근점수·완료 횟수를 보관합니다. 게임 점수로 일기 경험치·출석·재화를 지급하지 않으며, 일기·사진·레벨·공통 백업 형식을 변경하지 않습니다. 같은 파일을 쓰는 다른 게임과 충돌하지 않도록 잠금 후 다시 읽고 원자적으로 교체합니다. 기록이 손상되면 원본을 보존하고 안내합니다. 새 ID를 모르는 옛 게임 실행 파일은 기록을 읽지 못할 수 있으므로 최신 개발 실행 파일을 사용하세요.

## 수정 파일

| 바꿀 내용 | 파일 |
| --- | --- |
| 시간·주문 수·굽기 속도·판정 폭·점수 | [shared/mini-games.json](../shared/mini-games.json) → `python tools/sync-mini-games.py` |
| 굽기·판정·주문·피드백·정지 상태 | [CookingGame.cs](../windows/Core/CookingGame.cs) |
| 화면·캐릭터 불꽃·표정·간식 그림·키/마우스 조작 | [CookingGameWindow.cs](../windows/UI/CookingGameWindow.cs) |
| 5개 게임 + 공놀이 선택 화면·작은 창 배치 | [GameHubWindow.cs](../windows/UI/GameHubWindow.cs) |
| 최고점수 파일과 `bake` ID | [GameRecords.cs](../windows/Core/GameRecords.cs) |
| 실행 옵션 | [Program.cs](../windows/Program.cs) |
| 규칙·입력·실제 화면·저장 검사 | [CookingGameTests.cs](../windows/Tests/CookingGameTests.cs) |

외부 라이브러리·다운로드 이미지·서버를 추가하지 않았습니다. 간식과 요리 배경은 기존 System.Drawing으로 그립니다.

## 검사와 화면

`./windows/build.ps1 -OutputDirectory build/cooking-validation`로 별도 폴더에 빌드합니다. `MyDay.exe --self-test`는 기존 검사를 포함해 448개 assertion으로 판정·점수 중복·자동 태움·재시작·정지·30/120FPS 일치·시간/주문 종료·새 기록 보존을 확인합니다.

`MyDay.exe --cooking-preview <출력 폴더>`는 메모리 기록으로 실제 WinForms 창을 열어 키 내려감/올라감, 혼합 입력, 마우스 캡처 해제, 창 비활성화, 과열 후 키 반복, 한 번만 점수 저장, 결과 PNG를 확인합니다. 전체 `--smoke-test`에도 포함되어 기존 일기·게임·공놀이를 함께 검사합니다.

[실제 폼과 게임 렌더러 화면](../previews/cooking-20261114/README.md). 개인 기록을 사용하지 않은 재현 가능한 검사 장면입니다.

최종 검증: [Windows CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43452732024)에서 448개 assertion, 기존 일기·게임·공놀이의 실제 화면과 로컬 채팅이 모두 통과했습니다. [실행 파일의 소스·크기·SHA256](builds/windows-cooking-2026-11-14.json). GitHub에서 실행 파일을 받으려면 이 CI의 **Artifacts → myday-windows-77**을 내려받아 압축을 풀고 `bin/MyDay.exe`를 실행하세요. GitHub 로그인이 필요하며 게임만 실행하려면 `MyDay.exe --game-hub`를 사용합니다. 현재 공개 Release의 첨부 파일은 바꾸지 않았습니다.
