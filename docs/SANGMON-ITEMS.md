# 상몬의 작은 옷장 · 소품 300종

상몬과 함께하는 일상 소품입니다. **50가지 형태 × 6가지 파스텔 색감 = 300종**으로, 크림·살구·하늘·민트·라벤더·딸기를 고를 수 있습니다. 둥근 모양과 따뜻한 갈색 윤곽선으로 문구·인형·니트 느낌을 냈습니다. 기본 8종은 무료이고, 나머지는 뽑기·꽃잎 교환으로 얻어 입힙니다.

![소품 조합 예시](../previews/applied/cozy-v1/outfits-eight-slots.png)

| 위치 | 소품 예 | 개수 |
| --- | --- | ---: |
| 손 소품 | 색연필·크레용·꽃·카메라·풍선·막대 사탕·머그컵·비눗방울 막대·달 쿠션·노트·부채·우산 | 72 |
| 작은 소품 | 동그란 참·편지·반딧불 병·시계·방울·우유병 | 36 |
| 머리 | 리본·보닛·니트 비니·버킷햇·베레모·데이지 핀·도넛 머리띠·토끼 귀 | 48 |
| 얼굴 | 동그란/하트 안경·수면 안대·선글라스·별 스티커·볼터치 | 36 |
| 목 | 니트 목도리·리본·스카프·하트 목걸이·레이스 칼라·헤드폰 | 36 |
| 등 | 나비 날개·체크 담요·배낭·곰 인형·잎사귀 가방·크로스백 | 36 |
| 주변 장식 | 하트·꽃잎·별·비눗방울 | 24 |
| 발밑 | 폭신 구름·체크 쿠션 | 12 |

![50가지 형태](../previews/applied/cozy-v1/wardrobe-50-forms.png)

300종은 서로 다른 조각 300개가 아니라 50가지 도형에 여섯 색감을 적용한 구성입니다. 목록은 더 늘릴 수 있으며 300개 제한을 두지 않습니다.

## 사용 방법

- Windows: 일기 오른쪽 **옷장**, 또는 바탕화면 상몬 우클릭 → **작은 옷장**. 위치·색감·이름으로 찾고 **입혀 보기**를 누릅니다. 한 페이지에는 12개를 표시합니다.
- Android: **상몬 → 소품 300종**에서 위치·색감·이름으로 찾습니다.
- 위치마다 하나씩 최대 8개를 조합합니다. **이 소품 벗기기**로 개별 해제할 수 있습니다.
- 선택은 날짜·상몬 외형을 바꿔도 유지하고 JSON 백업에 포함합니다. 현금 구매·전투 능력치가 없으며 소품 변경은 XP를 지급하지 않습니다.

## 팀원이 수정할 파일

| 바꿀 내용 | 파일 |
| --- | --- |
| 소품 이름·목록·색감 이름·저장 ID | [SangmonItems.cs](../windows/Core/SangmonItems.cs)의 Forms·PaletteNames·Create |
| 50가지 그림·파스텔 RGB·선·음영 | [CozyItems.cs](../windows/Character/CozyItems.cs) |
| 아이콘 크기·손 소품의 움직임 | [ItemPainter.cs](../windows/Character/ItemPainter.cs) |
| 착용 위치·앞/뒤 순서·Android 레이어 | [ItemPainter.Wardrobe.cs](../windows/Character/ItemPainter.Wardrobe.cs) |
| Windows 옷장 화면·검색·필터 | [EquipmentWindow.cs](../windows/UI/EquipmentWindow.cs) |
| Android 옷장 화면·검색·필터 | [PhoneCompanion.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneCompanion.kt) |
| Android 착용 레이어·포즈 | [SangmonSprites.kt](../app/src/main/java/com/myday/diary/ui/character/SangmonSprites.kt) |
| Android 공통 자산 다시 생성 | [export-android-assets.ps1](../tools/export-android-assets.ps1) |

새 형태는 Forms와 CozyItems.Draw에 함께 추가합니다. PNG와 생성된 EquipmentCatalog.kt는 직접 수정하지 않습니다. 소품만 바꿨다면 생성 스크립트에 `-ItemsOnly`를 붙여 상몬 본체 자산을 다시 그리지 않을 수 있습니다.

## 저장 호환성

기존 선택을 보존하기 위해 내부 ID는 그대로입니다. 예를 들어 `solar-sword`는 이제 **크림 색연필**, `solar-crown`은 **크림 리본 머리띠**입니다. Solar/Ember 같은 열거형도 기존 저장용 값이며 화면에는 파스텔 색감으로 표시합니다. 표시 이름을 바꿀 때 ID를 바꾸지 마세요.

새 소품과 추가 위치는 소유권을 포함한 백업 버전 7을 사용합니다. 버전 1~6도 읽으며 성장·출석·일기 기록은 유지합니다. 새 백업은 이번 변경을 포함한 Windows·Android로 여세요. 이전 Releases 앱에는 이 옷장이 포함되지 않습니다.

[전체 300종 ID 목록](ITEM-CATALOG.md) · [출석·성장 규칙](SANGMON-GROWTH.md) · [적용 이미지](../previews/applied/cozy-v1/README.md)
