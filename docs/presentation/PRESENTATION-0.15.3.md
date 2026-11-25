# 마이데이 0.15.3 발표 자료

2026-11-25 · 본문 21장 + 상세 부록 10장. 새 할 일 목록과 일곱 게임을 포함한 최신 배포 기준입니다.

[배포 다운로드](https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.15.3-preview) · [작업자 기여 근거](contributions-evidence.json) · [배포 파일·검증 출처](../builds/release-0.15.3-2026-11-25.json)

기존에 제외 요청한 평가 항목 표·플랫폼 구현 범위·공통 데이터/OS 화면 구조의 별도 슬라이드는 다시 넣지 않았습니다.

## 01. 마이데이 MyDay

상몬과 함께 기록하는 일상
자유 배치 일기와 데스크톱 펫

오픈소스 SW를 활용한 응용 프로그램 개발

0.15.3 할 일 목록 업데이트 / 발표 본문 21장 + 상세 설명 부록 10장

**발표자 메모**

마이데이는 내 방식으로 일기를 만들고 상몬과 함께 기록을 이어가는 프로그램입니다. 먼저 해결하려는 문제와 일기 기능을 설명하고, 상몬의 반응과 성장, 쉬어가는 게임을 소개합니다. 이어 오픈소스 활용, GitHub 협업, 검증 결과와 다음 과제를 말씀드리겠습니다. 게임은 Windows에서 제공합니다.
발표 운영: 본문 1~21장을 사용하고 상세 사용 설명 22~31장은 질문에 맞춰 선택합니다.
기준: https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.15.3-preview

## 02. 문제와 해결 방향

사용자가 겪는 불편

정해진 양식에 기록을 맞춰야 한다.

일기를 꾸준히 쓰기 어렵다.

사진과 과거 기록을 다시 찾기 번거롭다.

마이데이의 구현 방향

글과 사진 블록을 자유롭게 배치

기본 상몬과 출석, 일기 작성 경험치

달력과 검색, 백업과 복구

목표 사용자: 원하는 형식으로 일상을 기록하고 싶은 학생과 일반 사용자

**발표자 메모**

처음에는 학습 집중도와 가계부 등 여러 주제를 검토했지만, 원하는 양식으로 일기를 만들고 싶다는 요구를 중심으로 프로젝트를 정했습니다. 기록 형식을 고정하지 않고 글, 사진, 할 일, 습관, 감정 블록을 배치하게 했습니다. 상몬과 경험치는 기록을 다시 시작할 계기를 제공하는 장치입니다. 실제 습관 개선 효과는 아직 사용자 연구로 측정하지 않았습니다.
기획 및 범위: https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/FOCUSED-APP.md

## 03. 자유 배치 일기와 기록 관리

글, 사진, 할 일과 감정 기록

마우스로 이동하고 크기 조절

100개 템플릿과 내 양식 저장

달력과 검색으로 기록 찾기

휴지통, 기록 이력과 복구

**발표자 메모**

실제 Windows 앱의 예제 데이터 화면입니다. 왼쪽에서 날짜를 고르고 중앙에서 기록합니다. 블록을 끌어 위치를 바꾸고 크기를 조절할 수 있습니다. 사진도 별도 블록으로 추가합니다. 템플릿 100개 중 선택하거나 자신만의 배치를 저장할 수 있습니다. 기록을 찾는 달력과 검색, 삭제 실수를 되돌리는 휴지통과 이력도 연결했습니다. 감정 기능은 감정 기록이며 AI 상담 기능을 구현한 것은 아닙니다.
화면: previews/focused-20261111/windows-example.png
https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/windows

다음 연결: 기록하는 화면을 살펴봤으니, 일기 창 밖에서는 상몬을 어떻게 만나는지 소개하겠습니다.

## 04. 할 일 목록과 중요도 관리

한 블록에 여러 할 일을 적고, 항목별로 진행 상황을 관리

추가 · 편집 · 삭제

입력 후 Enter로 추가
행에서 내용을 바로 수정

중요도와 완료 표시

상 · 중 · 하 선택
개별 체크와 전체 완료

기록과 성장 연결

내용 자동 저장
하루 완료 보상 중복 방지

원본 UI: mrsandwith-76 · 통합과 검증: parkhookjung-debug / GitHub 기록 기준

**발표자 메모**

2026-11-25 mrsandwith-76의 할 일 UI 두 커밋을 검토해 통합했습니다. 한 블록에서 여러 항목을 추가하고 각각 수정, 체크, 삭제할 수 있으며 중요도는 상·중·하입니다. 새 항목은 중으로 시작합니다. 전체 완료와 개별 완료를 함께 제공하고 오른쪽 실천 요약도 항목 수를 기준으로 계산합니다. 할 일 하나를 새로 완료하면 기존 하루 할 일 보상 5 XP에 연결하며 반복 체크로 중복 지급하지 않습니다. 중요도만 바꾸는 것은 새 글 작성으로 보상하지 않습니다. 기존 문장형 할 일과 백업을 보존하고 Android 백업에서 체크·중요도가 사라지지 않도록 보강했습니다. 새 목록 UI는 Windows에 먼저 적용했습니다. 변신·코디·옷장을 되살리지 않았습니다.
원본: https://github.com/parkhookjung-debug/myday-diary/commit/14aa3f213f5bb44916e684ac7b3fd07dcd6644e1
https://github.com/parkhookjung-debug/myday-diary/commit/b408c81c02d1b07573e76ba836d2e32d6cc92e13
검토·수정: https://github.com/parkhookjung-debug/myday-diary/commit/5edb1f2e25e0caa3d3432c6eb73fe8db6f196275
화면은 개인 일기가 아닌 격리된 예제 데이터입니다.
다음 연결: 기록하는 화면을 본 뒤, 일기 창 밖의 상몬 반응을 소개합니다.

## 05. 바탕화면 상몬의 조작과 반응

일기 밖에서도 만날 수 있는 캐릭터로 친근하게 접근
평소에는 숨쉬기, 눈 깜박임과 휴식 등 작은 동작으로 반응

