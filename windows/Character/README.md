# 상몬과 아이템 그림 수정

상몬 도형·추가 의상·아이템·행동·바탕화면 투명 창을 관리합니다. 이름과 해금 조건, 장비 목록은 Core와 함께 확인합니다.

[전체 수정 가이드](../../docs/EDITING-GUIDE.md) · [프로젝트 첫 화면](../../README.md)

| 바꾸려는 것 | 파일 | 역할 |
| --- | --- | --- |
| 상몬 기본 몸·눈·입·포즈 | [MonsterPainter.cs](MonsterPainter.cs) | 공통 몸 경로와 움직이는 포즈 |
| 이름·저장 ID·이동 속도 | [MonsterVariants.cs](MonsterVariants.cs) | 64종 외형 식별 |
| 귀·꼬리·날개 등 추가 장식 | [MonsterAdditions.cs](MonsterAdditions.cs) | 뒤/앞 레이어 |
| RPG 속성의 몸 재질 | [GameSkins.cs](GameSkins.cs) | 8종 속성 팔레트·명암·효과 |
| 성장 보상 의상 | [RewardSkins.cs](RewardSkins.cs) | 8종 의상·소품 |
| 검·보주·빛나는 효과 | [ItemPainter.cs](ItemPainter.cs) | Sword·Orb·Tone·DrawEquipped |
| 검·보주 이름과 설명 | [SangmonItems.cs](../Core/SangmonItems.cs) | 그림과 별도로 관리하는 아이템 목록 |
| 걷기·쉬기·하품·클릭 반응 | [PetBehavior.cs](PetBehavior.cs) | 행동 전환·가속·반응 시간 |
| 바탕화면 이동·메뉴·장비 적용 | [DesktopPet.cs](DesktopPet.cs) | 투명 창·드래그·경계·우클릭 메뉴 |
