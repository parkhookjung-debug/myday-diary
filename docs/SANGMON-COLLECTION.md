# 상몬의 선물 상자 · 뽑기와 수집 도감

Windows는 **옷장 → 소품 뽑기**, Android는 **상몬 → 소품 뽑기**에서 엽니다. **기본 소품 8종은 무료**이며, 나머지 292종은 뽑기나 꽃잎 교환으로 얻어야 입힐 수 있습니다. 옷장에서 모든 모양을 미리 보고, 보유한 소품만 골라 입힙니다. 현금 결제는 없습니다.

## 무료 기본 소품

| 위치 | 소품 |
| --- | --- |
| 손 소품 | 하늘 색연필 |
| 작은 소품 | 딸기 동그란 참 |
| 머리 | 크림 리본 머리띠 |
| 얼굴 | 하늘 동그란 안경 |
| 목 | 크림 니트 목도리 |
| 등 | 크림 나비 날개 |
| 주변 장식 | 크림 작은 하트 |
| 발밑 | 하늘 폭신 구름 |

기본 소품은 뽑기 대상에서 제외합니다. 처음에는 8/300을 보유합니다. 업데이트 직전에 저장된 착용 소품도 무료로 보존하므로 기존 사용자는 보유 수가 더 많을 수 있습니다. 한 번 보존한 소품은 벗겨도 계속 사용할 수 있습니다.

미보유 소품에는 잠금 표시가 나오며 입히기 버튼은 비활성화됩니다. 화면뿐 아니라 착용 처리와 버전 7 백업 검증에서도 소유권을 확인합니다. 뽑기·교환 후에는 옷장에 즉시 반영됩니다. **보유 소품만** 필터로 사용할 수 있는 소품을 찾습니다.

## 클로버 얻기

| 조건 | 클로버 |
| --- | ---: |
| 처음 시작하는 기본 잔액 | 40 |
| 활동으로 얻는 XP | 1 XP당 1 |
| 레벨 업 | 레벨마다 5 |
| 연속 출석 | 7일마다 10 |
| 만렙 이후 | 누적 50 XP마다 추가 5 |

잔액은 `40 + 누적 XP + 기존 XP 구간으로 계산한 성장 보상 횟수 × 5 + 완료한 연속 출석 주 수 × 10 - 뽑기 횟수 × 20`입니다. 화면 레벨은 Lv.100에서 멈추지만 재화용 성장 구간은 이후에도 50 XP마다 계속 계산합니다. 따라서 기존 고레벨 사용자의 재화와 지출을 보존합니다. 누적 XP에는 새 연속 7일 보너스 10 XP도 포함됩니다. 일기 삭제·반복 백업 가져오기로 재화를 다시 지급하지 않습니다. 출석이 끊기면 연속 일수만 다시 세며 이미 얻은 보상은 유지합니다.

첫날 미션 40 XP를 모두 모으면 Lv.5가 됩니다. 환영 보상 40 + 활동 40 + 레벨 보상 20 = 클로버 100개로 총 5번 뽑을 수 있습니다.

## 등급 뽑기·합성·중복 교환

[작은 조각 공방의 전체 확률·합성·제작·성취 보상과 수정 원본](SANGMON-ATELIER.md)을 참고하세요.

- 1회: 클로버 **20개**. 등급 확률은 일상 65%·반짝 25%·별빛 9%·꿈빛 1%이며 등급 안에서는 소품마다 균등합니다. 기본 8종은 제외합니다. 소품과 같은 등급 조각 1개도 받습니다.
- 10·20·30번째처럼 **10번째마다 아직 수집하지 않은 소품**을 보장합니다. 미수집만 대상으로 등급 가중치를 다시 배분하고 해당 등급 안에서는 균등합니다. 모두 모았다면 일반 확률로 돌아갑니다.
- 중복: 등급별 **꽃잎 3/6/12/24개**. 새 소품은 보유 목록에 추가하며 자동으로 착용하지 않습니다.
- 꽃잎 **15/30/60/120개**: 해당 등급의 원하는 미수집 소품 1개와 교환합니다. 교환은 클로버나 뽑기 보장 횟수에 영향을 주지 않습니다.
- 화면에서 재화·보유 수·보장까지 남은 횟수·보유 필터·이름 검색·선택 교환을 확인합니다. 휴대폰은 저장 성공 후 결과 화면과 햅틱을 표시하며 진동 설정을 따릅니다.