| 사용자 동작 | Windows에서 보이는 반응 |
| --- | --- |
| 가볍게 클릭 | 짧은 불꽃 반응과 일기 창 열기 |
| 몸을 잡고 드래그 | 움직이는 방향으로 몸이 늘어나며 따라오기 |
| 빠르게 움직이다 놓기 | 마우스 속도에 비례해 같은 방향으로 날아가기 |
| 화면 작업 영역의 가장자리에 충돌 | 잠깐 찌그러지고 튕긴 뒤 점차 감속 |
| 숨긴 상몬 다시 표시 | 트레이 MyDay의 캐릭터 표시 메뉴 사용 |

**발표자 메모**

이 장은 일기 기능과 성장·게임을 잇는 상몬 소개입니다. 일기 창 밖에서도 상몬을 만나고, 클릭하면 일기를 열 수 있습니다. 핵심은 드래그할 때 늘어나고 빠르게 놓으면 속도에 비례해 날아가며 화면 가장자리에서 튕기는 반응입니다. 발표에서는 모든 표 행을 읽지 않고 클릭, 드래그, 던지기 세 가지를 중심으로 설명합니다.
평상시에는 숨쉬기, 눈 깜박임과 주변 보기, 휴식 같은 작은 동작을 합니다. 계속 불을 뿜지 않으며 직접 건드릴 때 짧게 반응하고 하품 때 별도의 입을 추가하지 않습니다. Windows에서 몸을 잡아 이동하면 늘어나고 멈추면 변형이 완화됩니다. 빠르게 놓으면 마우스의 최근 이동 속도에 비례해 날아가지만 최대 속도 제한은 있습니다. 화면 작업 영역에 부딪히면 반동이 생기고 중력과 저항으로 점차 멈춥니다. 일반 프로그램의 창을 물리 충돌 대상으로 삼지는 않습니다. 상몬을 다시 잡으면 비행을 멈출 수 있습니다. 숨기기는 종료와 다릅니다. 숨긴 상몬은 트레이에서 다시 표시하고 완전히 끝내려면 모두 종료를 선택합니다. Android는 위젯의 탭 반응, 라이브 배경화면과 선택 가능한 햅틱을 사용하며 런처마다 터치 전달 방식이 다릅니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/DESKTOP-PET-MOTION.md
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/FOCUSED-APP.md
다음 연결: 이렇게 반응하는 상몬과 일기 쓰기를 연결한 것이 출석과 성장입니다.

## 06. 상몬과 함께 이어가는 기록 습관

기록 활동으로 경험치 적립

출석과 일기 작성 보상을 연결
하루 보상은 최대 40 XP
7일 출석 보너스 10 XP

최고 레벨 100
필요한 누적 경험치 3,590 XP

활동량에 따라 약 100~300일을 목표로 설계
보통 사용량은 약 200일

**발표자 메모**

출석과 일기 작성을 성장 보상에 연결했습니다. 하루 보상은 최대 40 경험치이고 7일 출석 보너스가 10입니다. 레벨 100까지 필요한 누적 경험치는 3590입니다. 사용 패턴 시뮬레이션에서는 적극적인 사용 99일, 평균 사용 200일, 느슨한 사용 295일 정도로 계산됩니다. 이는 보상 규칙을 검증한 설계 수치이고 실제 이용자의 장기 유지율을 측정한 결과는 아닙니다. 사용자가 제거를 요청한 변신, 코디, 뽑기와 재화 화면은 현재 제품 범위에서 제외했습니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/product.json
https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/shared

다음 연결: 성장은 기록 활동으로 쌓고, 게임은 일기와 구분해 잠깐 쉬어가는 기능으로 구성했습니다.

## 07. 상몬과 쉬어가는 일곱 게임

상몬과 잠깐의 휴식

간식 산책
상몬 슬링샷
느긋한 낚시
통통 점프
불꽃 요리
상몬 블록 쌓기
상몬 블록 퍼즐

공 가져오기 별도 제공
게임 점수와 일기 XP는 별도

**발표자 메모**

Windows 플레이룸은 일곱 게임입니다. 기존 간식 산책, 슬링샷, 낚시, 점프, 불꽃 요리와 낙하형 블록 쌓기를 유지하고 8x8 배치형 블록 퍼즐을 추가했습니다. 새 퍼즐의 점수는 별도 파일로 분리해 기존 최고 기록을 보존합니다. 공놀이는 별도 활동이며 게임 점수는 일기 경험치와 연결하지 않습니다. 실제 통합 소스를 빌드해 격리된 테스트 자료로 촬영한 화면입니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/BLOCK-PUZZLE-INTEGRATION-2026-11-23.md

발표 운영: 게임을 별도 주제로 길게 설명하지 않고, 같은 상몬으로 서로 다른 조작을 즐길 수 있다는 점을 보여 줍니다. 다음 일곱 화면은 게임당 약 12~15초로 목표와 대표 조작만 짚습니다. 상세 점수 규칙은 질문 때 설명합니다.

## 08. 간식 산책

목표

간식을 먹고
장애물 피하기

조작

마우스 좌우 이동
방향키 또는 A·D

진행과 점수

60초, 목숨 3개
간식 +10점
연속 5개마다 보너스

**발표자 메모**

발표용 핵심 설명: 간식을 먹고 장애물을 피하는 게임입니다. 마우스나 방향키로 좌우 이동합니다.
상세 설명과 근거:
상몬을 좌우로 움직여 사과, 딸기와 쿠키를 먹습니다. 돌과 가시에 닿으면 목숨이 줄어듭니다. 간식을 놓치거나 피해를 받으면 현재 콤보가 끊깁니다. 플레이룸의 오늘의 같은 배치 도전에서는 날짜별로 정한 동일한 순서를 재도전할 수 있습니다. Space는 시작과 일시정지, R은 새 판입니다.

화면 출처: 0.15.2 Windows 앱의 격리된 검사 데이터로 촬영한 실제 화면입니다. 점수와 최고 기록은 검사용 예제이며 개인 데이터가 아닙니다.
기능 근거: https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.3-preview/docs/SANGMON-PLAYROOM.md
검증 실행: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143148077

## 09. 상몬 슬링샷

목표

상몬을 날려
고리 통과하기

조작

몸을 뒤로 당긴 뒤 놓기
점선으로 방향 확인

진행과 점수

60초, 발사 기회 10번
고리 5개 통과
중앙 통과 시 추가 점수

**발표자 메모**

