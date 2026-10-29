# 0.11.0 · 하루 꾸미기

Windows·Mac은 사이드바의 **하루 꾸미기**, Android는 일기의 **하루 꾸미기** 칩 또는 **설정·백업 → 하루 꾸미기**를 엽니다. 여섯 기능을 같은 순서로 제공합니다. 인터넷 계정·유료 API·클라우드 배포 없이 사용합니다.

## 1. 사진·스티커

사진을 선택하고 왼쪽/오른쪽 회전, 중앙 정사각형 또는 4:3 자르기, 크림·라벤더·숲 프레임을 적용합니다. 꽃·하트·별·테이프 스티커를 고른 뒤 사진에서 붙일 위치를 누릅니다. **처음으로**는 편집을 초기화하고 **취소**는 기존 사진을 유지합니다. **이 사진 적용**을 눌러야 기록의 사진이 바뀝니다. Windows에서는 사진을 두 번 눌러 바로 열 수도 있습니다.

독립 스티커는 사진 블록으로 추가하고 기존 자유 배치로 이동·크기 조절합니다. 스티커를 사진 위에 붙이는 편집과 독립 블록 추가를 모두 지원합니다. 편집 결과는 긴 변 1,600px·2MB 이하 JPEG로 저장하며 원본 파일과 사진 설명·블록 ID·배치는 유지합니다. 독립 스티커는 크림 바탕을 포함합니다. 자유 영역 자르기·동영상 편집은 이번 범위에 포함하지 않습니다.

## 2. 상몬의 작은 방

화분·의자·러그·조명·테이블·쿠션 6종을 추가하고 마우스나 손가락으로 움직입니다. 가구는 최대 24개, 배경은 3가지입니다. 가구를 선택해 치우거나 배경을 바꿀 수 있습니다. 기존 옷장 소품을 착용한 상몬이 방에서도 숨쉬고 반응합니다. **상몬 쉬기 / 깨우기**는 방을 보는 동안의 휴식 동작을 바꿉니다. 평소에는 불꽃을 뿜지 않고 직접 눌렀을 때만 잠깐 반응합니다.

가구 이름·종류는 공통 카탈로그, 그림은 프로젝트 자체 벡터 렌더러에서 생성합니다. Android·Mac도 같은 그림을 씁니다. 방 꾸미기는 일기 본문이나 기존 300소품의 획득·착용 규칙을 바꾸지 않습니다. 새 가구 6종은 기본 무료입니다.

## 3. 감정 달력

직접 고른 감정을 날짜별 색으로 표시합니다. 이전/다음 달을 보고 날짜를 누르면 해당 일기를 엽니다. 기록이 없는 날짜에는 감정을 추정하지 않습니다. 기존 월간 리포트도 그대로 사용할 수 있습니다.

## 4. 태그·즐겨찾기

현재 일기 날짜에 쉼표로 구분한 태그를 최대 10개·각 20자까지 저장합니다. 앞의 `#`는 생략해도 되고 같은 태그의 대소문자 중복을 제거합니다. **이날을 즐겨찾기**를 선택하고 **이날의 분류 저장**을 누릅니다. 아래 태그 검색·즐겨찾기 필터로 찾은 날짜를 누르면 일기로 이동합니다. 결과는 최신순 최대 100개를 표시합니다. 기존 본문·사진 설명 검색은 달력·검색 메뉴에 있습니다.

## 5. 오늘의 질문

기기 안의 공통 질문 12개 중 날짜에 맞는 하나를 표시합니다. 같은 날짜는 세 앱에서 같은 질문을 제공합니다. **이 질문으로 일기 쓰기**를 누르면 제목·질문이 있는 빈 글 블록을 추가합니다. 이미 추가한 같은 질문은 중복 생성하지 않습니다. 질문 추가 자체로 글 작성 보상을 지급하지 않습니다. 답변은 기존 일기 저장·성장 규칙을 따릅니다.

## 6. 기기별 일기 잠금

기본은 꺼짐입니다. 숫자 6~12자리 PIN을 두 번 입력해 켭니다. 변경·끄기에는 현재 PIN이 필요합니다. 앱을 열 때와 앱에서 5분간 조작하지 않았을 때 잠급니다. **지금 잠그기**도 제공합니다. Android는 배경 전환 때도 잠그고 잠금을 켠 동안 최근 앱/화면 캡처를 제한하며 위젯의 활동 합계를 가립니다.

이 기능은 **앱 화면을 가리는 잠금**입니다. 저장 파일·JSON 백업 자체를 암호화하지 않습니다. PIN 원문은 저장하지 않고 난수 salt와 PBKDF2-HMAC-SHA256(100,000회) 파생값을 기기에 보관합니다. 5회 실패하면 30초 쉬며 이 제한은 재실행 후에도 유지됩니다. 잠금 설정·PIN은 공통 JSON·기기 연결로 보내지 않습니다. PIN을 잊었을 때를 위한 앱 내부 초기화·계정 복구는 이번에 제공하지 않으므로 별도로 기억해 주세요.

