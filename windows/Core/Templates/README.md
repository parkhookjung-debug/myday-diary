# 분야별 일기 형식 수정

제목·질문·설명·종이·배치를 분야별로 관리합니다. 화면 디자인은 UI/TemplateGallery.cs, 배치 알고리즘은 Core/JournalLayouts.cs에서 수정합니다.

[전체 수정 가이드](../../../docs/EDITING-GUIDE.md) · [프로젝트 첫 화면](../../../README.md)

| 바꾸려는 것 | 파일 | 역할 |
| --- | --- | --- |
| 일상 기록 | [DailyFormats.cs](DailyFormats.cs) | Daily 형식 |
| 회고·성장 | [ReflectionFormats.cs](ReflectionFormats.cs) | Reflection 형식 |
| 감사·행복 | [GratitudeFormats.cs](GratitudeFormats.cs) | Gratitude 형식 |
| 마음 정리 | [EmotionFormats.cs](EmotionFormats.cs) | Emotion 형식 |
| 학습·독서 | [LearningFormats.cs](LearningFormats.cs) | 공부·코넬·독서 질문 |
| 목표·계획 | [PlanningFormats.cs](PlanningFormats.cs) | Planning 형식 |
| 생활·루틴 | [WellbeingFormats.cs](WellbeingFormats.cs) | Wellbeing 형식 |
| 관계·편지 | [RelationshipFormats.cs](RelationshipFormats.cs) | Relationship 형식 |
| 창작·취향 | [CreativeFormats.cs](CreativeFormats.cs) | Creative 형식 |
| 여행·추억 | [MemoryFormats.cs](MemoryFormats.cs) | Memory 형식 |
| 분류 이름·출처·생성 도우미 | [TemplateCatalog.cs](TemplateCatalog.cs) | 분류와 T·S·D·H 등 생성 함수 |
| 전체 목록 문서 출력 | [CatalogExport.cs](CatalogExport.cs) | --template-catalog 연결 |
| 기본 6종·전체 목록 결합 | [DiaryTemplates.cs](../DiaryTemplates.cs) | Originals와 All |

기존 형식의 ID는 유지하고 이름·설명·질문을 바꿉니다. 형식을 추가하거나 제거하면 Tests.cs의 개수·분류 검증과 docs/JOURNAL-CATALOG.md도 갱신합니다.
