# 상몬 장비함 · 300종 / 8슬롯

기본 상몬의 클레이 외형과 함께 사용하는 새 장비함입니다. **50가지 형태 × 6가지 속성 재질 = 300종**이며, 기존 검·보주 12종의 ID와 이름을 보존합니다. 현재 장비는 모두 무료로 선택하는 꾸미기 아이템입니다.

![50가지 장비 형태](../previews/applied/wardrobe-v1/wardrobe-50-forms.png)

| 슬롯 | 형태 | 아이템 수 |
| --- | --- | ---: |
| 무기 | 검·단검·도끼·망치·지팡이·창·활·요술봉·낫·마도서·부채·우산 | 72 |
| 보주·소품 | 보주·수정 조각·랜턴·나침반·방울·물약병 | 36 |
| 머리 | 왕관·후드·마법 모자·투구·베레모·꽃·빛 고리·사슴뿔 | 48 |
| 얼굴 | 안경·외눈 안경·가면·고글·별 안대·볼 스티커 | 36 |
| 목 | 목도리·나비넥타이·넥타이·펜던트·칼라·헤드폰 | 36 |
| 등 | 날개·망토·가방·로켓·잎사귀·깃발 | 36 |
| 오라 | 마법진·룬 문자·별무리·거품 | 24 |
| 발밑 | 구름 발판·호버 보드 | 12 |

각 형태는 태양·화염·빙결·번개·그림자·별빛 재질로 나뉩니다. 300개의 서로 다른 조각을 수작업으로 그린다는 뜻이 아니라, 50가지 도형을 여섯 재질로 렌더링하는 구조입니다. 장비 목록은 계속 추가할 수 있고 UI나 저장기에 300개 제한을 두지 않습니다.

## 사용 방법

- **Windows:** 일기 오른쪽 아이템 또는 바탕화면 캐릭터 우클릭 → 아이템 장착. 슬롯·속성·이름으로 찾고 페이지를 넘겨 장착합니다. 페이지당 12개만 표시해 300개의 애니메이션을 동시에 실행하지 않습니다.
- **Android:** 상몬 → 장비 300종. 위쪽 슬롯 목록을 좌우로 넘기고 속성·이름으로 검색합니다. 목록은 화면에 필요한 카드만 표시합니다.
- 각 슬롯에 하나씩 **최대 8개를 함께 장착**합니다. 하나를 바꾸거나 해제해도 나머지는 유지합니다. 슬롯별 해제로 장식을 빼고 기본 상몬을 사용할 수 있습니다.
- 장비는 앱 전체 설정으로 날짜·상몬 버전을 바꿔도 유지합니다. 앱·바탕화면/위젯/라이브 배경화면에 함께 표시하고 JSON 백업에 포함합니다.
- 장비 변경은 XP를 지급하지 않습니다. 구매·가챠·전투 능력치는 없습니다.

![8슬롯 조합 예시](../previews/applied/wardrobe-v1/outfits-eight-slots.png)

## 팀원이 수정할 파일

| 바꿀 내용 | 파일 |
| --- | --- |
| 장비 추가·이름·속성·슬롯 | [SangmonItems.cs](../windows/Core/SangmonItems.cs)의 Forms·Create |
| 기존 검·보주 그림 | [ItemPainter.cs](../windows/Character/ItemPainter.cs) |
| 추가 48가지 형태·착용 위치·레이어 | [ItemPainter.Wardrobe.cs](../windows/Character/ItemPainter.Wardrobe.cs)의 Wardrobe·Placement |
| Windows 검색·필터·페이지·장착 UI | [EquipmentWindow.cs](../windows/UI/EquipmentWindow.cs) |
| Android 장비함·검색·필터 | [PhoneCompanion.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneCompanion.kt) |
| Android 착용 레이어·포즈 | [SangmonSprites.kt](../app/src/main/java/com/myday/diary/ui/character/SangmonSprites.kt) |
| 공통 이미지와 Kotlin ID 목록 다시 생성 | [export-android-assets.ps1](../tools/export-android-assets.ps1) |

새 형태는 Forms에 추가하고 Wardrobe에 그리는 분기를 추가합니다. 기존 ID를 이름 변경에 따라 바꾸지 마세요. 새 슬롯은 Windows 장비 모델·Get/Set·Placement, Android Equipment·JSON·홈 캐시·그리기 순서까지 함께 확장해야 합니다. PNG나 생성된 EquipmentCatalog.kt를 직접 고치지 않습니다.

[전체 300종 ID 목록](ITEM-CATALOG.md) · [출석·성장 규칙](SANGMON-GROWTH.md) · [실제 적용과 화면](../previews/applied/wardrobe-v1/README.md)

## 저장 호환성

새 장비나 추가 슬롯은 **백업 버전 5**로 저장합니다. 새 앱은 버전 1~4도 읽으며 기존 무기·보주와 일기를 보존합니다. 버전 5 백업은 이번 변경을 포함한 Windows·Android로 여세요. 이전 Releases의 앱으로는 새 백업을 열 수 없습니다. 새로운 기능을 저장해도 예전 형식으로 낮추지 않습니다.