## 저장·가져오기

- 새 `Studio` 자료에는 방 배경·가구 위치와 날짜별 태그·즐겨찾기만 들어갑니다. 좌표는 0~900의 정규화 값이며 기기 화면 크기에 맞춰 표시합니다.
- 꾸미기 자료를 사용하는 백업은 **버전 9**입니다. 세 앱 모두 **0.11.0 이상**으로 맞춰 교환하세요. 구버전 앱은 버전 9를 거부하므로 내용을 조용히 잃지 않습니다. 새 앱은 기존 버전 1~8을 계속 읽습니다.
- 가져온 방 배경·가구를 적용하고, 태그·즐겨찾기는 날짜별로 합칩니다. 같은 날짜는 가져온 분류가 우선합니다. Studio가 없는 이전 백업을 가져오면 기존 방·분류를 유지합니다.
- 기존 일기·사진·자유 배치·내 레이아웃·상몬 64종·300소품·성장·재화 내역을 보존합니다. 경제 내역 충돌 검사와 복구 사본 규칙도 유지합니다. 복원으로 XP·재화를 다시 지급하지 않습니다.
- 일일 자동 백업과 QR 전송도 버전 9 자료를 보존합니다. PIN은 기기별 `screen-lock.json`에 따로 있으며 공통 백업에 포함되지 않습니다.

## 어떤 파일을 수정하나요?

| 바꿀 기능 | Windows | Mac | Android |
|---|---|---|---|
| 여섯 화면·버튼 | [StudioWindow.cs](../windows/UI/StudioWindow.cs), [DiaryWindow.Studio.cs](../windows/UI/DiaryWindow.Studio.cs) | [StudioView.swift](../macos/Sources/MyDay/StudioView.swift) | [PhoneStudio.kt](../app/src/main/java/com/myday/diary/ui/diary/PhoneStudio.kt) |
| 사진 변환·스티커 그림 | [PhotoDecor.cs](../windows/Core/PhotoDecor.cs), [편집 화면](../windows/UI/PhotoDecorWindow.cs) | [PhotoDecor.swift](../macos/Sources/MyDay/PhotoDecor.swift) | [PhotoDecor.kt](../app/src/main/java/com/myday/diary/ui/diary/PhotoDecor.kt) |
| 방·태그·즐겨찾기 저장·검증 | [StudioData.cs](../windows/Core/StudioData.cs), [DiaryData.cs](../windows/Core/DiaryData.cs) | [StudioData.swift](../macos/Sources/MyDay/StudioData.swift), [백업](../macos/Sources/MyDay/WindowsCompatibility.swift) | [StudioData.kt](../app/src/main/java/com/myday/diary/data/StudioData.kt), [백업](../app/src/main/java/com/myday/diary/data/NotebookJson.kt) |
| 질문·가구 이름·감정 목록 | [shared/studio.json](../shared/studio.json) → [sync-studio.py](../tools/sync-studio.py)로 세 앱 생성 | 같은 공통 원본 | 같은 공통 원본 |
| 가구 그림 | [RoomArt.cs](../windows/Core/RoomArt.cs) → [export-studio-art.ps1](../tools/export-studio-art.ps1) | [prepare-mac-assets.py](../tools/prepare-mac-assets.py)로 묶음 | 생성된 [studio/ PNG](../app/src/main/assets/studio/) |
| PIN·유휴 잠금·화면 보호 | [PinVault.cs](../windows/Core/PinVault.cs), [ScreenPrivacy.cs](../windows/UI/ScreenPrivacy.cs) | [ScreenPrivacy.swift](../macos/Sources/MyDay/ScreenPrivacy.swift) | [PinVault.kt](../app/src/main/java/com/myday/diary/data/PinVault.kt), [PhonePrivacy.kt](../app/src/main/java/com/myday/diary/ui/diary/PhonePrivacy.kt) |
| 검사 | [StudioTests.cs](../windows/Tests/StudioTests.cs), [백업 교환](../tools/WindowsStudioInterop.cs) | [StudioVerification.swift](../macos/Sources/MyDay/Verification/StudioVerification.swift) | [StudioTest.kt](../app/src/test/java/com/myday/diary/diary/StudioTest.kt), [기기 검사](../app/src/androidTest/java/com/myday/diary/PhoneNotebookDeviceTest.kt) |

Mac의 `.swift` 파일은 `macos/Sources/MyDay/`, Android의 `data/`, `ui/`, `diary/`는 `app/src/main/java/com/myday/diary/` 기준입니다. 생성된 그림과 `.g.*` 파일은 직접 수정하지 않고 원본에서 다시 생성합니다.

[실제 화면·완료된 검사·실행 파일](../previews/studio/README.md) · [기존 연결·앨범·백업](DEVICE-ALBUM-BACKUP.md)