발표용 핵심 설명: 상몬을 뒤로 당겼다가 놓아 고리를 통과시킵니다. 늘어나는 몸과 반동을 게임에 활용했습니다.
상세 설명과 근거:
상몬을 뒤로 당기면 반대 방향으로 발사합니다. 점선은 첫 충돌 전의 예상 경로입니다. 화면 가장자리와 바닥에서 튕기며 고리 통과는 100점, 중심 통과는 150점입니다. 바탕화면 상몬의 속도 비례 던지기와는 조작 방향이 다릅니다. 잡은 상태에서 창을 벗어나거나 정지하면 발사를 취소하고 기회를 소비하지 않습니다.

화면 출처: 0.15.2 Windows 앱의 격리된 검사 데이터로 촬영한 실제 화면입니다. 점수와 최고 기록은 검사용 예제이며 개인 데이터가 아닙니다.
기능 근거: https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.3-preview/docs/SANGMON-PLAYROOM.md
검증 실행: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143148077

## 10. 느긋한 낚시

목표

입질을 기다렸다가
타이밍에 맞춰 낚기

조작

Space 또는 물가 클릭
입질 후 초록 칸에서 누르기

진행과 점수

한 판 60초
성공 +30점
중앙에 맞추면 +50점

**발표자 메모**

발표용 핵심 설명: 입질을 기다린 뒤 초록 구간에 맞춰 누르는 낚시입니다. 타이밍에 집중하는 느긋한 게임입니다.
상세 설명과 근거:
Space나 물가 클릭으로 낚싯줄을 던지고 입질 표시를 기다립니다. 입질에 한 번 눌러 게이지를 연 다음 움직이는 표시가 초록 칸에 왔을 때 한 번 더 누릅니다. 성공은 30점, 중심은 50점입니다. 화면은 입질이 발생한 실제 검사 장면입니다. 낚시에서 Space는 동작 버튼이므로 P나 Esc로 일시정지합니다.

화면 출처: 0.15.2 Windows 앱의 격리된 검사 데이터로 촬영한 실제 화면입니다. 점수와 최고 기록은 검사용 예제이며 개인 데이터가 아닙니다.
기능 근거: https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.3-preview/docs/SANGMON-PLAYROOM.md
검증 실행: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143148077

## 11. 통통 점프

목표

장애물을 뛰어넘고
공중의 간식 먹기

조작

Space, 위 방향키
또는 마우스 클릭

진행과 점수

60초, 목숨 3개
장애물 통과 +20점
간식 +10점

**발표자 메모**

발표용 핵심 설명: 다가오는 장애물을 타이밍에 맞춰 뛰어넘습니다. 점프와 착지 때 상몬의 몸이 변형됩니다.
상세 설명과 근거:
달려오는 장애물을 타이밍에 맞춰 넘는 게임입니다. 점프할 때 길어지고 착지할 때 잠깐 납작해지는 상몬의 반응을 넣었습니다. 공중에서 다시 점프할 수 없고 키를 계속 누르고 있어도 자동으로 다시 뛰지 않습니다. 피해 없이 넘긴 장애물에 20점, 간식에 10점을 줍니다. P나 Esc로 일시정지하고 R로 새 판을 시작합니다.

화면 출처: 0.15.2 Windows 앱의 격리된 검사 데이터로 촬영한 실제 화면입니다. 점수와 최고 기록은 검사용 예제이며 개인 데이터가 아닙니다.
기능 근거: https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.3-preview/docs/JUMP-AND-DESKTOP-FETCH.md
검증 실행: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143148077

## 12. 불꽃 요리

목표

상몬의 불꽃으로
간식을 알맞게 굽기

조작

Space나 마우스를 꾹 누르기
목표 구간에서 놓기

진행과 점수

60초 또는 주문 8개
황금 구간 +30점
초록 구간 +50점

**발표자 메모**

발표용 핵심 설명: 누르면 불을 뿜고 놓으면 굽기를 멈춥니다. 목표 구간에 맞춰 간식을 완성합니다.
상세 설명과 근거:
쿠키, 식빵과 마시멜로를 순서대로 굽습니다. 누르고 있을 때만 불을 뿜으며 게이지가 황금 구간에 왔을 때 놓으면 30점, 초록 중심은 50점입니다. 덜 익히거나 태우면 그 주문은 0점이고 이미 받은 점수는 줄지 않습니다. 주문마다 목표 위치와 굽는 속도가 달라집니다. 기본 상몬의 일상 반응에서 계속 불을 뿜는 기능과는 구분되는 게임 조작입니다.

화면 출처: 0.15.2 Windows 앱의 격리된 검사 데이터로 촬영한 실제 화면입니다. 점수와 최고 기록은 검사용 예제이며 개인 데이터가 아닙니다.
기능 근거: https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.3-preview/docs/SANGMON-COOKING.md
검증 실행: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143148077

## 13. 상몬 블록 쌓기

목표

내려오는 상몬 블록으로
가로 한 줄 채우기

조작

방향키로 이동과 회전
Space로 바로 내리기

진행과 점수

다음 블록 3개와 착지 안내
블록 5개마다 속도 증가
줄을 지워 점수 획득

**발표자 메모**

발표용 핵심 설명: 내려오는 블록을 이동하고 회전해 가로 줄을 채웁니다. 상몬 모양 블록으로 만든 낙하형 게임입니다.
상세 설명과 근거:
기존 테트리스형 낙하 게임입니다. 상몬 네 마리로 구성한 일곱 모양을 쌓습니다. 위나 X로 회전하고 Z로 반대 회전, 아래로 빠르게 내리고 Space로 즉시 내려놓습니다. 벽과 바닥 주변 회전, 예상 착지 위치와 다음 세 블록을 제공합니다. 낙하 속도는 놓은 블록 다섯 개마다 증가하며 줄 수에 따른 점수 레벨과 구분합니다. 화면의 2450점 최고 기록은 검사용 예제입니다.

화면 출처: 0.15.2 Windows 앱의 격리된 검사 데이터로 촬영한 실제 화면입니다. 점수와 최고 기록은 검사용 예제이며 개인 데이터가 아닙니다.
기능 근거: https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.3-preview/docs/SANGMON-BLOCKS.md
검증 실행: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143148077

## 14. 상몬 블록 퍼즐

목표

8×8 보드에 블록을 놓아
가로와 세로 줄 완성

조작

블록을 끌어 놓기
또는 선택 후 빈칸 클릭

진행과 점수

세 블록 사용 후 새 세트
시간 제한과 회전 없음
콤보와 전체 비우기 보너스

