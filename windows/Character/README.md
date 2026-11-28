# 상몬과 아이템 그림 수정

**0.16.1 영상 속 새 기본 상몬만 표시합니다.** `MonsterPainter.Draw`는 저장된 과거 외형·장비와 관계없이 기본 클레이 상몬을 그립니다. `DrawLegacy`는 이전 자산과 백업 호환성 검사를 위한 개발용 경로입니다. 변신·장착 선택 화면은 제거했습니다. [현재 범위](../../docs/FOCUSED-APP.md).

상몬 도형·추가 의상·아이템·행동·바탕화면 투명 창을 관리합니다. 이름과 해금 조건, 장비 목록은 Core와 함께 확인합니다.

[전체 수정 가이드](../../docs/EDITING-GUIDE.md) · [프로젝트 첫 화면](../../README.md)

| 바꾸려는 것 | 파일 | 역할 |
| --- | --- | --- |
| 상몬 기본 몸·눈·입·포즈 | [MonsterPainter.cs](MonsterPainter.cs) | 공통 몸 경로와 움직이는 포즈 |
| 기본 상몬의 3D풍 클레이 음영·얼굴·작은 불꽃 | [ClayMaterial.cs](ClayMaterial.cs) | 기본 original 외형만 담당, [실제 적용 미리보기](../../previews/applied/sangmon-plump-clay/README.md) |
| 이름·저장 ID·이동 속도 | [MonsterVariants.cs](MonsterVariants.cs) | 64종 외형 식별 |
| 귀·꼬리·날개 등 추가 장식 | [MonsterAdditions.cs](MonsterAdditions.cs) | 뒤/앞 레이어 |
| RPG 속성의 몸 재질 | [GameSkins.cs](GameSkins.cs) | 8종 속성 팔레트·명암·효과 |
| 성장 보상 의상 | [RewardSkins.cs](RewardSkins.cs) | 8종 의상·소품 |
| 소품 아이콘·손 소품의 움직임 | [ItemPainter.cs](ItemPainter.cs) | 공통 크기·DrawEquipped |
| 일상 소품 50형태·파스텔 6색 | [CozyItems.cs](CozyItems.cs) | 그림·음영·따뜻한 윤곽선 |
| 8슬롯 착용 위치 | [ItemPainter.Wardrobe.cs](ItemPainter.Wardrobe.cs) | Placement·앞/뒤 레이어·Android 착용 PNG 원본 |
| 소품 이름과 설명 | [SangmonItems.cs](../Core/SangmonItems.cs) | 그림과 별도로 관리하는 아이템 목록 |
| 걷기·쉬기·하품·클릭 반응 | [PetBehavior.cs](PetBehavior.cs) | 행동 전환·가속·반응 시간 |
| 바탕화면 이동·마우스·메뉴 | [DesktopPet.cs](DesktopPet.cs) | 투명 창·드래그·경계·우클릭 메뉴 |
| 탄성·던지기·중력·화면 충돌 | [PetDynamics.cs](PetDynamics.cs) | [사용법과 조절 값](../../docs/DESKTOP-PET-MOTION.md) |

## 세 플랫폼에 같은 새 외형 적용

영상 마지막의 통통한 몸통·낮은 배·점눈·얇은 입선이 원본입니다. [character-art.json](../../shared/character-art.json)에 원본 영상의 해시와 생성 자산의 해시를 기록합니다. `Original` 분기만 수정하고 기존 저장 ID를 유지합니다.

1. `MonsterPainter.cs`의 Original 몸 경로, `ClayMaterial.cs`의 얼굴·재질·불꽃을 수정합니다.
2. `./tools/export-android-assets.ps1 -Focused`로 `app/src/main/assets/sangmon/original.png` 한 장만 갱신합니다.
3. 명세의 atlas SHA-256을 갱신하고 `-CheckFocused`로 64프레임을 실제 렌더러와 비교합니다.
4. `python tools/prepare-mac-assets.py`로 Mac에 같은 파일을 준비합니다. Mac 빌드에서도 자동 실행합니다.
5. 빌드·동작·기존 데이터 호환 검사를 수행합니다. `--clay-preview 출력폴더`로 실제 적용 그림을 볼 수 있습니다.

과거 전체 생성 모드는 호환성 개발용입니다. 현재 제품의 외형 변경에는 `-Focused`를 사용하며 의상·변신 선택이나 배포 자산을 다시 추가하지 않습니다.

## 바탕화면 공놀이

[FetchGame.cs](FetchGame.cs)는 추적·운반·귀환, [DesktopBallWindow.cs](DesktopBallWindow.cs)는 작은 투명 공 창과 드래그, [DesktopPet.Fetch.cs](DesktopPet.Fetch.cs)는 기존 상몬과의 연결/정리를 담당합니다. [사용법·공통 규칙·검사](../../docs/JUMP-AND-DESKTOP-FETCH.md).
