# 상몬 레벨업 · 출석과 일기 미션

Windows는 **상몬 성장**, Android는 **상몬 탭 → 오늘의 미션**에서 확인합니다. 오늘 출석 버튼을 누르거나 일기를 작성·저장하면 경험치가 쌓입니다.

| 활동 | 점수 | 인정 기준 |
| --- | ---: | --- |
| 출석 | +5 XP | 오늘 출석 버튼, 하루 한 번 |
| 첫 기록 저장 | +10 XP | 글·사진 설명·감정·사진·할 일/습관 완료 등 실제 내용 변경 후 저장 성공 |
| 글 20자 이상 | +10 XP | 글·감정 블록의 공백을 제외한 합계 20자 이상, 해당 글 내용을 새로 작성·수정 |
| 사진 첨부 | +5 XP | 실제 사진 새 첨부 또는 교체 |
| 할 일 완료 | +5 XP | 미완료 → 완료 체크 또는 완료된 새 블록 |
| 습관 완료 | +5 XP | 미완료 → 완료 체크 또는 완료된 새 블록 |

**항목별 하루 한 번, 하루 최대 40 XP**입니다. 짧은 글을 먼저 저장해 +10을 받은 후 20자를 채우면 같은 날 추가 +10을 받을 수 있습니다. 사진이나 체크를 여러 개 추가해도 해당 항목은 한 번입니다. 글을 고쳐도 예전 사진·이미 완료된 체크에는 새 점수를 주지 않습니다.

## 레벨과 보상

**누적 50 XP마다 레벨 업**합니다. Lv.1부터 시작하고, 기존 경험치와 레벨 계산을 그대로 유지합니다. 레벨 제한을 별도로 두지 않으며 쉬는 날·일기 삭제에도 이미 받은 점수를 차감하지 않습니다.

| 해금 | 누적 XP | 성장 보상 |
| --- | ---: | --- |
| Lv.2 | 50 | 기록 견습 상몬 |
| Lv.3 | 100 | 책갈피 상몬 |
| Lv.4 | 150 | 일기 기사 상몬 |
| Lv.5 | 200 | 기억 마법사 상몬 |
| Lv.6 | 250 | 별빛 탐험가 상몬 |
| Lv.7 | 300 | 일기 연금술사 상몬 |
| Lv.8 | 350 | 페이지 군주 상몬 |
| Lv.10 | 450 | 추억 용사 상몬 |

기존 자유 선택 56종과 장비 300종은 계속 자유롭게 사용합니다. 성장 보상 8종만 해당 레벨에서 열립니다. 장비를 바꿔도 점수를 지급하지 않습니다.

## 중복 지급과 이전 기록

- 기준 날짜는 **저장에 성공한 기기의 현지 날짜**입니다. 일기에서 선택한 과거/미래 날짜가 아닙니다. 같은 날 다른 날짜의 일기를 여러 개 써도 하루 한도가 같습니다.
- 글자 수만 판정하며 AI로 글의 질을 평가하지 않습니다. 빈 블록·빈 사진 자리·테마·기분 선택·배치·장비 변경·단순 순서 변경은 기록 점수를 주지 않습니다.
- 마지막으로 저장된 내용과 비교합니다. 자정을 지나 글을 새로 수정하면 새 날짜의 기록/글 미션을 받을 수 있지만, 그대로 재저장하거나 기존 사진·체크를 그대로 두는 것만으로 받지는 않습니다.
- 저장 실패 시 출석·활동·레벨은 확정하지 않습니다. 일기 초안은 남기고 재시도 성공 시 한 번만 지급합니다.
- 기존 AwardedDays의 **하루 10 XP는 그대로 보존**합니다. 과거 기록에서 새로운 미션 점수를 소급 계산하지 않습니다.
- 백업 병합은 지급 날짜와 항목의 합집합으로 처리합니다. 같은 백업을 다시 가져와도 XP가 더 늘지 않습니다. Windows↔Android 자동 동기화는 아니며, 사용자가 JSON을 옮겨 병합합니다.
- 현지 시각을 사용하는 오프라인 앱입니다. 기기 시계 변경을 서버에서 검증하는 시스템은 없습니다.

## 개발 파일

| 바꿀 내용 | Windows | Android |
| --- | --- | --- |
| 점수·활동 판정·중복 방지·합병 | [SangmonGrowth.cs](../windows/Core/SangmonGrowth.cs) | [NotebookRules.kt](../app/src/main/java/com/myday/diary/diary/NotebookRules.kt) |
| 저장 성공 후 확정·실패 복구 | [DiaryWindow.cs](../windows/UI/DiaryWindow.cs), [DiaryWindow.Growth.cs](../windows/UI/DiaryWindow.Growth.cs) | [NotebookController.kt](../app/src/main/java/com/myday/diary/diary/NotebookController.kt), [NotebookViewModel.kt](../app/src/main/java/com/myday/diary/diary/NotebookViewModel.kt) |
| 레벨·출석·미션 화면 | [GrowthWindow.cs](../windows/UI/GrowthWindow.cs) | [PhoneCompanion.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneCompanion.kt) |
| 단위 검사 | [GrowthTests.cs](../windows/GrowthTests.cs), [ActivityTests.cs](../windows/ActivityTests.cs) | [WardrobeGrowthTest.kt](../app/src/test/java/com/myday/diary/diary/WardrobeGrowthTest.kt) |

활동은 버전 5의 Progress.Activities에 날짜·Flags로 저장하고, XP는 지급 기록에서 계산합니다. 임의의 합계 XP 필드를 중복 저장하지 않습니다. 새 활동을 추가하려면 양쪽 Flags·점수·검증·병합·화면·검사를 함께 수정합니다.

[장비 300종](SANGMON-ITEMS.md) · [실제 적용 화면](../previews/applied/wardrobe-v1/README.md)