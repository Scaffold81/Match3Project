# GDD 08 — Промпты для фонов основного игрового уровня (ChatGPT / DALL·E)

> Цель: фоны игрового поля (за доской Match-3) для каждой страны.
> Стиль: тёплая, живописная (painterly) подача в духе TAPCLAP «Pirate Treasures» —
> насыщенный свет, выраженная глубина сцены (передний/средний/задний план) —
> но в теме исследователя-археолога «Lost Expedition», БЕЗ пиратской атрибутики.
> Список стран: **Egypt → Greece → China → Maya → India → Russia**
>
> Синхронизировано с `GDD_03_Content.md` / `GDD_00_Index.md` (актуализированы под 6 стран).
> Отдельная копия этого файла для удобного редактирования: `Lost_Expedition_Background_Prompts.md` (выгружен отдельно).

---

## 📐 Safe Zone (Вариант A) — композиция под разные форм-факторы

Экраны телефонов (~9:16–9:20) и планшетов (~3:4–4:3) обрезают портретный кадр по-разному.
Чтобы не пришлось делать отдельные ассеты под каждый форм-фактор, держим весь критичный
контент (главный landmark + обрамляющие элементы переднего плана) в **центральном
безопасном квадрате 1:1**, вертикально расположенном по центру кадра, во всю ширину.

Всё, что выше и ниже этого квадрата (лишняя портретная высота) — это "запас" (bleed):
небо, земля, дымка — некритичные элементы, которые можно безопасно обрезать на более
узких экранах без потери смысла композиции.

```
CRITICAL SAFE-ZONE RULE: this artwork will be cropped differently on phones (tall ~9:19.5)
and tablets (~3:4). Keep ALL critical focal content — the main midground landmark and the
key foreground framing elements — inside a centered 1:1 square safe zone (full canvas width,
vertically centered) within the portrait canvas. The extra portrait height above and below
that central square must contain only expendable atmospheric filler (sky, ground, haze,
distant blur) that can be safely cropped out on narrower screens without breaking the
composition's meaning or losing any storytelling elements.
```

Этот блок добавлен в style-lock и учтён в каждом промпте ниже.

---

## 🎭 РАЗДЕЛ 0 — Style-lock промпт (отправить первым)

```
You are a visual art director for a mobile casual Match-3 puzzle game called "Lost Expedition."

The game concept: the player is an archaeologist-explorer who travels across ancient civilizations
— Egypt, Ancient Greece, Han China, Maya Mexico, Mughal-era India, and medieval Kievan Rus / old Russia —
discovering lost artifacts through puzzle gameplay.

Visual direction for BACKGROUND ART specifically (game-level backgrounds, sit behind the match-3 board):
- Warm, painterly digital-gouache style — rich saturated colors, soft golden-hour lighting
  (reference: TAPCLAP's "Pirate Treasures" background art quality and lighting, but NOT pirate/nautical subject matter)
- Strong sense of depth: clear foreground framing elements, midground focal landmark, soft atmospheric
  haze in the background — parallax-ready layering
- Archaeological-adventure mood: excavation sites, ancient ruins, ancient architecture, exploration —
  NOT ships, NOT pirates, NOT treasure chests with skulls
- Semi-realistic painted textures, not flat cartoon, not photorealistic
- Cozy, inviting, adventurous — casual mobile game feel, appropriate for 25-45 audience
- No characters, no UI, no text baked into the image — center-lower area must stay clear/open
  for the match-3 board overlay

CRITICAL SAFE-ZONE RULE: this artwork will be cropped differently on phones (tall ~9:19.5)
and tablets (~3:4). Keep ALL critical focal content — the main midground landmark and the
key foreground framing elements — inside a centered 1:1 square safe zone (full canvas width,
vertically centered) within the portrait canvas. The extra portrait height above and below
that central square must contain only expendable atmospheric filler (sky, ground, haze,
distant blur) that can be safely cropped out on narrower screens without breaking the
composition's meaning or losing any storytelling elements.

Please confirm you understand this direction before I give you country-specific tasks.
```

---

## 🖼️ РАЗДЕЛ 1 — Фоны игрового уровня по странам

Общий каркас (используется для всех стран, меняются только акценты в квадратных скобках):

```
A vibrant, painterly mobile match-3 game background illustration in a warm digital-gouache style
(lighting and depth quality like TAPCLAP's "Pirate Treasures", archaeological-adventure subject matter,
NOT pirate/nautical). Depicts [ЛОКАЦИЯ].
Warm golden-hour lighting, rich saturated colors, semi-realistic painted textures, not flat cartoon,
not photorealistic.
Strong foreground-midground-background depth with layered parallax elements:
foreground — [ДЕТАЛИ ПЕРЕДНЕГО ПЛАНА], midground — [ГЛАВНЫЙ ОБЪЕКТ/ЛЕНДМАРК],
background — [ДЕТАЛИ ФОНА] in soft atmospheric haze.
Archaeological excavation mood — exploration, discovery, ancient ruins, no ships, no pirates.
Adventurous, cozy, inviting mood, no characters, no UI elements, no text.
Keep the main landmark and key foreground elements inside a centered 1:1 square safe zone
(full width, vertically centered) — the extra top and bottom portrait area must contain
only expendable sky/ground/haze filler that can be safely cropped on narrower screens.
[ORIENTATION] composition, open empty space in the center-lower area reserved for the match-3 game board.
```