**발표자 메모**

발표용 핵심 설명: 빈 보드에 블록을 직접 놓아 가로와 세로를 채웁니다. 앞 게임과 달리 시간 제한과 회전이 없는 배치형 퍼즐입니다.
상세 설명과 근거:
cms6727의 PR #11을 통합한 일곱 번째 게임입니다. 기존 낙하형 게임과 달리 빈칸에 블록을 직접 놓습니다. 세 블록을 모두 사용하면 새 세트를 받습니다. 가로와 세로를 동시에 지우고 교차 칸은 중복 삭제하지 않습니다. 남은 블록을 어느 곳에도 놓을 수 없으면 끝납니다. 키보드는 1,2,3 선택, 방향키 이동, Enter나 Space 배치, Esc 취소입니다. 점수는 Games/block-puzzle.json에 따로 보관해 기존 게임 기록을 보존합니다.

화면 출처: 0.15.2 Windows 앱의 격리된 검사 데이터로 촬영한 실제 화면입니다. 점수와 최고 기록은 검사용 예제이며 개인 데이터가 아닙니다.
기능 근거: https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.3-preview/docs/BLOCK-PUZZLE-INTEGRATION-2026-11-23.md
검증 실행: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143148077

다음 연결: 사용자에게 보이는 기능을 살펴봤습니다. 이제 이 기능을 구현하는 데 어떤 오픈소스를 활용했는지 설명하겠습니다.

## 15. 오픈소스를 적용한 실제 기능

라이브러리를 실제 사용자 기능에 연결하고 플랫폼 차이에 맞춰 적용했습니다.

| 라이브러리 | 앱에서 활용한 기능 | 적용 코드 |
| --- | --- | --- |
| Compose / Material 3 | Android 일기 UI와 자유 배치 화면 | PhoneNotebookScreen.kt<br>PhoneFreeBoard.kt |
| Activity / Lifecycle | 사진 선택과 화면 상태 관리 | Android 화면과 ViewModel |
| Kotlin Coroutines | 저장과 사진 처리, 비동기 작업 | Android 비동기 처리 코드 |
| ZXing Core 3.5.3 | Android 기기 연결 QR 생성과 해석 | PhoneDeviceLink.kt |
| Nayuki QR Code generator | Windows용 QR 생성 로직을 이식 | windows/Core/DeviceQr.cs |

**발표자 메모**

가장 직접적으로 활용한 오픈소스 사례입니다. Android의 Compose와 Material 3는 일기 화면과 자유 배치 UI를 구성합니다. Activity와 Lifecycle은 시스템 사진 선택 및 화면 상태 관리에 활용합니다. Coroutines는 저장과 사진 처리를 비동기로 실행합니다. 기기 연결 QR은 Android에서 ZXing을 쓰고 Windows에서는 Nayuki의 QR 생성 알고리즘을 제한된 바이트 모드에 맞춰 이식했습니다. Mac의 QR은 운영체제 CoreImage 기능입니다. 같은 기능이라도 적용 기술과 라이선스 근거를 구분했습니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/build.gradle.kts
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/Core/DeviceQr.cs
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/presentation/open-source-inventory.csv

## 16. 라이선스와 활용 방식

WinForms와 SwiftUI는 운영체제 플랫폼 API로 구분
카카오 SDK 연동 소스는 유지하되 공개 로그인과 업로드는 비활성

| 구성 요소 | 라이선스 | 관리 방식 |
| --- | --- | --- |
| AndroidX / Material / Coroutines | Apache 2.0 | Gradle 의존성과 버전 기록 |
| ZXing Core 3.5.3 | Apache 2.0 | 원본 라이선스와 사용 위치 기록 |
| Nayuki QR 생성 코드 | MIT | 이식 코드와 저작권 고지 보존 |
| JUnit 4.13.2 | EPL 1.0 | 테스트에 사용 |
| MyDay 자체 소스 | MIT | 저장소 LICENSE로 공개 |

**발표자 메모**

오픈소스 활용에서는 무료라는 이유만으로 오픈소스로 분류하지 않았습니다. AndroidX와 Material, Coroutines 및 ZXing은 Apache 2.0, Windows에 이식한 Nayuki 코드는 MIT이며 고지 파일을 보존합니다. JUnit은 EPL 1.0으로 테스트에서 사용합니다. 자체 소스는 MIT로 공개합니다. .NET Framework 4.8의 WinForms와 macOS SwiftUI는 운영체제 플랫폼 API로 별도 구분합니다. 카카오 SDK의 소스와 서비스 이용 조건 역시 구분해야 하며 공개 연결은 현재 비활성입니다. 캐릭터와 외부 이미지의 이용 권한은 코드 라이선스와 별도로 관리해야 합니다.
공식 라이선스: https://github.com/androidx/androidx/blob/androidx-main/LICENSE.txt
https://github.com/zxing/zxing/blob/zxing-3.5.3/LICENSE
https://github.com/Kotlin/kotlinx.coroutines/blob/1.7.3/LICENSE.txt
https://github.com/nayuki/QR-Code-generator/blob/master/Readme.markdown
프로젝트 고지: https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/Core/DeviceQr.LICENSE.txt
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/LICENSE

다음 연결: 오픈소스의 이용 조건을 구분했듯, 팀 작업도 변경 출처를 확인하며 GitHub에서 통합했습니다.

## 17. GitHub 협업과 변경 통합

브랜치에서 작업하고 변경 내용을 검토한 뒤 앱에 반영했습니다.

원본 검토 → 간소화 기준 확인 → 기존 기능 보존 → 빌드·검사 → 배포

| 협업 기록 | 변경 내용 | 현재 상태 |
| --- | --- | --- |
| PR #8 / ceed3927 | 디자인과 macOS 관련 제안 | 검토 후 Windows에 이식 |
| PR #9 / cms6727 | Android 사진과 친구 공유 소스 | 통합 완료 · 공개 연결 비활성 |
| anniversary-calendar | 기념일 · 게임 메뉴 · 블록 개선 | 원본 검토 후 기존 앱에 반영 |
| PR #10 / 통합 작업 브랜치 | 통합 검증과 다운로드 안내 | 시험판 배포 · main 코드 미병합 |
| PR #11 / cms6727 | 8×8 상몬 블록 퍼즐 | 일곱 번째 게임으로 통합 |
| 11/25 / mrsandwith-76 | 항목별 할 일과 중요도 UI | 0.15.3 반영 · 제거 기능 제외 |

