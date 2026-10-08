# 파일·폴더 안내

Android와 Windows 소스, 팀 문서, 미리보기, 다운로드용 빌드를 나눠 관리합니다. Android Studio의 표준 프로젝트 구조를 유지하므로 루트에서 Gradle을 실행합니다.

```text
myday-diary/
├── app/                       Android 앱
│   └── src/
│       ├── main/
│       │   ├── java/com/myday/diary/
│       │   │   ├── data/      기록 모델·저장
│       │   │   ├── diary/     날짜·입력·일기 동작
│       │   │   ├── platform/  홈 위젯·배경화면 설정 연결
│       │   │   └── ui/        화면·디자인·캐릭터·배경화면
│       │   └── res/           색·크기·위젯 레이아웃·설정
│       ├── debug/             Android Studio 화면 Preview
│       └── test/              일기·캐릭터 단위 테스트
├── windows/                   네이티브 Windows 앱
│   ├── Core/                  기록·저장·입력 제스처
│   ├── UI/                    일기 창·블록·디자인
│   ├── Character/             상몬 그림·행동·외형·게임 재질
│   ├── Program.cs             시작·세션·테스트 명령 연결
│   ├── Tests.cs               저장·캐릭터·네이티브 검증
│   ├── build.ps1              .NET Framework 빌드
│   └── 실행.cmd               소스에서 빌드 후 실행
├── docs/                      실행·디자인·협업 문서
├── previews/
│   ├── android/               Android PNG·GIF·초기 HTML 예시
│   └── windows/               Windows PNG·상몬 GIF
├── .github/                   Android/Windows CI·협업 양식
├── tools/                     선택 개발 도구: PNG 프레임 → GIF
├── gradle/                    Gradle Wrapper
├── README.md                  시작 안내·다운로드·전체 기능
├── CONTRIBUTING.md            협업 방법
└── LICENSE                    MIT 라이선스
```

## 기능별 수정 위치

| 원하는 작업 | Android | Windows |
| --- | --- | --- |
| 색·글꼴·간격 | `ui/design/`, `res/values/` | `UI/Design.cs` |
| 일기 화면·블록 | `ui/diary/`, `ui/components/` | `UI/DiaryWindow.cs`, `UI/BlockCard.cs`, `UI/DiaryBoard.cs` |
| 자유 배치·크기 조절 | 현재 Windows에 적용 | `Core/DiaryLayout.cs`, `UI/DiaryBoard.cs` |
| 기록·저장 | `data/`, `diary/` | `Core/DiaryData.cs` |
| 캐릭터 그림 | `ui/character/` | `Character/MonsterPainter.cs`, `MonsterAdditions.cs` |
| 게임 속성 재질 | 현재 Windows에 적용 | `Character/GameSkins.cs` |
| 움직임 | `ui/character/CharacterAnimation.kt` | `Character/PetBehavior.cs` |
| 홈/바탕화면 | 위젯 Provider, Wallpaper Service, `ui/wallpaper/` | `Character/DesktopPet.cs` |
| 검증 | `app/src/test/` | `windows/Tests.cs` |

Android 표의 Java 경로는 `app/src/main/java/com/myday/diary/` 기준입니다. Windows 경로는 `windows/` 기준입니다. 문서와 미리보기는 각 플랫폼 코드의 실행 파일이 아닙니다.

## 실행 파일 배포

GitHub Releases에 플랫폼별로 배포합니다.

- Windows: `MyDay-Windows-v0.3.4-preview.zip`에 `MyDay.exe`, 실행 안내, 라이선스
- Android: `MyDay-Android-v0.3.0-preview.apk` 개발용 debug 빌드
- GitHub에서 자동 제공하는 Source code ZIP/TAR: 같은 태그의 전체 소스

빌드 출력은 소스 폴더에 커밋하지 않습니다. `windows/bin/`, `app/build/`는 로컬 빌드가 생성하고, CI 결과와 Releases에서 실행 파일을 받습니다. 캐시·기기별 SDK 경로·개인 기록·참고 사진·인증 파일도 저장소에 포함하지 않습니다.

## 팀 문서

- [Android 실행과 기능](ANDROID.md)
- [Windows 실행과 개발](../windows/README.md)
- [Windows 디자인 수정](WINDOWS-DESIGN.md)
- [일기장 형식 100종 사용](JOURNAL-FORMATS.md)
- [100종 전체 목록·참고 자료](JOURNAL-CATALOG.md)
- [Android 디자인 수정](DESIGN.md)
- [상몬 외형·게임 스타일](SANGMON-DESIGN.md)
- [일기 자유 배치](FREE-LAYOUT.md)
- [5명 팀 역할](TEAM.md)
- [미리보기 설명](../previews/README.md)
