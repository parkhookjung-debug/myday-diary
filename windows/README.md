# MyDay Windows

Windows에서 실행하는 C# / Windows Forms 일기 앱과 투명한 바탕화면 캐릭터입니다.
Android 프로젝트와 별도 폴더에서 개발하며 외부 패키지, 브라우저, Node.js가 필요하지 않습니다.

## 실행

Windows 10/11과 .NET Framework 4.8 이상을 사용합니다.
`실행.cmd`를 더블클릭하면 처음에는 소스를 빌드한 뒤 바탕화면에 캐릭터가 나타납니다.
캐릭터를 한 번 클릭하면 일기 창이 열립니다.
빌드 후에는 `bin/MyDay.exe`만 더블클릭해도 실행할 수 있습니다.
GitHub CI의 Windows artifact에서도 실행 파일을 받을 수 있습니다.
[다운로드용 Windows ZIP 및 EXE](https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.3.9-preview)은 압축을 풀고 `MyDay.exe`를 실행합니다.

## 기능

- DM 스타일 기록함: 날짜별 목록·본문 미리보기·현재 날짜 강조, 목록 클릭으로 기록 이동
- [월간 달력·일기 검색](../docs/CALENDAR-SEARCH.md): 왼쪽 **달력 · 일기 검색** 또는 Ctrl+F로 열기, 저장한 날짜·사진 있는 날 표시, 본문·사진 설명 전체 검색, 날짜·결과 클릭으로 일기 열기
- 아이보리 종이와 청록색 포인트, 날짜 표지, 노트·카드·도트 표면. [디자인 변경 안내](../docs/WINDOWS-DESIGN.md)
- [일기 형식 100종](../docs/JOURNAL-FORMATS.md): 10개 분야·검색·대략적인 작성 시간·질문과 배치 미리보기. [전체 100종 목록과 참고 자료](../docs/JOURNAL-CATALOG.md)
- 긴 글·질문 카드·본문+메모·타임라인·편지·플래너·코넬 노트·비교형 8종 배치. 빈 페이지에 적용하며 기존 자유 배치에는 원래 블록을 보존해 아래에 추가
- 날짜별 글 일기, 할 일, 습관 체크, 감정 기록
- [사진 첨부](../docs/PHOTOS.md): 아래 **＋ 사진**으로 추가, 설명 입력, 사진 교체, 자유 배치·크기 조절, 사진을 포함한 JSON 백업·복원
- 블록 추가, 순서 이동, 너비 변경, 삭제 확인
- 자유 배치: 제목을 잡아 드래그 이동, 오른쪽 아래 ↘로 크기 조절, 날짜별 위치·크기 저장
- [내 레이아웃](../docs/MY-LAYOUTS.md): **내 레이아웃 → 현재 배치 저장**으로 이름 붙여 보관, 다른 날짜에 불러오기, 배치 미리보기·이름 검색·이름 변경·삭제, JSON 백업에 포함
- 배경 테마 3종과 오늘의 마음 기록
- 입력 후 자동 저장, 날짜 이동·종료 전 저장 확인
- JSON 백업과 가져오기 (같은 날짜는 확인 후 교체)
- 투명한 불꽃 몬스터: 자유로운 방향 전환, 경계 반사, 눈 깜박임, 숨 쉬기, 클릭 시 일기 열기, 드래그
- 걷기·통통 뛰기·두리번거리기·쉬기·짧은 졸기 사이를 전환하며, 몸 눌림·늘어남·기울임과 눈빛·불티를 표현
- 마우스가 가까이 오면 멈춰 바라보고, 드래그 후 내려놓으면 가볍게 통통 튀는 반응
- 평소에는 불꽃이 없고, 클릭하거나 내려놓으면 약 0.7초만 불을 뿜습니다. 가끔 하품하며 약 0.35초 동안 작은 불꽃이 나오고, 자동 하품은 최소 30초 간격입니다.
- 상몬 48종: 기존 스케치 8종과, 기본 몸·두 눈이 솟은 머리·옆입·발 사이 아치를 유지하며 외형을 덧붙인 40종을 제공합니다. 자연·테마·장비 외형에 고양이·토끼·여우·강아지·곰·양·사슴·용·나비·아홀로틀·상어·공작·거북·문어·로봇·선인장 16종을 추가했습니다. 전체 목록은 [상몬 외형 디자인](../docs/SANGMON-DESIGN.md)에서 확인할 수 있습니다.
- 게임 스타일 8종: 화염·빙결·전격·맹독·그림자·암석·해류·비전. 원래 상몬 몸과 얼굴에 선명한 색, 단계가 나뉜 명암, 재질 문양과 움직이는 속성 효과를 적용합니다. 기존 외형과 합쳐 자유롭게 고르는 56종입니다. 캐릭터 우클릭 → **게임 스타일**에서 바로 선택할 수 있습니다.
- 캐릭터 우클릭: 일기 열기, 이동 켜기/끄기, 다른 창 위에 표시, 불 뿜기, 숨기기
- 알림 영역 아이콘: 일기 열기, 캐릭터 표시/숨기기, 모두 종료

