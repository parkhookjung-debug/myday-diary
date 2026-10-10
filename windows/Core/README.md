# Windows 기능과 저장 규칙

기록 모델·파일 저장·검색·레이아웃·경험치·장비 목록을 관리합니다. 화면의 모양은 UI, 그림은 Character에서 수정합니다.

[전체 수정 가이드](../../docs/EDITING-GUIDE.md) · [프로젝트 첫 화면](../../README.md)

| 바꾸려는 것 | 파일 | 역할 |
| --- | --- | --- |
| 일기 데이터·백업·버전 | [DiaryData.cs](DiaryData.cs) | 날짜·블록·사진·성장·장비의 저장과 검증 |
| 사진 입력·압축·회전 | [DiaryPhoto.cs](DiaryPhoto.cs) | 입력 파일·JPEG 변환·용량 |
| 좌표·크기·자동 정렬 | [DiaryLayout.cs](DiaryLayout.cs) | 블록의 논리 좌표 계산 |
| 검색·월간 달력 데이터 | [DiaryBrowse.cs](DiaryBrowse.cs) | 검색 대상·날짜 범위·미리보기 |
| 내 레이아웃 저장·적용 | [SavedLayouts.cs](SavedLayouts.cs) | 내용을 제외한 배치 캡처·이름·병합 |
| 형식 생성·적용·검색 | [DiaryTemplates.cs](DiaryTemplates.cs) | 기본 형식과 전체 목록·블록 생성 |
| 분야별 일기 제목·질문 | [README.md](Templates/README.md) | 분야별 *Formats.cs 찾기 |
| 편지·코넬 등 배치 | [JournalLayouts.cs](JournalLayouts.cs) | 형식별 배치 계산 |
| 경험치·해금 조건 | [SangmonGrowth.cs](SangmonGrowth.cs) | DailyXP·LevelXP·보상 레벨·중복 지급 방지 |
| 아이템 이름·설명·슬롯 | [SangmonItems.cs](SangmonItems.cs) | 12종 목록·장착·해제·백업 복원 |
| 클릭과 드래그 구분 | [PetGesture.cs](PetGesture.cs) | 포인터 입력 상태 |
