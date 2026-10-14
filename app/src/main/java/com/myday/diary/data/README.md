# Android 데이터·저장을 바꾸려면

| 바꾸려는 것 | 먼저 열 파일 | 역할 |
| --- | --- | --- |
| 기록·블록·장비·레이아웃 모델 | [DiaryModels.kt](DiaryModels.kt) | 데이터와 저장 인터페이스 |
| 파일 저장·이전 Android 기록 이전 | [DiaryStore.kt](DiaryStore.kt) | AtomicFile·동일 파일 잠금·실패 보존 |
| Windows JSON 호환·백업 필드 | [NotebookJson.kt](NotebookJson.kt) | 버전 1~4 읽기·버전 4 쓰기 |
| 사진 입력·EXIF 회전·압축·검증 | [NotebookPhoto.kt](NotebookPhoto.kt) | 긴 변 1600px·JPEG·메타데이터 제거 |
| 위젯·배경화면의 가벼운 설정 캐시 | [DiaryHomeState.kt](DiaryHomeState.kt) | 상몬·장비·테마·완료 수, 본문/사진 제외 |
| 공통 캐릭터·장비·100종 형식 읽기 | [NotebookCatalog.kt](NotebookCatalog.kt) | 생성된 catalog.json 읽기 |
| 생성된 상몬 해금 레벨 | [SangmonIdentity.kt](SangmonIdentity.kt) | 직접 편집 대신 Windows 원본에서 재생성 |

저장 규칙·경험치·레이아웃 병합은 [NotebookRules.kt](../diary/NotebookRules.kt), 화면 상태·저장 요청은 [NotebookController.kt](../diary/NotebookController.kt)입니다. 모델 필드나 ID를 바꾸면 Windows 호환성과 이전 기록 이전도 함께 확인합니다.

개인 기록은 앱 내부 파일이며 공개 저장소에 넣지 않습니다. 자동 백업·마이그레이션 수정은 손상 파일과 저장 실패 때 기존 내용이 유지되는지 확인합니다.

[Android 전체 안내](../README.md) · [공통 자산 생성 도구](../../../../../../../../tools/README.md)
