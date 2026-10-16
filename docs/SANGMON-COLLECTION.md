# 상몬의 선물 상자 · 뽑기와 수집 도감

Windows는 **옷장 → 소품 뽑기**, Android는 **상몬 → 소품 뽑기**에서 엽니다. 기존 300종 자유 꾸미기는 유지합니다. 도감은 뽑기·꽃잎 교환으로 모은 소품을 별도로 표시합니다. 현금 결제는 없습니다.

## 클로버 얻기

| 조건 | 클로버 |
| --- | ---: |
| 처음 시작하는 기본 잔액 | 40 |
| 활동으로 얻는 XP | 1 XP당 1 |
| 레벨 업 | 레벨마다 5 |
| 연속 출석 | 7일마다 10 |

잔액은 `40 + 누적 XP + (현재 레벨 - 1) × 5 + 완료한 연속 출석 주 수 × 10 - 뽑기 횟수 × 20`입니다. 기존에 얻은 XP와 레벨 보상도 반영합니다. 일기 삭제·반복 백업 가져오기로 재화를 다시 지급하지 않습니다. 출석이 끊기면 연속 일수만 다시 세며 이미 얻은 보상은 유지합니다.

## 뽑기·확률·중복 교환

- 1회: 클로버 **20개**. 일반 회차에는 300종 각각 **1/300** 확률입니다. 외형/색감/위치마다 가중치를 다르게 두지 않습니다.
- 10·20·30번째처럼 **10번째마다 아직 수집하지 않은 소품**을 보장합니다. 미수집 N종 각각 **1/N**입니다. 모두 모았다면 일반 확률로 돌아갑니다.
- 중복: **꽃잎 3개**. 새 소품은 수집 도감에 추가하며 자동으로 착용하지 않습니다.
- 꽃잎 **15개**: 원하는 미수집 소품 1개와 교환합니다. 교환은 클로버나 뽑기 보장 횟수에 영향을 주지 않습니다.
- 화면에서 재화·수집 수·보장까지 남은 횟수·수집 필터·이름 검색·선택 교환을 확인합니다. 휴대폰은 저장 성공 후 결과 화면과 햅틱을 표시하며 진동 설정을 따릅니다.

## 저장과 백업

뽑기 결과를 생성한 뒤 저장에 성공해야 잔액·도감·결과를 확정합니다. 저장 실패 시 원래 재화와 수집 상태를 유지합니다. 저장 중 연타로 여러 번 지출할 수 없습니다.

수집 내역은 **버전 6의 Collection.Actions**에 순서대로 저장합니다. 각 행동의 ID·종류(draw/exchange)·소품 ID로 잔액·중복·보장 규칙을 재계산하며 잔액 숫자를 별도로 신뢰하지 않습니다. ID 중복·잘못된 소품·재화 부족·잘못된 10회 보장은 거부합니다. 내역은 최대 50,000건입니다.

동일한 수집 내역이나 그 이전 백업은 지출을 중복 적용하거나 되돌리지 않고 더 긴 내역을 유지합니다. **두 기기에서 같은 백업을 출발점으로 따로 뽑은 경우에는 병합을 거부합니다.** 현재 기록을 먼저 백업한 뒤 한 기기의 최신 수집 기록을 이어서 사용하세요. 기존 버전 1~5도 계속 읽습니다. 버전 6 백업은 새 Windows·Android 앱으로 여세요. 이전 Releases 앱에는 이 기능이 포함되지 않습니다.

## 팀원이 수정할 파일

| 내용 | Windows | Android |
| --- | --- | --- |
| 레벨 구간·출석 주 보상 계산 | [SangmonGrowth.cs](../windows/Core/SangmonGrowth.cs) | [NotebookRules.kt](../app/src/main/java/com/myday/diary/diary/NotebookRules.kt) |
| 클로버·비용·확률·보장·꽃잎·병합 | [SangmonCollection.cs](../windows/Core/SangmonCollection.cs) | [CollectionRules.kt](../app/src/main/java/com/myday/diary/diary/CollectionRules.kt) |
| 선물 상자·수집 도감 화면 | [CollectionWindow.cs](../windows/UI/CollectionWindow.cs) | [PhoneCollection.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneCollection.kt) |
| 저장 성공 후 결과·실패 복구 | [DiaryWindow.Collection.cs](../windows/UI/DiaryWindow.Collection.cs) | [NotebookController.kt](../app/src/main/java/com/myday/diary/diary/NotebookController.kt) |
| 버전 6 JSON | [DiaryData.cs](../windows/Core/DiaryData.cs) | [DiaryModels.kt](../app/src/main/java/com/myday/diary/data/DiaryModels.kt) · [NotebookJson.kt](../app/src/main/java/com/myday/diary/data/NotebookJson.kt) |
| 검사 | [CollectionTests.cs](../windows/CollectionTests.cs) | [CollectionTest.kt](../app/src/test/java/com/myday/diary/diary/CollectionTest.kt) |

[성장 점수표](SANGMON-GROWTH.md) · [소품 디자인 수정](SANGMON-ITEMS.md) · [실제 화면](../previews/applied/collection-v1/README.md)
