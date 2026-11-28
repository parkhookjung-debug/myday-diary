# 영상 속 새 상몬 적용 · 0.16.1

사용자가 전달한 `sangmon-2d-to-matching-3d.mp4`의 마지막 3D 모습을 기준으로, 기본 상몬을 새로 그렸습니다. 영상 파일을 재생하거나 흰 배경을 붙인 그림을 올리는 방식이 아니라 투명 배경의 네이티브 그림과 동작 프레임을 사용합니다.

![실제 렌더러의 아홉 장면](poses.png)

통통한 몸통, 넓고 둥근 눈 돌기, 검은 점눈, 낮고 평평한 배 아래, 짧은 발, 작은 혀가 있는 얇은 입선을 반영했습니다. 하품에는 새로 벌어지는 입을 만들지 않고 옆 입선을 유지합니다. 가만히 있을 때는 불꽃이 없고 터치와 짧은 하품 반응에만 불꽃이 나옵니다.

Windows는 `MonsterPainter`와 `ClayMaterial`로 직접 그립니다. 같은 렌더러의 8동작 × 8프레임을 `original.png` 한 장으로 내보내 Android·Mac의 앱·배경화면/바탕화면·놀이터에 사용합니다. Android 홈 위젯의 정적 상몬 그림도 같은 새 프레임을 사용하며, 기존 날짜·응원·할 일·습관 표시와 탭 동작을 유지합니다. 저장 ID `original`과 기존 일기·경험치는 바꾸지 않습니다. 변신·코디·뽑기 자산은 복구하지 않았습니다.

![실제 Windows 성장 화면](windows-growth.png)

![실제 Mac 성장 화면](mac-companion.png)

![실제 Android 성장 화면](android-growth.png)

검증: Windows 538개 검사와 실제 창, Mac 네이티브 빌드·화면·공통 계약, Android 출시 빌드 단위 검사 65개·API35 기기 검사 22개·린트 오류 0개·동일 전용 서명을 확인했습니다. 64프레임은 모든 셀 경계가 투명하며 Windows와 픽셀 단위로 같습니다. [배포 파일별 근거](../../../docs/builds/release-0.16.1-2026-11-28.json).

원본/해시/프레임 규격은 [공통 그림 명세](../../../shared/character-art.json), 수정 위치는 [Windows 캐릭터 안내](../../../windows/Character/README.md)를 보세요.

```powershell
./tools/export-android-assets.ps1 -Focused
./tools/export-android-assets.ps1 -CheckFocused
python tools/prepare-mac-assets.py
python tools/test_character_art.py
```

`-Focused`는 기본 상몬 한 장만 갱신합니다. 이전 64종/장비/도감/백업 검사 자료를 다시 생성하지 않습니다. `-CheckFocused`는 묶인 64프레임의 모든 픽셀을 실제 Windows 렌더 결과와 비교합니다.