## 저장과 백업

뽑기 결과를 생성한 뒤 저장에 성공해야 잔액·도감·결과를 확정합니다. 저장 실패 시 원래 재화와 수집 상태를 유지합니다. 저장 중 연타로 여러 번 지출할 수 없습니다.

새 등급 뽑기·합성·제작 내역은 **버전 8의 Collection.Actions**에 순서대로 저장합니다. 이전 버전의 지출·보상 규칙은 당시 방식으로 보존합니다. 각 행동의 ID·종류(draw/exchange/closet/legacy)·소품 ID로 잔액·중복·보장 규칙을 재계산하며 잔액 숫자를 별도로 신뢰하지 않습니다. ID 중복·잘못된 소품·재화 부족·잘못된 10회 보장은 거부합니다. 내역은 기본 지급·이전 착용 보존까지 포함해 최대 50,301건입니다.

동일한 뽑기·교환 내역이나 그 이전 백업은 지출을 중복 적용하거나 되돌리지 않고 더 긴 지출 내역을 유지합니다. 기본 지급과 이전 착용 보존은 재화를 추가하지 않습니다. 버전 6의 기존 뽑기 내역을 먼저 계산한 다음 새 기본 소품을 지급하므로 예전 중복 꽃잎과 보장 결과를 소급 변경하지 않습니다. **두 기기에서 같은 백업을 출발점으로 따로 뽑은 경우에는 병합을 거부합니다.** 현재 기록을 먼저 백업한 뒤 한 기기의 최신 수집 기록을 이어서 사용하세요. 기존 버전 1~7도 계속 읽습니다. 버전 8 백업은 이를 지원하는 최신 Windows·Android 앱으로 여세요. 이전 Releases 앱에는 이 기능이 포함되지 않습니다.

## 팀원이 수정할 파일

| 내용 | Windows | Android |
| --- | --- | --- |
| 레벨 구간·출석 주 보상 계산 | [SangmonGrowth.cs](../windows/Core/SangmonGrowth.cs) | [NotebookRules.kt](../app/src/main/java/com/myday/diary/diary/NotebookRules.kt) |
| 클로버·비용·확률·보장·꽃잎·병합 | [SangmonCollection.cs](../windows/Core/SangmonCollection.cs) | [CollectionRules.kt](../app/src/main/java/com/myday/diary/diary/CollectionRules.kt) |
| 선물 상자·수집 도감 화면 | [CollectionWindow.cs](../windows/UI/CollectionWindow.cs) | [PhoneCollection.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneCollection.kt) |
| 저장 성공 후 결과·실패 복구 | [DiaryWindow.Collection.cs](../windows/UI/DiaryWindow.Collection.cs) | [NotebookController.kt](../app/src/main/java/com/myday/diary/diary/NotebookController.kt) |
| 버전 8 JSON | [DiaryData.cs](../windows/Core/DiaryData.cs) | [DiaryModels.kt](../app/src/main/java/com/myday/diary/data/DiaryModels.kt) · [NotebookJson.kt](../app/src/main/java/com/myday/diary/data/NotebookJson.kt) |
| 검사 | [CollectionTests.cs](../windows/CollectionTests.cs) | [CollectionTest.kt](../app/src/test/java/com/myday/diary/diary/CollectionTest.kt) |

[성장 점수표](SANGMON-GROWTH.md) · [소품 디자인 수정](SANGMON-ITEMS.md) · [현재 잠금·보유·뽑기 화면](../previews/applied/ownership-v1/README.md)
