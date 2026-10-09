# 미리보기 자료

플랫폼별로 나눠 보관합니다. 실행 파일은 GitHub Releases에서 받습니다.

## Android와 초기 화면

`android/`의 `monster-motion`과 `character-motion`은 앱의 캐릭터 그림·포즈 코드를 렌더링한 PNG/GIF입니다. `myday-example.html`과 PNG는 초기 일기 화면을 재현한 예시입니다. 실제 갤럭시 화면 캡처가 아닙니다.

## Windows

| 파일 | 내용 |
| --- | --- |
| `windows/windows-example.png` | 실제 Windows UI에 격리된 예시 기록을 넣은 화면 |
| `windows/windows-photos.png` | 사진·설명과 글 블록을 자유롭게 배치한 실제 Windows UI (풍경은 검증용 생성 이미지) |
| `windows/windows-calendar.png` | 월간 달력·기록한 날·사진 있는 날·이달의 기록 목록 |
| `windows/windows-diary-search.png` | 사진 설명을 전체 날짜에서 검색한 실제 Windows UI |
| `windows/windows-free-layout.png` | 블록을 자유롭게 옮기고 크기를 조절하는 편집 화면 |
| `windows/windows-journal-templates.png` | 일기 형식 100종의 분류·검색·미리보기 화면 |
| `windows/windows-template-learning.png` | 학습 분야 10종 |
| `windows/windows-template-search.png` | 코넬 학습 형식 검색 |
| `windows/windows-cornell.png` | 실제 코넬 배치로 작성한 예시 |
| `windows/windows-reflection.png` | 하루 회고 형식으로 작성한 일기 예시 |
| `windows/windows-pet.png` | 초기 투명 바탕화면 캐릭터 예시 |
| `windows/windows-lively.gif` | 캐릭터 포즈·생동감 있는 동작 예시 |
| `windows/windows-variants-first.gif` | 상몬 1–16번 |
| `windows/windows-variants-second.gif` | 상몬 17–32번 |
| `windows/windows-variants-third.gif` | 상몬 33–48번 |
| `windows/windows-game-styles.gif` | RPG 속성 8종 |
| `windows/windows-variants.gif` | 전체 56종 |

캐릭터 GIF는 실제 앱 렌더러로 그렸습니다. 바탕화면에서 돌아다니는 위치 이동 대신 포즈와 재질 움직임을 보여줍니다. 실제 사용자 일기와 원본 참고 사진은 포함하지 않습니다.

새 미리보기는 Android 자료를 `android/`, Windows 자료를 `windows/`에 넣고 해당 문서 링크를 갱신합니다.

## Windows GIF 다시 만들기

앱은 `--game-preview`, `--variants-first`, `--variants-second`, `--variants-third`, `--variant-preview`로 번호가 붙은 PNG 프레임을 생성합니다. 이를 GIF로 묶는 선택 개발 도구는 `tools/package-preview.py`입니다. 이 도구에만 Python과 Pillow가 필요하며 앱 실행에는 필요하지 않습니다.

```powershell
./windows/build.ps1
$render = Start-Process windows/bin/MyDay.exe -ArgumentList '--game-preview','.bootstrap/game-frames' -Wait -PassThru -WindowStyle Hidden
if ($render.ExitCode -ne 0) { throw 'Render failed' }
python -m pip install Pillow
python tools/package-preview.py .bootstrap/game-frames previews/windows/windows-game-styles.gif
```

명령은 저장소 루트에서 실행합니다. `.bootstrap/`의 중간 프레임은 커밋하지 않고 최종 GIF를 해당 플랫폼 폴더에 넣습니다.