`[ORIENTATION]` — генерировать в двух вариантах:
- `Vertical 9:16 mobile portrait`
- `Horizontal 16:9 landscape / tablet`

---

### 1.1 Egypt

```
A vibrant, painterly mobile match-3 game background illustration in a warm digital-gouache style
(lighting and depth quality like TAPCLAP's "Pirate Treasures", archaeological-adventure subject matter,
NOT pirate/nautical). Depicts an archaeological excavation site in the Egyptian desert at golden hour.
Warm golden-hour lighting, rich saturated colors (sandy golds, deep lapis blue, sun orange),
semi-realistic painted textures, not flat cartoon, not photorealistic.
Strong foreground-midground-background depth with layered parallax elements:
foreground — excavation tools, ropes, unearthed stone fragments framing the edges,
midground — a half-uncovered sphinx head and sandstone columns with hieroglyph carvings,
background — pyramids silhouetted against a warm orange-purple sunset sky in soft atmospheric haze.
Archaeological excavation mood — exploration, discovery, ancient ruins.
Adventurous, cozy, inviting mood, no characters, no UI elements, no text.
Keep the sphinx head, columns and foreground tools inside a centered 1:1 square safe zone
(full width, vertically centered) — the extra sky above and sand below must be expendable
filler that can be safely cropped on narrower phone screens.
Vertical 9:16 mobile portrait composition, open empty space in the center-lower area reserved for the match-3 game board.
```

*(для лендшафт-версии заменить последнюю строку на `Horizontal 16:9 landscape / tablet composition, ...`)*

---

### 1.2 Greece

```
A vibrant, painterly mobile match-3 game background illustration in a warm digital-gouache style
(lighting and depth quality like TAPCLAP's "Pirate Treasures", archaeological-adventure subject matter,
NOT pirate/nautical). Depicts an archaeological dig among ancient Greek ruins on a Mediterranean cliffside.
Warm golden-hour lighting, rich saturated colors (white marble, sky blue, olive green, bronze),
semi-realistic painted textures, not flat cartoon, not photorealistic.
Strong foreground-midground-background depth with layered parallax elements:
foreground — fallen marble column fragments, olive branches, an archaeologist's brush and lantern framing the edges,
midground — a partially excavated marble temple with fluted columns and a broken pediment,
background — the Aegean sea and white-washed cliffside village fading into soft atmospheric haze.
Archaeological excavation mood — exploration, discovery, ancient ruins.
Adventurous, cozy, inviting mood, no characters, no UI elements, no text.
Keep the marble temple and foreground column fragments inside a centered 1:1 square safe zone
(full width, vertically centered) — the extra sky above and ground below must be expendable
filler that can be safely cropped on narrower phone screens.
Vertical 9:16 mobile portrait composition, open empty space in the center-lower area reserved for the match-3 game board.
```

---

### 1.3 China

```
A vibrant, painterly mobile match-3 game background illustration in a warm digital-gouache style
(lighting and depth quality like TAPCLAP's "Pirate Treasures", archaeological-adventure subject matter,
NOT pirate/nautical). Depicts an archaeological excavation site near an ancient Han-dynasty imperial tomb.
Warm golden-hour lighting, rich saturated colors (jade green, imperial red, ink black, gold),
semi-realistic painted textures, not flat cartoon, not photorealistic.
Strong foreground-midground-background depth with layered parallax elements:
foreground — unearthed terracotta warrior fragments, bamboo scaffolding, lanterns framing the edges,
midground — a row of excavated terracotta soldiers and a red-lacquered pagoda gate,
background — misty green mountains and distant pagoda rooftops fading into soft atmospheric haze.
Archaeological excavation mood — exploration, discovery, ancient ruins.
Adventurous, cozy, inviting mood, no characters, no UI elements, no text.
Keep the terracotta soldiers and pagoda gate inside a centered 1:1 square safe zone
(full width, vertically centered) — the extra sky above and ground below must be expendable
filler that can be safely cropped on narrower phone screens.
Vertical 9:16 mobile portrait composition, open empty space in the center-lower area reserved for the match-3 game board.
```

---

### 1.4 Maya