**발표자 메모**

팀원 작업을 무조건 병합하지 않고 현재 기능과 데이터 보존 조건을 검토했습니다. ceed3927의 PR 8은 디자인 자료를 참고해 Windows 화면으로 이식했으며 PR 자체가 병합됐다는 의미는 아닙니다. cms6727의 PR 9는 병합했고 공개 서버 연결은 꺼 둡니다. hongmin060115-create가 실제 푸시한 기념일 브랜치는 원본 변경을 검토해 반영했습니다. 통합 작업은 codex/windows-app에서 진행하고 PR 10과 시험판 릴리스로 공유했습니다. PR 11은 원본 코드와 저장 호환성을 검토한 뒤 작업 브랜치에 병합했습니다. 0.15.2의 일곱 번째 게임이며 기존 테트리스형 블록 쌓기도 유지합니다.
https://github.com/parkhookjung-debug/myday-diary/pull/8
https://github.com/parkhookjung-debug/myday-diary/pull/9
https://github.com/parkhookjung-debug/myday-diary/pull/10
https://github.com/parkhookjung-debug/myday-diary/pull/11
https://github.com/parkhookjung-debug/myday-diary/tree/codex/anniversary-calendar
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/branch-integrations.json
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/BLOCK-PUZZLE-INTEGRATION-2026-11-23.md

## 18. 팀원별 역할과 확인된 결과


| GitHub 계정 | 확인된 작업 | 기여 근거 |
| --- | --- | --- |
| parkhookjung-debug | 기획 · 통합 · 검증 · 배포 | PR #10 · 통합/릴리스 기록 |
| ceed3927 | 디자인 · Mac 일기 화면 | PR #8 · 디자인 직접 푸시 |
| hongmin060115-create | 기념일 · 게임 메뉴 · 블록 개선 | anniversary-calendar 푸시 |
| cms6727 | Android 사진/공유 · 블록 퍼즐 | PR #9 · PR #11 |
| mrsandwith-76 | Windows 할 일 목록 UI | 14aa3f2 · b408c81 (11/25) |

**발표자 메모**

다섯 계정의 확인된 결과물을 정리했습니다. mrsandwith-76은 11월 25일 Windows 할 일 목록 UI 변경 두 커밋을 올렸고 통합 과정에서 간소화 이전 기능 참조와 저장 호환 오류를 수정했습니다. hongmin060115-create는 커밋 author만이 아니라 실제 브랜치 푸시 기록을 근거로 표시했습니다. 오프라인 작업은 팀원 확인 후 추가할 수 있습니다. 기획·통합에는 AI 코딩 도구 활용도 포함되며 모든 코드를 수작업으로 작성했다는 의미는 아닙니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/TODO-INTEGRATION-2026-11-25.md
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/TEAM-UPDATES-2026-11-23.md

## 19. 검증과 배포로 확인한 완성도

Windows 통합본 검사

Android JVM 테스트

Android 기기 테스트

0.15.3 세 플랫폼 CI 통과 / 검증 단위는 구분해서 표시

| 검증 환경 | 확인한 연결 동작 |
| --- | --- |
| Windows | 새 할 일 목록 · 일곱 게임 · 펫 · 백업 · 채팅 |
| Android API 35 | JVM 60 · API35 기기 19 · Lint 오류 0 · 백업 호환 |
| macOS Apple Silicon | Apple Silicon 빌드 · 공통 배치 규칙 · 백업 · 채팅 |

**발표자 메모**

0.15.3 실제 배포 소스 49acf5b의 검증 결과입니다. Windows는 522개 자체 검사와 새 할 일 목록 및 기존 일기·일곱 게임·기념일·펫의 네이티브 화면과 입력 검사를 통과했습니다. Android JVM은 기존 57개에 새 할 일 백업 호환 검사 3개를 더한 60개입니다. Android 35 에뮬레이터 기기 검사 19개와 Lint 오류 0, Windows 백업 왕복도 확인했습니다. Mac은 Apple Silicon 앱 빌드·네이티브 렌더링·로컬 채팅·공통 계약과 Windows 백업 교환을 통과했습니다. 테스트의 단위가 달라 합산하지 않습니다. 처음 검사에서 할 일 기본 높이의 OS 간 불일치를 발견해 shared 공통 원본으로 수정하고 같은 소스로 전부 재검증했습니다. 실제 사용자의 습관 개선 효과를 입증하는 테스트는 아닙니다.
Windows: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45563569615
Android: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45563573621
Mac·공통: https://github.com/parkhookjung-debug/myday-diary/actions/runs/45563577400
파일 출처: https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.15.3-preview/SOURCE.json

## 20. 현재 한계와 다음 작업

현재 확인해야 할 범위

게임과 기념일 편집은 Windows 중심

Android는 시험용 서명의 APK

Mac은 Apple Silicon과 macOS 13 이상

공개 채팅 서버와 자동 동기화 미제공

우선순위가 높은 후속 작업

실제 사용자와 할 일 작성 흐름 점검

Galaxy와 Mac에서 사용성 확인

새 할 일 UI의 다른 기기 적용 검토

백업 교환과 일곱 게임 조작 점검

기록 습관 개선 효과는 실제 사용자 평가가 필요합니다.

**발표자 메모**

게임과 기념일 편집 UI는 Windows에 먼저 제공합니다. Android는 시험용 서명이고 Mac은 Apple Silicon과 macOS 13 이상용 시험판입니다. 공개 채팅 서버와 자동 동기화는 제공하지 않습니다. cms6727의 PR 11은 이번 0.15.3에 통합했습니다. 다음 작업은 사용자 시연으로 일기와 일곱 게임의 조작 흐름을 점검하는 것입니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/BLOCK-PUZZLE-INTEGRATION-2026-11-23.md

다음 연결: 다음 과제는 기능 수를 늘리는 것보다 기록 흐름과 실제 조작을 사용자에게 검증하는 데 우선순위를 둡니다.

## 21. 마이데이 MyDay

내 방식으로 남기는 일기
상몬과 함께하는 기록 습관

소스 코드와 협업 기록

github.com/parkhookjung-debug/myday-diary

0.15.3 통합판 다운로드

Windows EXE

macOS ZIP

Android APK

감사합니다. 질문을 받겠습니다.

**발표자 메모**

