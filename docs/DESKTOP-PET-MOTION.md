# Windows 상몬: 탄성 드래그와 던지기

2026-11-11 · Windows 바탕화면 상몬에 적용. Mac·Android의 드래그 물리는 이번 변경 범위에 포함하지 않습니다.

## 사용법

1. 상몬의 몸을 마우스 왼쪽 버튼으로 잡고 움직입니다. 움직이는 방향으로 몸이 길어지고 반대 방향은 살짝 좁아집니다.
2. 잡은 상태에서 마우스를 멈추면 몸이 원래 모양으로 돌아옵니다.
3. 빠르게 움직이면서 버튼을 놓으면 같은 방향으로 날아갑니다. 빠르게 던질수록 빠르고 멀리 갑니다.
4. 화면의 좌우·위쪽과 바닥에 닿으면 잠깐 찌그러졌다가 튕깁니다. 중력·공기 저항과 충돌 감쇠로 점점 멈춥니다. 바닥은 작업 표시줄을 제외한 화면 영역입니다.
5. 천천히 내려놓거나 멈춘 뒤 놓으면 그대로 내려놓습니다. 다시 잡으면 날아가던 상몬을 멈출 수 있습니다.

살짝 클릭하면 기존처럼 불을 잠깐 뿜고 일기 창을 엽니다. 드래그·던지기는 일기 창을 열거나 경험치를 지급하지 않습니다. 우클릭 메뉴의 자유 이동을 꺼도 직접 던지기는 사용할 수 있습니다. 숨긴 뒤에는 기존 앱의 캐릭터 표시 기능으로 복귀합니다.

## 조절할 파일

| 수정할 내용 | 파일과 값 |
| --- | --- |
| 던지기 속도 | [PetDynamics.cs](../windows/Character/PetDynamics.cs)의 `ThrowGain=1.80`: 추정한 마우스 이동 속도에 곱하는 값 |
| 과도한 발사 제한 | 같은 파일 `MaxThrowSpeed=5200` px/s. 제한 미만에서는 같은 조건의 드래그 속도가 2배면 발사 속도도 2배 |
| 중력·반발·감속 | 같은 파일 `Gravity`, `Restitution`, `Step`의 공기 저항·바닥 마찰 |
| 잡았을 때 늘어남 | 같은 파일 `Held` 분기의 따라오기·속도·거리 기반 변형. 늘어남은 최대 약 1.75배이고 멈추면 완화 |
| 마우스 캡처·클릭 구분·화면 경계 | [DesktopPet.cs](../windows/Character/DesktopPet.cs), [PetGesture.cs](../windows/Core/PetGesture.cs) |
| 몸·눈·얼굴 그림 | 기존 [MonsterPainter.cs](../windows/Character/MonsterPainter.cs), [ClayMaterial.cs](../windows/Character/ClayMaterial.cs) |
| 동작 검사·프레임 출력 | [PetDynamicsTests.cs](../windows/Tests/PetDynamicsTests.cs) |

별도 엔진·패키지·그림 자산을 추가하지 않습니다. 기존 타이머에서 물리를 계산합니다. 투명 창의 여백을 늘려 큰 변형이 잘리지 않게 했으며 기본 몸 크기는 유지합니다. 각 모니터의 작업 영역을 기준으로 튕기고, 잡아서 다른 모니터로 옮길 수 있습니다. 일반 프로그램 창은 충돌 대상으로 처리하지 않습니다.

## 검증과 미리보기

`windows/build.ps1`로 빌드하고 `windows/bin/MyDay.exe --self-test`로 속도 비례·상한·중력·네 방향 충돌·감쇠·음수 좌표 화면·화면 크기 변경·취소·프레임 간격 차이를 검사합니다. `--smoke-test <출력 폴더>`는 격리된 기록으로 기존 일기 화면을 검사하고, 실제 상몬의 공통 포인터 처리 경로로 클릭·드래그·던지기·캡처 취소와 최대 변형의 그림 경계를 검사합니다.

`--pet-physics-preview <출력 폴더>`는 앱의 물리 계산과 렌더러로 180장의 재현 가능한 PNG를 만듭니다. `--pet-playground`는 일기 데이터를 읽거나 쓰지 않는 상몬 전용 시험 창을 엽니다. 시험 창에서 클릭의 일기 열기는 연결하지 않습니다. 실제 앱에서는 기존 일기 열기가 유지됩니다.

[움직이는 미리보기](../previews/pet-motion-20261111/README.md)

검증 완료: Windows CI의 346개 assertion·네이티브 화면/포인터 smoke·로컬 채팅 서버/Windows 두 클라이언트가 모두 통과했습니다. [빌드 원본·용량·SHA256](builds/windows-pet-motion-2026-11-11.json) · [CI 결과](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43032278564).