```
A vibrant, painterly mobile match-3 game background illustration in a warm digital-gouache style
(lighting and depth quality like TAPCLAP's "Pirate Treasures", archaeological-adventure subject matter,
NOT pirate/nautical). Depicts an archaeological excavation site at a Maya jungle temple ruin.
Warm golden-hour lighting, rich saturated colors (deep jungle green, obsidian black, turquoise, feather red),
semi-realistic painted textures, not flat cartoon, not photorealistic.
Strong foreground-midground-background depth with layered parallax elements:
foreground — jungle vines, carved stone glyphs, an excavation rope and torch framing the edges,
midground — a stepped stone pyramid partially reclaimed by jungle, carved serpent motifs,
background — dense rainforest canopy and misty mountains fading into soft atmospheric haze.
Archaeological excavation mood — exploration, discovery, ancient ruins.
Adventurous, cozy, inviting mood, no characters, no UI elements, no text.
Keep the stepped pyramid and foreground carvings inside a centered 1:1 square safe zone
(full width, vertically centered) — the extra sky above and ground below must be expendable
filler that can be safely cropped on narrower phone screens.
Vertical 9:16 mobile portrait composition, open empty space in the center-lower area reserved for the match-3 game board.
```

---

### 1.5 India

```
A vibrant, painterly mobile match-3 game background illustration in a warm digital-gouache style
(lighting and depth quality like TAPCLAP's "Pirate Treasures", archaeological-adventure subject matter,
NOT pirate/nautical). Depicts an archaeological excavation site at an ancient Indian sandstone fort/palace ruin.
Warm golden-hour lighting, rich saturated colors (saffron orange, deep red, turquoise, gold),
semi-realistic painted textures, not flat cartoon, not photorealistic.
Strong foreground-midground-background depth with layered parallax elements:
foreground — carved sandstone rubble, marigold garlands, an excavation lantern and brush framing the edges,
midground — a weathered sandstone archway with intricate jali lattice carving and a domed pavilion,
background — a distant hilltop fort and warm hazy sky fading into soft atmospheric haze.
Archaeological excavation mood — exploration, discovery, ancient ruins.
Adventurous, cozy, inviting mood, no characters, no UI elements, no text.
Keep the sandstone archway and domed pavilion inside a centered 1:1 square safe zone
(full width, vertically centered) — the extra sky above and ground below must be expendable
filler that can be safely cropped on narrower phone screens.
Vertical 9:16 mobile portrait composition, open empty space in the center-lower area reserved for the match-3 game board.
```

---

### 1.6 Russia

```
A vibrant, painterly mobile match-3 game background illustration in a warm digital-gouache style
(lighting and depth quality like TAPCLAP's "Pirate Treasures", archaeological-adventure subject matter,
NOT pirate/nautical). Depicts an archaeological excavation site near an ancient Kievan Rus kurgan burial mound
and old wooden fortress ruins.
Warm golden-hour lighting, rich saturated colors (gilded gold domes, deep teal-blue, birch white, warm red brick),
semi-realistic painted textures, not flat cartoon, not photorealistic.
Strong foreground-midground-background depth with layered parallax elements:
foreground — unearthed ancient silver jewelry, birch branches, an excavation lantern and brush framing the edges,
midground — a weathered wooden fortress gate and a partially excavated burial mound with old stone carvings,
background — onion-domed towers and a birch forest fading into soft warm atmospheric haze.
Archaeological excavation mood — exploration, discovery, ancient ruins.
Adventurous, cozy, inviting mood, no characters, no UI elements, no text.
Keep the fortress gate and burial mound inside a centered 1:1 square safe zone
(full width, vertically centered) — the extra sky above and ground below must be expendable
filler that can be safely cropped on narrower phone screens.
Vertical 9:16 mobile portrait composition, open empty space in the center-lower area reserved for the match-3 game board.
```

---

## 📐 Советы по использованию

1. Сначала отправь **Раздел 0** (style-lock, включает Safe Zone правило), дождись подтверждения от ChatGPT.
2. Для каждой страны — сначала портретную версию (9:16), затем ландшафтную (заменить последнюю строку промпта).
3. Если стиль "уезжает" между странами — добавляй в начало каждого промпта: `"Keep the exact same lighting, rendering technique and depth style as the previous background, only change the location and color palette."`
4. Размер генерации: `1024x1792` для портрета, `1792x1024` для ландшафта.
5. Средний план (midground landmark) должен быть узнаваемым, но не быть точной копией реального охраняемого памятника — избегаем прямого копирования конкретных культурных объектов (модель может отказать или дать неточный результат).
6. **Safe Zone проверка после генерации:** открой готовый фон, мысленно (или в фигме/фотошопе) наложи центральный квадрат 1:1 — если landmark вылезает за его пределы по бокам, а не по верху/низу, стоит перегенерировать с более явным акцентом на "centered square" в промпте.
7. В Unity: настраиваем `Canvas Scaler` → `Scale With Screen Size`, `Match` ближе к `Height` (0.5–1), чтобы на широких планшетах обрезка шла по бокам safe zone, а не по критичному контенту сверху/снизу.
