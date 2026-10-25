# 개발 도구를 바꾸려면

| 할 일 | 실행/수정 파일 | 용도 |
| --- | --- | --- |
| 꾸미기 질문·가구 목록 생성 | [sync-studio.py](sync-studio.py) | shared/studio.json에서 세 앱 카탈로그 생성, --check 지원 |
| 공통 가구 PNG 생성 | [export-studio-art.ps1](export-studio-art.ps1) · [ExportStudioArt.cs](ExportStudioArt.cs) | Windows RoomArt.cs의 자체 벡터 그림을 Android·Mac용 PNG로 내보냄 |
| 세 앱의 공통 메뉴·성장·공방과 PC 형식·색상·제한 수정 | [sync-desktop.py](sync-desktop.py) | shared/에서 네이티브 앱과 서버용 파일 생성, --check로 생성 누락 확인 |
| 기록 도구 버전·보관 한도·응원 문구 수정 | [sync-journal.py](sync-journal.py) | shared/journal-tools.json에서 세 네이티브 앱 정책 생성, --check로 검증 |
| 앱 내 기기 연결의 공통 브라우저 화면 생성 | [sync-device-link.py](sync-device-link.py) | shared/device-link.html을 세 앱에 묶음, --check 지원. [연결·앨범·백업](../docs/DEVICE-ALBUM-BACKUP.md) |
| 선택적 이전 Wi-Fi 전송 도구 | [lan-transfer.py](lan-transfer.py) | Python 표준 라이브러리·묶인 MIT QR 사용. 0.10.0 앱의 기기 연결에는 필요 없음. [이전 사용법](../docs/JOURNAL-TOOLS.md) |
| Mac 앱에 같은 캐릭터 그림 묶기 | [prepare-mac-assets.py](prepare-mac-assets.py) | Windows 원본에서 만든 Android 그림을 Mac 리소스로 staging, Mac 빌드에서 자동 실행 |
| 두 앱의 실제 공통 동작 비교 | [compare-desktop.py](compare-desktop.py) | 네이티브 앱 보고서를 공통 원본·좌표 사례와 비교 |
| 공통 상몬·장비·일기 형식을 Android에 반영 | [export-android-assets.ps1](export-android-assets.ps1) · [ExportAndroidAssets.cs](ExportAndroidAssets.cs) | Windows 원본에서 PNG·썸네일·catalog·해금 목록·검사 자료 생성 |
| 이전 Android debug 앱의 개인 기록 백업 | [backup-android.ps1](backup-android.ps1) | USB/ADB로 읽기 전용 백업, 설치·삭제는 하지 않음 |
| Android 백업을 실제 Windows 코드로 검사 | [WindowsAndroidInterop.cs](WindowsAndroidInterop.cs) | JSON·사진·좌표·성장·장비 호환성 확인 |
| CI 에뮬레이터에서 화면·저장 검사 | [android-device-tests.sh](android-device-tests.sh) | 일회용 CI AVD·캡처·보고서, 개인 휴대폰이 연결된 PC 실행용이 아님 |
| Windows 렌더러의 PNG 프레임을 GIF로 묶기 | [package-preview.py](package-preview.py) | 선택 미리보기 도구 |

## 공통 그림·질문을 수정하는 순서

1. [Windows 상몬/아이템 원본](../windows/Character/README.md), [아이템 이름·설명](../windows/Core/SangmonItems.cs), [100종 질문 원본](../windows/Core/Templates/README.md)을 수정합니다.
2. 공통 일기 질문·색상·제한을 수정했다면 먼저 `python tools/sync-desktop.py`를 실행합니다. Android 자산을 갱신할 때는 Windows에서 저장소 루트 기준 `./tools/export-android-assets.ps1`를 실행합니다. 소품만 수정했다면 `-ItemsOnly`로 본체 자산 생성을 생략할 수 있고, `-ItemSlot head`처럼 한 위치만 생성할 수도 있습니다. 공통 목록과 검사 자료는 항상 갱신합니다.
3. 생성된 `app/src/main/assets/`와 `SangmonIdentity.kt`·`EquipmentCatalog.kt`, v4·v5·v6·v7 검사 자료 변경을 함께 확인합니다. `items/`는 장비함 아이콘, `equipment/`는 착용 위치를 맞춘 투명 레이어이며 300종 모두 경계 잘림을 검사합니다.
4. 해당 플랫폼의 빌드·관련 검사를 실행하고 PR에 수정한 화면·파일을 적습니다.

생성 결과를 직접 고치면 다음 생성 때 덮어써집니다. 경험치 지급량·레벨 계산을 변경할 때는 Windows의 SangmonGrowth와 Android의 NotebookRules도 함께 확인합니다. 개인 백업·임시 빌드·서명 키는 커밋하지 않습니다.

[저장소 첫 화면](../README.md) · [전체 수정 가이드](../docs/EDITING-GUIDE.md)