일기 창의 X는 기록을 저장한 뒤 창만 숨깁니다. 캐릭터는 계속 움직이며 다시 한 번 클릭하면 일기를 열 수 있습니다.
알림 영역의 **모두 종료**는 앱과 캐릭터를 함께 종료합니다. 캐릭터 클릭은 짧은 불꽃 반응과 함께 일기를 열고, 우클릭 메뉴나 일기 안의 몬스터에서도 잠깐 불을 뿜게 할 수 있습니다. 마우스를 가까이 대기만 할 때는 불을 뿜지 않습니다.
감정 기록은 일기 저장 기능이며 AI 대화는 없습니다.
[자유 배치 사용·개발 안내](../docs/FREE-LAYOUT.md): 위쪽 **배치 편집**을 누르고 제목으로 이동하거나 ↘로 크기를 바꾼 뒤 **편집 완료**로 글 입력을 다시 켭니다. **자동 정리**는 블록을 겹치지 않게 배치합니다. **자동 정렬**로 전환했다가 자유 배치로 돌아와도 저장된 위치를 유지합니다.
일기 왼쪽 캐릭터 아래 선택 메뉴 또는 캐릭터 우클릭 → **캐릭터 버전**에서 모습을 바꿉니다. 앱과 바탕화면에 함께 적용하며 재실행해도 선택을 유지합니다. 캐릭터 선택은 날짜별이 아닌 앱 전체 설정입니다.
이 버전의 캐릭터는 Windows 위에 뜨는 투명 창입니다. Windows 배경화면을 교체하지 않습니다.
Android 기록과 자동 동기화하지 않습니다. 영상·시작 시 자동 실행은 아직 추가하지 않았습니다.

[상몬 성장](../docs/SANGMON-GROWTH.md): 왼쪽 **성장** 또는 상몬 우클릭 → **상몬 성장**에서 경험치와 다음 보상을 확인합니다. 실제 기록일에 하루 +10 XP, 50 XP마다 레벨 업하며 추가 보상 8종을 해금·장착합니다. 총 64종이고 기존 56종은 계속 자유롭게 선택합니다.

[아이템 장비함](../docs/SANGMON-ITEMS.md): 왼쪽 **아이템** 또는 상몬 우클릭 → **아이템 장착**에서 검 6종·마법 보주 6종을 선택합니다. 모두 바로 장착하고 두 슬롯을 함께 쓰거나 각각 해제합니다. 일기와 바탕화면에 함께 적용하고 저장·백업에 포함합니다.

## 기록 위치

