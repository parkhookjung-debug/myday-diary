# 일기 형식 공통 원본 안내

Windows·Mac의 일기 이름·질문·설명은 [shared/catalog/templates.json](../../../shared/catalog/templates.json)에서 함께 수정합니다. 이전 분야별 `*Formats.cs`와 기본 6종을 공통 원본으로 옮겼습니다. ID와 내용은 유지했습니다.

| 바꿀 내용 | 파일 |
| --- | --- |
| 전체 100종·분야별 질문 | [공통 templates.json](../../../shared/catalog/templates.json)의 category·id로 찾기 |
| 분류 이름·배치 이름·참고 링크 | [shared/desktop.json](../../../shared/desktop.json) |
| 원본 생성·검사 | [sync-desktop.py](../../../tools/sync-desktop.py) |
| Windows 블록 적용 | [DiaryTemplates.cs](../DiaryTemplates.cs) |
| Windows 배치 계산 | [JournalLayouts.cs](../JournalLayouts.cs) |
| 검색·선택 화면 | [TemplateGallery.cs](../../UI/TemplateGallery.cs) |
| 목록 문서 출력 | [CatalogExport.cs](CatalogExport.cs) |

수정 후 `python tools/sync-desktop.py`를 실행하고 생성 파일도 함께 커밋합니다. `Core/Shared/Templates.g.cs`를 직접 고치면 검사에서 거부합니다. Android 반영은 이후 기존 `tools/export-android-assets.ps1`을 실행합니다.

[공통 개발 구조·호환 범위](../../../docs/DESKTOP-SHARED.md) · [전체 수정 가이드](../../../docs/EDITING-GUIDE.md)
