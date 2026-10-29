# 일기장 형식

Android에도 원본과 같은 20개 질문·배치를 제공합니다. 일기 탭 → **일기 형식 · 블록 추가**에서 분야·이름·질문·배치 이름을 검색하고 적용합니다. [휴대폰 안내](ANDROID.md)

Windows와 Mac의 **일기 형식 · 블록 추가**를 누르면 자주 쓰는 **20개 형식**을 고를 수 있습니다. 블록은 같은 선택 목록의 **기본** 분류에 있으며 Mac에서는 글·할 일·습관·감정·사진·영상 중 하나를 추가합니다. Mac 기본 블록이나 레이아웃 목록의 ☆를 누르면 **즐겨찾기** 분류에 함께 모입니다. 분야를 선택하거나 제목·질문·배치 이름을 검색하고, 미리보기를 확인한 뒤 **이 형식 추가**를 누릅니다. Ctrl+F로 검색칸을 선택합니다. 배경 선택 옆에서는 **노트·카드·도트** 표면을 따로 바꿉니다.

Mac 영상 블록은 이 Mac의 2GB 이하 MP4·MOV 원본을 연결해 앱 안에서 재생합니다. 원본을 옮기거나 지우면 다시 선택해야 하며 영상 파일 자체는 공통 백업에 포함되지 않습니다.

| 분야 | 수 | 예시 |
| --- | --- | --- |
| 일상 기록 | 2 | 자유 일기, 한 줄 일기 |
| 회고·성장 | 2 | 하루 회고, 주간 회고 |
| 감사·행복 | 2 | 감사 일기, 세 가지 감사 |
| 마음 정리 | 2 | 감정 체크인, 걱정 분류 일기 |
| 학습·독서 | 2 | 코넬 학습 일기, 공부 기록 |
| 목표·계획 | 2 | 불렛 저널, 하루 계획 일기 |
| 생활·루틴 | 2 | 습관 관찰 일기, 수면 기록 |
| 관계·편지 | 2 | 상몬에게 편지, 미래의 나에게 |
| 창작·취향 | 2 | 브레인스토밍 일기, 꿈 기록 |
| 여행·추억 | 2 | 여행 하루 기록, 연말 일기 |

[전체 20개 구성과 참고 자료](JOURNAL-CATALOG.md)를 확인할 수 있습니다. 앱 사용 통계가 없는 상태에서 실측 순위를 주장하지 않고, 반복 사용하기 좋은 대표 용도를 10개 분야에서 2개씩 골랐습니다. 각 형식은 고유한 제목·질문 조합이며, 대략적인 작성 시간과 블록 배치를 미리 보여줍니다.

배치는 **긴 글·질문 카드·본문+메모·타임라인·편지·플래너·코넬 노트·나란히 비교** 8종입니다. 좁은 캔버스에서는 최소 크기를 지키며 세로로 배치합니다.

기존 글·체크·자유 배치는 보존하고 새로운 블록을 추가합니다. 저장하지 않은 새 날짜의 기본 빈 글 칸은 첫 선택 시 교체하며, 빈 페이지에는 형식별 배치를 적용하고 자유 배치로 전환합니다. 기존 자유 배치에서는 형식의 블록들을 원래 블록 아래에 배치합니다. 자동 정렬 페이지에는 새 블록을 추가합니다. 제목 드래그·↘ 크기 조절로 다시 꾸밀 수 있습니다. 블록 한도 200개를 넘으면 일부만 추가하지 않고 전체 추가를 취소합니다.

안내 질문은 빈 입력칸의 도움말로 표시됩니다. 글을 쓰기 시작하면 사라지며 답변란에 질문이 자동 입력되지 않습니다. 형식의 제목·질문과 종이 스타일은 날짜별로 저장하고 JSON 백업에 포함합니다. 형식 선택은 원래 기분·배경·상몬 선택을 바꾸지 않습니다.

새 날짜는 넓은 글 일기 한 칸으로 시작합니다. 이전 저장본에는 템플릿 필드가 없어도 기존 글·블록·체크·배치를 그대로 읽습니다. 상몬에게 편지는 로컬 감정 기록이며 AI 답변은 제공하지 않습니다.

팀 수정: Windows·Mac 공통 원본 `shared/catalog/templates.json`에서 category·id로 형식을 찾고 `python tools/sync-desktop.py`를 실행합니다. `JournalLayouts.cs`는 배치, `TemplateGallery.cs`는 검색·분야·선택 화면, `TemplateOption.cs`는 목록과 배치 그림입니다. 모델 선택 필드는 `PageStyle`, `DiaryBlock.Title`, `DiaryBlock.Prompt`이며 저장 검증은 `windows/Core/DiaryData.cs`에 있습니다.

웹 자료의 주제·구조를 참고하고 한국어 질문은 프로젝트에서 새로 작성했습니다. [Day One](https://dayoneapp.com/guides/tips-and-tutorials/templates/), [Journey](https://journey.cloud/types-of-diary), [Bullet Journal](https://bulletjournal.com/blogs/faq/collections), [에든버러대 회고 모델](https://reflection.ed.ac.uk/reflectors-toolkit/reflecting-on-experience/gibbs-reflective-cycle), [코넬대 노트](https://lsc.cornell.edu/how-to-study/taking-notes/cornell-note-taking-system/) 등을 참고했습니다. 개별 형식의 참고 링크는 전체 목록에 있습니다.

![형식 선택](../previews/windows/windows-journal-templates.png)

![하루 회고 예시](../previews/windows/windows-reflection.png)

![학습 분야](../previews/windows/windows-template-learning.png)

![검색 예시](../previews/windows/windows-template-search.png)

![코넬 형식 적용](../previews/windows/windows-cornell.png)