`%LOCALAPPDATA%/MyDay/Windows/diary.json`에 보관합니다. 프로그램 폴더를 옮겨도 같은 Windows 계정의 기록을 유지합니다.
이전 저장본은 `diary.json.bak`에 남습니다. 기록을 수정하거나 PC를 옮기기 전 **기록 백업**으로 별도 파일을 보관하세요.
내 레이아웃이나 빈 사진 자리만 저장한 기록은 버전 2 형식(v0.3.7-preview 이상)입니다. 성장 기록을 저장하면 버전 3 형식이며 v0.3.8-preview 이상에서 엽니다. 기존 버전 1·2 기록과 백업도 읽습니다. 아이템 장비를 저장한 기록은 버전 4이며 v0.3.9-preview 이상으로 열어주세요. 버전 3의 성장 기록도 읽고 보존합니다.
손상된 기록을 읽지 못하면 앱이 오류를 표시하고 종료하며 빈 기록으로 덮어쓰지 않습니다.

## 팀 디자인·기능 수정

| 영역 | 파일 |
| --- | --- |
| 색상·글꼴·버튼·카드 | `UI/Design.cs` |
| 일기 창 배치·날짜·연결 | `UI/DiaryWindow.cs` |
| 날짜별 기록 목록의 표시 | `UI/HistoryRow.cs` |
| 달력·검색·결과 창 | `UI/JournalBrowser.cs`, `UI/MonthCalendar.cs` |
| 달력 날짜·본문 검색·미리보기 | `Core/DiaryBrowse.cs` |
| 달력·검색 단위 검증 | `BrowseTests.cs` |
| 내 레이아웃 모델·검증·적용·병합 | `Core/SavedLayouts.cs` |
| 내 레이아웃 목록·검색·관리·미리보기 | `UI/MyLayoutsWindow.cs`, `UI/LayoutNameDialog.cs`, `UI/SavedLayoutDiagram.cs` |
| 내 레이아웃 저장·재사용 검증 | `SavedLayoutTests.cs` |
| 상몬 성장·레벨·보상·병합 | `Core/SangmonGrowth.cs`, `UI/GrowthWindow.cs`, `UI/DiaryWindow.Growth.cs` |
| 성장 보상 의상·검증 | `Character/RewardSkins.cs`, `GrowthTests.cs` |
| 아이템 모델·장착·백업 | `Core/SangmonItems.cs`, `UI/DiaryWindow.Items.cs` |
| 아이템 그림·장비함·미리보기 | `Character/ItemPainter.cs`, `UI/EquipmentWindow.cs`, `UI/ItemView.cs` |
| 장비 저장·슬롯·그림 경계 검증 | `ItemTests.cs` |
| 날짜 표지 | `UI/JournalCover.cs` |
| 형식 선택·미리보기 | `UI/TemplateGallery.cs` |
| 형식의 블록 제목·질문·구성 | `Core/DiaryTemplates.cs` |
| 분야별 형식 데이터·참고 링크 | `Core/Templates/` |
| 8종 배치 알고리즘 | `Core/JournalLayouts.cs` |
| 목록과 배치 그림 | `UI/TemplateOption.cs` |
| 자유 캔버스·드래그·리사이즈 핸들 | `UI/DiaryBoard.cs` |
| 일기 블록 UI | `UI/BlockCard.cs` |
| 사진 미리보기 | `UI/PhotoView.cs` |
| 사진 입력·회전·압축·검증 | `Core/DiaryPhoto.cs` |
| 사진 백업·손상·용량 테스트 | `PhotoTests.cs` |
| 기록 모델·검증·저장·백업 | `Core/DiaryData.cs` |
| 논리 좌표·자동 정렬·크기 범위 | `Core/DiaryLayout.cs` |
| 스케치 캐릭터 그림·포즈 | `Character/MonsterPainter.cs` |
| 캐릭터 버전 ID·이름 | `Character/MonsterVariants.cs` |
| 원형을 유지하는 추가 외형·장식 | `Character/MonsterAdditions.cs` |
| RPG 속성 색상·명암·재질·효과 | `Character/GameSkins.cs` |
| 행동 전환·가속·반응 | `Character/PetBehavior.cs` |
| 바탕화면 이동·드래그·투명 창 | `Character/DesktopPet.cs` |
| 앱 안의 캐릭터 | `UI/MonsterView.cs` |

