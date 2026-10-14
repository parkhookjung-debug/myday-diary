# Android 기능·저장 동작을 바꾸려면

| 바꾸려는 것 | 먼저 열 파일 |
| --- | --- |
| 날짜 이동·블록 편집·장착·저장 실패 상태 | [NotebookController.kt](NotebookController.kt) |
| 경험치·좌표 제한·내 레이아웃·백업 병합 | [NotebookRules.kt](NotebookRules.kt) |
| 회전 시 상태 보존·직렬 백그라운드 저장 | [NotebookViewModel.kt](NotebookViewModel.kt) |

화면은 [ui/diary](../ui/diary/README.md), 실제 JSON·파일·사진은 [data](../data/README.md)에 있습니다. ViewModel은 빠른 입력을 묶어 마지막 내용을 저장하고, 성공한 저장을 기준으로 성장과 장비 상태를 확정합니다.

`DiaryController.kt`는 초기 Preview·기존 검사용입니다. 현재 앱 기능을 수정하려면 **Notebook 파일**을 봅니다.

[단위 검사](../../../../../../test/java/com/myday/diary/diary/NotebookTest.kt) · [실제 저장기 검사](../../../../../../androidTest/java/com/myday/diary/NotebookWriterDeviceTest.kt) · [Android 전체 안내](../README.md)