마이데이는 사용자가 직접 일기 형태를 만들고 기본 상몬과 함께 기록을 이어가는 프로그램입니다. 새 할 일 목록과 중요도를 포함해 0.15.3으로 배포했습니다. 오픈소스로 화면과 QR 기능을 구현하고 GitHub 브랜치와 PR로 디자인 및 기능을 통합했습니다.
발표 구성: 1~2장 기획, 3~6장 일기·할 일·상몬·성장, 7~14장 게임, 15~16장 오픈소스, 17~18장 협업, 19~21장 검증·개선·마무리. 22~31장은 질의응답용 상세 부록입니다. 2분 기능 시연 영상에서는 게임을 생략하고 기록·배치·상몬·성장·백업 흐름을 소개합니다. 영상은 이전 0.15.2에서 촬영해 새 할 일 목록 화면을 포함하지 않습니다.
배포: https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.15.3-preview

## 22. 상몬 앱의 전체 사용 흐름

날짜별 기록을 만들고, 상몬과 함께 관리하는 일기 프로그램

발표 본문은 1~21장 / 22~31장은 상세 사용 설명과 질의응답 자료

| 기능 위치 | 사용자가 하는 일 | 앱이 처리하는 결과 |
| --- | --- | --- |
| 일기 | 날짜 선택 후 글, 사진과 체크 기록 | 날짜별 내용과 배치를 기기에 저장 |
| 달력과 검색 | 날짜, 검색어와 분류로 기록 찾기 | 일치하는 날짜의 원래 일기로 이동 |
| 상몬 성장 | 오늘 출석과 기록 활동 확인 | 저장된 활동에 경험치 지급 |
| 라운지 | 로컬 서버에 입장하고 대화 | 같은 서버 참여자와 텍스트 교환 |
| 설정과 백업 | 설정 변경, 백업 저장과 가져오기 | 기기 설정 관리와 기록 이동 |

**발표자 메모**

이 부록은 앱을 처음 사용하는 사람이 실제 조작과 저장 범위를 이해하도록 구성했습니다. 먼저 날짜를 골라 일기 조각을 추가하고 자동 저장을 확인합니다. 나중에는 달력과 검색으로 기록을 찾아봅니다. 출석과 의미 있는 기록은 상몬 성장에 연결합니다. 라운지와 설정은 기록 편집과 별도 메뉴입니다. 기본 기능은 개인 기기에서 동작하며 모든 기록을 인터넷에 자동 업로드하지 않습니다. 본 발표의 구성은 1~21장을 사용하고 이후 자료는 기능 설명이나 질문에 맞춰 선택해 보여 줍니다.
현재 범위: https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/FOCUSED-APP.md
메뉴: https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/app-shell.json

## 23. 일기 블록과 자동 저장

입력, 체크와 배치 변경 후 자동 저장
다른 날짜로 이동하기 전 저장 완료 상태 확인

| 블록 종류 | 사용 방법 | 남는 기록 |
| --- | --- | --- |
| 글 일기 | 자유롭게 본문 입력 | 그날의 글과 제목 |
| 사진 | 사진 선택 후 설명 입력 | 변환된 사진 사본과 설명 |
| 할 일 | 여러 항목 추가 · 개별/전체 체크 | 내용 · 체크 · 중요도 (Windows) |
| 습관 | 실천한 행동을 적고 체크 | 해당 날짜의 실천 기록 |
| 감정처리반 | 상몬에게 털어놓듯 마음 입력 | 작성한 글, AI 답변 기능 없음 |

**발표자 메모**

일기는 한 덩어리의 긴 글만 쓰는 방식이 아니라 여러 조각을 조합합니다. 글은 본문, 사진은 파일과 설명, 할 일과 습관은 체크 상태까지 저장합니다. 습관 블록은 그날의 실천 기록이며 미래 모든 날짜에 자동 반복되는 예약은 아닙니다. 감정처리반은 글을 남기는 기능이고 AI 답변이나 진단을 하지 않습니다. 입력과 배치 변경은 자동 저장되지만 저장 중과 저장 실패를 구분해야 합니다. 오류가 나타나면 다시 저장한 뒤 날짜를 바꿉니다. 빈 칸을 추가하거나 배경만 바꾸는 것은 성장 보상 대상이 아닙니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-5
현재 범위: https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/product.json
0.15.3 Windows 할 일은 항목별 내용·체크·중요도를 함께 저장합니다. Android와 Mac은 기존 편집기를 유지하면서 새 메타데이터를 백업에 보존합니다.

## 24. 100개 형식과 자유 배치, 내 레이아웃

시작할 구성을 선택

일기 형식에서 목적별 템플릿 선택
공부, 회고, 감사, 여행 등 질문과 블록 구성 제공

위치와 크기를 직접 조정

PC는 제목 영역 드래그와 모서리 크기 조절
Android는 배치 편집에서 길게 눌러 이동

원하는 구성을 내 레이아웃으로 저장

블록 종류와 위치를 다른 날짜에 재사용
글과 실제 사진은 복사하지 않고 새로 작성

**발표자 메모**

무엇을 적을지 막막하면 100종의 형식 중 목적에 맞는 것을 선택합니다. 형식은 제목과 질문, 블록 종류와 배치의 초안입니다. AI가 내용을 대신 작성하는 기능은 아닙니다. 기존 기록이 있는 날에는 원래 내용을 보존하면서 형식의 새 블록을 추가하는 흐름입니다. 자유 배치에서는 PC의 제목 이동 영역을 잡고 끌거나 모서리로 크기를 조절합니다. Android는 배치 편집을 켜고 길게 눌러 옮기며 두 손가락으로 화면을 이동할 수 있습니다. 완성한 구성을 내 레이아웃으로 저장하면 다른 날짜에 틀을 재사용합니다. 실제 본문과 사진을 그대로 복제하는 것이 아니라 새 글과 빈 사진 자리로 시작합니다. 화면의 친구에게 공개 버튼은 외부 서버가 비활성인 통합판에서 공개 전송을 의미하지 않습니다.
화면: previews/focused-20261111/android-free-layout.png
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-7
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-9

## 25. 사진 첨부와 사진 꾸미기

사진을 기록으로 남기는 순서

1. 사진 블록을 추가하고 파일 선택
2. 사진 설명을 쓰고 위치와 크기 조정
3. 사진 꾸미기에서 편집 후 적용