캐릭터는 Android 버전과 같은 스케치 좌표를 사용하지만 플랫폼별 렌더링 코드로 관리합니다. 생동감 있는 추가 행동은 현재 Windows 버전에 적용했습니다.

![Windows 캐릭터 움직임 코드로 렌더링한 예시](../previews/windows/windows-lively.gif)

![자유 배치 편집 예시](../previews/windows/windows-free-layout.png)

![상몬 1–16번 움직임 예시](../previews/windows/windows-variants-first.gif)

![상몬 17–32번 움직임 예시](../previews/windows/windows-variants-second.gif)

![상몬 33–48번 움직임 예시](../previews/windows/windows-variants-third.gif)

![게임 스타일 상몬 8종](../previews/windows/windows-game-styles.gif)

참고 자료와 새 형태의 특징은 [상몬 형태 연구](../docs/SANGMON-DESIGN.md)에 정리했습니다.

참고 사진의 게시물 화면·계정 정보는 저장소에 포함하지 않고 캐릭터를 코드로 그렸습니다. 이전 기록 파일에 캐릭터 설정이 없으면 기본형을 사용합니다.
잠시 제공했던 물방울·납작·길쭉·네모·구름·쌍둥이 선택은 지느러미·등껍질·꼬리·수정·복슬·꽃 버전으로 읽습니다. 기록 내용은 유지하며 다음 저장부터 새 ID를 사용합니다.

## 빌드 및 확인

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File windows/build.ps1
$process = Start-Process windows/bin/MyDay.exe -ArgumentList '--self-test' -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw 'Tests failed' }
```

자동 테스트는 실제 기록과 분리된 임시 폴더에서 저장 보존·날짜 분리·백업·손상 파일 처리·애니메이션을 확인합니다.
사진 테스트는 원본 삭제 후 백업 복원, EXIF 회전·축소·투명 영역, 손상된 사진으로 기존 기록을 덮어쓰지 않는 처리, 사진 용량 제한을 확인합니다.
달력·검색 테스트는 윤년과 연도 경계·날짜 범위, 사진 표시·모든 날짜의 본문/사진 설명 검색·긴 글 미리보기·재로드·기록 보존을 확인합니다. 네이티브 검증은 월 이동·검색 지우기·결과 없음·121일의 결과 페이지 이동·작은 창·저장 후 결과/빈 날짜 열기·취소를 확인합니다.
내 레이아웃 검증은 본문·사진·체크 제외, 기존 날짜 보존과 새 날짜 독립성, 자동/자유 배치 재사용, 버전 2 저장·백업·이전 기록 읽기, 이름 변경·충돌·병합·삭제·용량/좌표 거부를 확인합니다. 네이티브 검증은 이름 입력·미리보기·검색·관리·작은 창·기존/새 날짜 적용·파일 잠금 시 목록 복구·사진 자리 채우기를 확인합니다.
`--smoke-test <출력 폴더>`는 격리된 예시 데이터로 입력·체크·순서 이동·날짜 이동·재로드와 네이티브 투명 창을 확인하고 PNG를 만듭니다.
다중 모니터·고배율 DPI·절전 복귀·우클릭 메뉴·드래그는 실제 사용 환경에서도 확인해주세요.

기반 기술: [Microsoft Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/), [UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow).

성장 검증은 하루 한 번 지급·빈 기록 제외·실제 입력과 배치 변경의 구분·레벨별 해금·기존 외형 유지·버전 3 저장·백업 합치기·중복/잘못된 날짜 거부를 확인합니다. 네이티브 창에서는 잠긴 메뉴·성장 카드·8종 장착·파일 잠금 시 경험치 복구·재시도·작은 창을 확인합니다.

아이템 검증은 12종 저장·두 슬롯 독립성·해제·옛 백업 유지·버전 4 저장과 이전 기록 호환·잘못된 장비 복구·모든 검/보주 조합의 투명 창 경계를 확인합니다. 네이티브 장비함에서는 분류·12종 장착·해제·경험치 미지급·파일 잠금 시 복구·작은 창·날짜 전환·백업을 검증합니다.
