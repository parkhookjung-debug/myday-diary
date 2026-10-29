# Mac·Windows 공통 개발

선택한 방식은 **기능·데이터 규칙은 공통 원본, 화면과 OS 기능은 플랫폼별 구현**입니다. 기존 Windows C#·WinForms와 Mac Swift·SwiftUI를 유지합니다. 전체 앱을 새 프레임워크로 옮기거나 사용자의 일기 파일을 자동 변환하지 않습니다.

## 작업 흐름

```text
shared/desktop.json ───────────────┐
shared/catalog/templates.json ────┼─ tools/sync-desktop.py
shared/fixtures/layout.json ──────┘         ├─ Windows C# 생성 파일
                                          └─ Mac Swift 생성 파일·템플릿 리소스
```

공통 파일은 [shared/README.md](../shared/README.md)에서 바로 고릅니다. 원본 수정 후 `python tools/sync-desktop.py`를 실행하고 생성 결과도 함께 커밋합니다. Python은 개발용이며 배포한 앱에는 필요하지 않습니다. 빌드·검사·로컬 실행에 유료 서비스나 클라우드 계정을 추가하지 않았습니다.

## 이번에 함께 관리하는 기능

- 반복해서 쓰기 좋은 일기 형식 100종, 10개 분류, 8개 배치의 표시 이름.
- 배경 세 색, 글자·보조 글자·강조·종이·사이드바·채팅 말풍선 색상. 글꼴·창 크기·네이티브 입력 조작은 OS 화면 코드에서 관리합니다.
- 한 날짜의 블록 수, 본문·제목·질문의 UTF-16 길이, 저장 좌표·최소/최대 크기·기본 높이.
- 채팅 메시지·페이지·참여자·응답 크기 제한. 실제 HTTP 전송·취소와 OS 입력 화면은 각각 유지합니다. 서버에 일기·사진을 자동 전송하지 않습니다.
- 공통 입력을 Windows와 Mac의 실제 이동·크기 조절 코드에 넣어 같은 결과가 나오는지 검사합니다.

## 어디를 수정하나요?

| 변경 | 공통 원본 | Windows 화면·연결 | Mac 화면·연결 |
| --- | --- | --- | --- |
| 일기 질문·형식 | [templates.json](../shared/catalog/templates.json) | [DiaryTemplates.cs](../windows/Core/DiaryTemplates.cs), [TemplateGallery.cs](../windows/UI/TemplateGallery.cs) | [Templates.swift](../macos/Sources/MyDay/Templates.swift) |
| 색상·배경 | [desktop.json / design](../shared/desktop.json) | [Design.cs](../windows/UI/Design.cs) | [SharedDesktop.g.swift](../macos/Sources/MyDay/SharedDesktop.g.swift)의 생성된 DesktopPalette 사용 |
| 블록 이동·저장 제한 | [desktop.json / limits](../shared/desktop.json) | [DiaryLayout.cs](../windows/Core/DiaryLayout.cs), [DiaryData.cs](../windows/Core/DiaryData.cs) | [DiaryLayout.swift](../macos/Sources/MyDay/DiaryLayout.swift), [WindowsCompatibility.swift](../macos/Sources/MyDay/WindowsCompatibility.swift) |
| 채팅 입력·목록 제한 | [desktop.json / chat](../shared/desktop.json) | [ChatClient.cs](../windows/Chat/ChatClient.cs), [ChatWindow.cs](../windows/UI/ChatWindow.cs) | [ChatClient.swift](../macos/Sources/MyDay/ChatClient.swift), [ChatView.swift](../macos/Sources/MyDay/ChatView.swift) |

예를 들어 공부 일기의 질문은 `shared/catalog/templates.json`에서 `cornell-notes`를 찾아 `sections[].prompt`를 바꾸고 생성기를 실행합니다. 크림 배경을 바꾸려면 `shared/desktop.json`의 `design.Backgrounds[0]`을 수정합니다. 저장된 theme 번호와 기존 템플릿 ID는 그대로 유지합니다.

## 실제 앱 결과 비교

`python -m unittest discover -s tools -p test_sync_desktop.py -v`는 한 질문을 바꾸면 두 앱에 반영되는지, 생성 파일 직접 수정·중복 ID·잘못된 배치·UTF-16 길이를 검사합니다.

Windows `MyDay.exe --desktop-contract 파일.json`과 Mac `MyDay --desktop-contract 파일.json`은 실제로 앱이 읽은 20개 형식의 모든 필드·질문, 좌표 계산 결과, 제한과 색상을 내보냅니다. 개인 일기 대신 공통 테스트 자료만 사용합니다.

```sh
python tools/compare-desktop.py windows-report.json mac-report.json
```

두 결과는 서로만 비교하지 않고 공통 원본의 기대값과도 비교합니다. `.github/workflows/chat-compatibility.yml`은 생성 누락 검사, 두 네이티브 앱 빌드·채팅 검사, 앱별 결과 비교, 실제 Mac 버전 4 백업을 현재 Windows 직렬화 코드로 읽는 검사를 실행합니다. 공개 저장소의 표준 러너에서만 실행하며 서버를 배포하지 않습니다.

2026-10-19 소스 커밋 `11dff9d`의 [공통 개발 CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/39268012217)에서 서버, Windows, Apple Silicon Mac, 실제 결과 비교, Mac → Windows 백업 검사 5개 작업이 모두 통과했습니다. 실행 페이지의 Artifacts에서 두 앱의 빌드와 화면·검사 결과를 받을 수 있습니다. 산출물은 7일 보관하며 Intel Mac 빌드·실기기 실행은 별도 확인 대상입니다.

## 세 플랫폼 공통화

2026-10-23 변경부터 Mac에도 같은 상몬 64종·300소품·8슬롯·Lv.100·출석·등급 선물 공방·버전 8 백업과 내 레이아웃 관리를 구현합니다. Android에는 기존 공통 서버에 연결하는 라운지를 추가합니다. 메뉴·인사말은 `shared/app-shell.json`, 색상은 `shared/desktop.json`, 등급 정책은 `shared/collection.json`에서 세 앱으로 생성합니다.

[Windows·Mac·Android 수정 위치, 화면·기능 표, 백업과 실행 파일 안내](THREE-PLATFORMS.md).