편집 기능

좌우 회전, 중앙 정사각형과 4:3 자르기
크림, 라벤더와 숲 프레임
꽃, 하트, 별과 테이프 스티커

원본은 유지하고 사진 사본 저장 / 동영상 첨부는 미제공

**발표자 메모**

사진 블록을 만든 뒤 운영체제의 파일 또는 사진 선택기에서 파일을 고릅니다. 설명은 본문 검색과 함께 나중에 찾는 데 사용됩니다. 회전과 중앙 자르기, 테두리와 사진 위 스티커를 적용할 수 있습니다. 자유롭게 영역을 그리는 자르기가 아니라 중앙 정사각형과 4대3 자르기입니다. 이 사진 적용을 눌러야 결과가 반영됩니다. 취소하면 기존 일기 사진을 유지합니다. 원본 파일을 바꾸지 않고 앱에서 쓰는 JPEG 사본으로 저장하므로 원본 사진 보관 도구와 용도가 다릅니다. GIF 움직임 보존과 동영상 첨부는 제공하지 않습니다. 이 사진 편집 화면은 0.12에서 촬영한 기존 기능의 검사 화면이며 현재 간소화 범위에서도 사진 편집은 유지합니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-10
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/FOCUSED-APP.md

## 26. 달력, 검색과 기록 분류

기록을 다시 찾는 방법

달력 날짜를 눌러 일기 열기

본문과 사진 설명으로 검색

태그와 즐겨찾기로 분류

앨범에서 월별 사진 확인

Windows 기념일: 매년 반복 또는 한 번만 표시

**발표자 메모**

달력은 기록 날짜를 선택하는 입구입니다. 검색어를 입력하면 글과 사진 설명에서 일치한 날짜를 찾고 원래 일기로 이동합니다. 태그와 즐겨찾기는 날짜별 분류이므로 본문 검색과 역할이 다릅니다. 감정 달력에서는 사용자가 저장한 마음을 색과 함께 봅니다. 이 값은 AI가 분석한 감정이 아닙니다. 사진 앨범은 월별 사진을 모아서 원래 기록으로 이동하게 합니다. Windows 0.15.1에서는 기념일 이름과 날짜를 추가해 매년 반복 또는 일회성으로 표시할 수 있고 달력의 별과 설명으로 확인합니다. Mac과 Android는 기념일 데이터 보존을 지원하지만 동일한 편집 화면은 아직 없습니다.
화면: previews/release-0151-20261116/windows-integrated-browser.png
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-19
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/TEAM-UPDATES-2026-11-16.md

## 27. 출석과 레벨업의 세부 규칙

각 조건은 하루 한 번 / 같은 백업을 다시 가져와도 XP 재지급 없음
선택한 일기 날짜가 아니라 실제 활동일 기준 / Lv.100 누적 3,590 XP

| 보상 활동 | 경험치 | 지급 조건 |
| --- | --- | --- |
| 오늘 출석 | 5 XP | 출석 버튼을 누르고 저장 성공 |
| 첫 의미 있는 기록 | 10 XP | 새 글, 사진 또는 완료 체크 저장 |
| 글 20자 이상 | 10 XP | 실제 작성 내용과 분량 조건 충족 |
| 사진 첨부 | 5 XP | 실제 사진 추가 또는 교체 저장 |
| 할 일 완료 / 습관 완료 | 각 5 XP | 할 일 한 항목 또는 습관 새 완료 |
| 연속 7일 출석 | 10 XP | 기본 하루 40 XP와 별도 보너스 |

**발표자 메모**

경험치는 실제 기록 활동을 저장한 뒤 확정합니다. 출석은 앱 실행만으로 지급하지 않고 버튼을 눌러야 합니다. 첫 의미 있는 기록과 글 분량 조건은 각 10이고, 사진과 할 일, 습관은 각각 5입니다. 합하면 기본 하루 최대 40입니다. 각 조건은 같은 날 한 번만 지급하고 연속 7일 출석 보너스는 별도 10입니다. 과거 날짜 여러 개를 오늘 수정해도 오늘 보상 조건은 중복되지 않습니다. 백업 가져오기는 이미 받은 보상을 다시 주지 않습니다. 레벨은 100, 누적 3590을 기준으로 하며 사용 패턴에 따라 약 100일, 평균 약 200일, 느슨한 활동 약 300일을 목표로 설계했습니다. 경험치는 글의 질을 AI로 평가한 점수가 아닙니다.
현재 보상 설명: https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/experience.json
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/FOCUSED-APP.md
0.15.3 Windows 목록에서는 모든 항목을 끝내기 전에도 한 개를 새로 완료하면 하루 할 일 목표가 인정됩니다. 개별 체크나 중요도를 반복 변경해 같은 날 보상을 늘릴 수 없습니다.

## 28. 로컬 채팅과 카카오 연동의 범위

로컬 라운지 입장

운영자가 PC에서 로컬 서버 실행
서버 주소, 닉네임과 초대코드 입력
같은 서버 참여자와 텍스트 대화

현재 서비스 범위

개인 일기와 사진은 자동 전송 안 함
공개 서버와 일대일 메시지는 미제공
카카오 인증과 공유 소스는 유지
공개 로그인과 업로드는 비활성

**발표자 메모**

라운지는 운영자가 실행한 로컬 서버의 공용 대화방입니다. 참여자는 서버 주소와 닉네임, 초대코드를 입력해 입장합니다. 다른 기기에서는 127.0.0.1이 아닌 운영 PC의 실제 사설 IP를 사용해야 하며 같은 Wi-Fi와 방화벽 조건을 확인합니다. Windows는 Enter 전송과 Shift+Enter 줄바꿈, Mac은 보내기 또는 Command+Enter 전송, Android는 보내기 버튼을 사용합니다. 텍스트는 최대 2000자입니다. 개인 일기나 사진을 자동으로 채팅에 올리지 않습니다. 친구 목록, 일대일 메시지, 채팅 사진 첨부와 공개 서버는 현재 범위가 아닙니다. 카카오 SDK와 인증 및 공유 연결 소스는 보존했지만 공개 서버 연결을 끈 상태이므로 실제 카카오 로그인과 업로드가 운영 중이라고 설명하지 않습니다. 화면은 예제 닉네임과 메시지로 로컬 연결을 검사한 장면입니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-26
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/FOCUSED-APP.md

