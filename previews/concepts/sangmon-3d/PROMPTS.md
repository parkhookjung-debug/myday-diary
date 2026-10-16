# 상몬 3D풍 시안 제작 사양

사용 도구: 기본 `image_gen` 도구. CLI/API fallback은 사용하지 않았습니다.

원본 정체성은 저장소의 `app/src/main/assets/portraits/original.png`를 사용했습니다. 사용자가 제공한 설명 이미지는 데스크톱 펫의 작고 단순한 3D 스타일과 자연스러운 동작 방향을 참고하는 용도입니다.

## 캐릭터 기준

- 두 눈이 몸 위의 두 봉우리 안에 있고, 왼쪽으로 나온 주둥이와 작은 옆입을 유지합니다.
- 하나로 이어진 넓은 아치형 몸과 몸 아래의 빈 공간을 유지합니다.
- 새 팔·손가락·다리·꼬리·뿔·소품을 붙이지 않습니다.
- 아이보리색 무광 클레이/비닐 재질과 부드러운 조명을 사용합니다.
- 같은 얼굴·비율·시점을 유지하면서 포즈와 눈 표정만 바꿉니다.
- 평소 불꽃은 없고, 마지막 터치 반응 시안에서만 짧은 불꽃을 표현합니다.
- 하품에는 새로 벌어진 동그란 입을 만들지 않습니다.

## 포즈 생성 프롬프트

```text
Use case: identity-preserve / stylized-concept. Create a clean game-character pose sheet of this EXACT cream-white 3D Sangmon desktop pet from the reference, arranged as a strict 3 by 3 equal-sized grid with NINE separate full-body copies. No visible grid lines, no text or labels, no props. Preserve the exact paired upright eye bumps with dark oval eyes, long left-projecting snout with its small asymmetrical SIDE mouth fold, wide arched one-piece bridge-shaped blob body with the open inverted arch gap underneath, cream matte vinyl/clay material and subtle warm studio lighting. All nine are the SAME individual with the same volume, colors, eye placement, silhouette and left-facing view; only natural pose and eye expression vary. No arms, hands, fingers, added legs, tail, horns, clothing or accessories. Keep the distinct Sangmon body, not a generic frog or dragon. Full silhouettes inside every cell, consistent neutral framing and scale, enough gutters so no neighboring poses overlap.

Cell order left-to-right, top-to-bottom: 1 relaxed neutral breathing; 2 tiny waddling step by flexing ONLY the existing lower body lobes; 3 a little upward hop with gentle body stretch; 4 curious head/body tilt looking around with the same eyes; 5 a brief happy blink, eyes as simple closed arcs; 6 a subtle sleepy yawn/stretch using only closed eyes and body stretch, KEEP the existing side mouth fold and DO NOT add or open a round mouth; 7 sleeping with the body slightly squashed and eyes closed; 8 being gently picked up/dragged, existing body slightly elongated and eyes attentive, no visible hand; 9 touched reaction with a SINGLE short small golden-orange flame puff projecting left from the existing side mouth, preserving the same face and body. Only cell 9 has a flame. Avoid watermarks, letters, hearts, Z symbols, speech bubbles, motion lines and disconnected specks. Exact 3x3 grid, not a collage or scene.
```

## 최종 배경 정리 프롬프트

선택한 최종 파일은 아래 편집을 거친 불투명 배경 시트입니다.

```text
Precise edit / background cleanup for a polished character presentation board. Preserve all NINE cream 3D Sangmon characters in this 3x3 grid, with their exact face identity, cream vinyl material, silhouettes, poses, order and tiny flame ONLY on the bottom-right character. Replace EVERYTHING outside the nine character silhouettes with a completely clean, uniform opaque warm light-gray #EEECE7 background. REMOVE every stray white ragged fragment, every red/yellow mark, every noisy cutout edge and speck outside the body. Carefully antialias each silhouette against this opaque background. Do not reproduce transparency or checkerboards. Do not turn the noise into fur or effects. Keep the exact smooth, simple toy-body surfaces and their shading. No new arms, fingers, legs, props, text, labels, separators, watermark, hearts, floating symbols or shadows between cells. All nine full bodies must remain clearly separated with generous clean gutters and outer margins; modestly reduce every character's size equally if necessary so each has 12 percent clear cell margins. Same two eye bumps and same left-facing snout with the small side mouth fold throughout. Sleeping and yawning must not acquire any extra round open mouth. Make the output a neat professionally rendered 3x3 character pose presentation image, on an entirely smooth opaque warm gray field.
```
