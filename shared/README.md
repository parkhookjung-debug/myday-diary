# Mac·Windows 공통 원본

**두 앱에 함께 적용할 내용은 이 폴더에서 수정합니다.** Windows 화면은 WinForms, Mac 화면은 SwiftUI를 유지합니다. 공통 JSON에서 C#·Swift 코드와 Mac 리소스를 생성하며 앱 실행 때 Python·인터넷·서버는 필요하지 않습니다.

| 바꾸려는 내용 | 수정할 원본 |
| --- | --- |
| 일기 100종의 이름·질문·설명·배치 | [catalog/templates.json](catalog/templates.json) |
| 분류 이름·배치 이름·참고 링크 | [desktop.json](desktop.json)의 categories·layouts·references |
| 공통 색상·종이·배경·채팅 말풍선 | [desktop.json](desktop.json)의 design |
| 블록 수·본문·제목·질문 길이·좌표·크기 | [desktop.json](desktop.json)의 limits |
| 채팅 메시지·닉네임·페이지·내역 제한 | [desktop.json](desktop.json)의 chat |
| 두 OS에서 같아야 하는 이동·크기 결과 | [fixtures/layout.json](fixtures/layout.json) |
| 공동 디자인의 원본 커밋·반영 기준 | [design-source.json](design-source.json), [디자인 반영 안내](../docs/DESIGN-SYNC.md) |
| Windows·Android 만렙·주간 보너스·99/200/295일 검증 패턴 | [desktop.json](desktop.json)의 growth·growthProfiles, [성장 안내](../docs/SANGMON-GROWTH.md) |

원본을 수정한 뒤 저장소 루트에서 Python 3.11 이상으로 실행합니다.

```sh
python tools/sync-desktop.py
python tools/sync-desktop.py --check
```

생성된 `windows/Core/Shared/*.g.cs`, `macos/Sources/MyDay/SharedDesktop.g.swift`, `macos/Sources/MyDay/Resources/templates.json`, Android의 `diary/SharedGrowth.g.kt`, 서버의 `shared-chat.g.ts`·`chat_contract_generated.py`는 직접 고치지 않습니다. 원본을 수정하고 생성기를 다시 실행하세요. 생성 결과를 빼먹거나 생성 파일만 고치면 CI에서 거부합니다. 기존 일기·형식 ID는 유지하세요.

Windows만의 배치·버튼은 `windows/UI/`, Mac만의 배치·버튼은 `macos/Sources/MyDay/`에서 수정합니다. 공통 생성기는 일기 파일·초대코드·DB·사용자 사진에 접근하지 않습니다. Android 자산은 기존 `tools/export-android-assets.ps1`에서 생성된 C# 목록을 이용하며, 이 도구는 Android의 사진·캐릭터 자산을 변경하지 않습니다.

[공통 개발 구조·검사·아직 남은 차이](../docs/DESKTOP-SHARED.md)