## 29. 백업, 복구와 개인정보 보호

같은 날짜는 가져온 기록이 우선할 수 있으므로 가져오기 내용을 확인
PIN, 알림과 채팅 기록은 공통 일기 백업과 구분

| 기능 | 사용 방법 | 작동 범위 |
| --- | --- | --- |
| 공통 백업 | 설정과 백업에서 JSON 내보내기 | 일기, 사진, 배치와 성장 자료 보관 |
| 다른 기기로 이동 | 백업 가져오기 또는 같은 Wi-Fi 연결 | 확인 후 가져오기, 자동 동기화 아님 |
| 휴지통과 수정 이력 | 날짜와 저장 시각을 골라 복원 | 보관된 이전 기록으로 되돌리기 |
| 자동 백업 | 기기 내부 사본 상태와 성공 시각 확인 | 기기 고장에 대비한 외부 백업도 필요 |
| PIN 화면 잠금 | PIN 설정 후 지금 잠그기 | 화면 접근 제한, 파일 암호화 아님 |

**발표자 메모**

설정과 백업에서 공통 JSON을 내보내면 글과 사진, 자유 배치와 성장 등 기록 자료를 별도로 보관할 수 있습니다. 다른 기기에서는 먼저 현재 기록을 백업한 뒤 가져올 파일과 겹치는 날짜를 확인합니다. 같은 날짜는 가져온 일기가 우선하는 규칙이며 문장 단위로 자동 합치지 않습니다. 같은 Wi-Fi에서 QR이나 주소로 백업을 전달해도 앱에서 가져오기를 확정하는 과정은 별도입니다. 휴지통과 이력은 삭제나 변경 전 보관된 내용을 복원합니다. 모든 키 입력이나 모든 과거 상태를 무제한 보관하지 않습니다. 자동 백업은 같은 기기 내부 사본이므로 기기 분실과 디스크 고장에는 별도 보관이 필요합니다. PIN은 앱 화면 잠금이고 저장 파일이나 내보낸 JSON을 암호화하지 않습니다. PIN과 알림 설정, 채팅 DB와 게임 점수는 공통 일기 백업과 별도입니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-21
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-22
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-25

## 30. 설치부터 첫 일기까지

첫 사용 순서

오늘 날짜 선택, 글 작성, 사진 추가, 자유 배치 후 저장 확인
상몬 성장에서 오늘 출석을 누르고 기록 보상 확인

| 운영체제 | 다운로드 파일 | 실행 조건과 첫 조작 |
| --- | --- | --- |
| Windows | MyDay-Windows.exe | Windows 10/11, .NET Framework 4.8<br>실행 후 상몬 클릭 또는 트레이에서 일기 열기 |
| macOS | MyDay-macOS-arm64.zip | Apple Silicon, macOS 13 이상<br>압축 해제 후 MyDay.app 실행 |
| Android | MyDay-Android.apk | Android 8 이상, 시험용 APK<br>설치 후 일기 탭에서 날짜와 블록 선택 |

**발표자 메모**

다운로드는 GitHub의 v0.15.3-preview Releases에서 운영체제에 맞는 파일을 선택합니다. Windows EXE를 Mac에서 실행할 수는 없습니다. Windows는 .NET Framework 4.8 환경이고 Mac 배포본은 Apple Silicon과 macOS 13 이상용이며 공증을 완료한 스토어 앱이 아닙니다. Android는 minSdk26에 해당하는 Android 8 이상 시험용 APK입니다. 운영체제가 요구하는 설치 안내를 확인합니다. Android 업데이트에서 서명이 다르다는 메시지가 나오면 백업 확인 없이 기존 앱을 지우지 않습니다. 기존 사용자는 앱을 종료하고 파일을 교체하되 개인 기록 폴더는 삭제하지 않습니다. 첫 실행은 오늘 날짜에 글과 사진을 넣고 배치를 바꾼 뒤 다른 날짜로 이동했다 돌아와 저장을 확인하는 순서가 좋습니다. 출석은 상몬 성장의 버튼으로 직접 처리합니다.
다운로드: https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.15.3-preview
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/DOWNLOADS.md
Windows 0.15.3에는 일곱 게임이 포함됩니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/BLOCK-PUZZLE-INTEGRATION-2026-11-23.md

## 31. 기록을 돕는 보조 기능

현재 간소화 기준: 기본 상몬과 기록 기능 유지
변신, 코디, 옷장과 뽑기 화면은 제외

| 기능 | 사용자가 하는 일 | 기능의 의미 |
| --- | --- | --- |
| 오늘의 질문 | 질문을 골라 글 블록에서 답 작성 | 정해진 공통 질문으로 기록 시작 |
| 감정 달력 | 저장한 오늘의 마음을 월별 확인 | 입력한 감정 표시, AI 분석 아님 |
| 월간 리포트 | 달을 골라 기록과 사진, 습관 확인 | 앱 안에 저장한 기록의 통계 |
| 일기 알림 | 하루 한 번 알림 시간 설정 | 운영체제 알림 권한과 실행 조건 적용 |
| 상몬의 방 | 기본 가구 배치와 방 배경 변경 | 기본 상몬이 쉬는 공간 구성 |

**발표자 메모**

오늘의 질문은 공통 목록에서 날짜에 따라 정한 질문을 보여 주며 AI가 매번 만드는 질문이 아닙니다. 감정 달력은 사용자가 고른 마음 값이므로 진단이나 정서 상태를 추정한 결과로 해석하지 않습니다. 월간 리포트의 기록 일수와 사진 수, 습관 완료율 역시 앱 내부 기록 기준입니다. 일일 알림은 기본 꺼짐이고 사용자가 시간을 선택해 켭니다. Windows는 앱이 트레이에서 실행 중이어야 하며 다른 기기는 운영체제 권한과 절전 설정 영향을 받습니다. 상몬의 방은 기본 가구와 방 배경을 다루는 기능으로 상몬에게 장비나 코디를 입히는 옷장과 구분됩니다. 현재 0.15 제품은 기본 상몬과 기록에 집중하고 변신, 300개 코디, 뽑기와 재화, 합성 화면은 제거했습니다. 과거 백업의 관련 메타데이터는 호환용으로 보존하지만 현행 기능으로 홍보하지 않습니다.
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-11
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-13
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/USER-MANUAL.md#chapter-20
https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/FOCUSED-APP.md
