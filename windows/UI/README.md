# Windows 화면 수정

색·배치·카드·선택 창을 수정하는 폴더입니다. 일기 저장 규칙은 Core, 상몬과 아이템 그림은 Character에서 관리합니다.

[전체 수정 가이드](../../docs/EDITING-GUIDE.md) · [프로젝트 첫 화면](../../README.md)

| 바꾸려는 것 | 파일 | 역할 |
| --- | --- | --- |
| 전체 색·글꼴·버튼 | [Design.cs](Design.cs) | Accent·Font·둥근 버튼·카드 |
| 일기 메뉴·버튼·날짜 연결 | [DiaryWindow.cs](DiaryWindow.cs) | 메인 창 구성과 기능 연결 |
| 블록의 입력창·체크·사진 버튼 | [BlockCard.cs](BlockCard.cs) | 블록 카드의 표시와 입력 |
| 마우스 배치·크기 조절 | [DiaryBoard.cs](DiaryBoard.cs) | 드래그·핸들·캔버스 |
| 날짜 표지·기록 행 | [JournalCover.cs](JournalCover.cs), [HistoryRow.cs](HistoryRow.cs) | 표지 장식과 목록 행 |
| 일기 형식 선택 | [TemplateGallery.cs](TemplateGallery.cs), [TemplateOption.cs](TemplateOption.cs) | 100종 분류·검색·미리보기 |
| 달력·검색 | [JournalBrowser.cs](JournalBrowser.cs), [MonthCalendar.cs](MonthCalendar.cs) | 결과 창과 달력 |
| 사진과 빈 사진 자리 | [PhotoView.cs](PhotoView.cs) | 이미지 비율과 썸네일 |
| 내 레이아웃 관리 | [MyLayoutsWindow.cs](MyLayoutsWindow.cs), [LayoutNameDialog.cs](LayoutNameDialog.cs), [SavedLayoutDiagram.cs](SavedLayoutDiagram.cs) | 목록·이름 입력·배치 그림 |
| 성장 화면·보상 장착 | [GrowthWindow.cs](GrowthWindow.cs), [DiaryWindow.Growth.cs](DiaryWindow.Growth.cs) | 경험치·보상 카드와 연결 |
| 아이템 장비함·장착 연결 | [EquipmentWindow.cs](EquipmentWindow.cs), [DiaryWindow.Items.cs](DiaryWindow.Items.cs) | 두 슬롯과 저장·적용 |
| 앱 안 캐릭터·아이템 미리보기 | [MonsterView.cs](MonsterView.cs), [ItemView.cs](ItemView.cs) | 타이머와 렌더링 연결 |
