# Промпты для ChatGPT / DALL-E — Lost Expedition

> Все промпты написаны на английском — для лучшего качества генерации.
> Разбиты по задачам. Каждый промпт самодостаточен — можно копировать и вставлять напрямую.

---

## 🎭 РАЗДЕЛ 1 — СТИЛЬ И НАПРАВЛЕНИЕ (задать первым)

Отправь этот промпт **первым**, чтобы зафиксировать стиль. Потом ссылайся на него в остальных.

---

```
You are a visual art director for a mobile casual Match-3 puzzle game called "Lost Expedition."

The game concept: the player is an archaeologist-explorer who travels across ancient civilizations — Egypt, Ancient Greece, Han China, Maya/Aztec Mexico, and Inca Peru — discovering lost artifacts through puzzle gameplay.

Visual style guidelines:
- Bright, clean 2D art — legibility over detail
- Warm, inviting color palette — casual mobile game feel, NOT dark or gritty
- Slightly painterly style — like high-quality mobile game concept art (reference: Royal Match, Candy Crush Saga)
- UI design: minimalist with cultural accents per country
- Characters: stylized, cute but not childish — approachable for adults 25-45 years old, predominantly female audience
- Animations feel: soft, organic, bouncy (think DOTween OutBack/OutBounce)

Each country has its own color palette:
- Egypt: warm sandy golds, deep lapis blue, sun orange
- Greece: white marble, sky blue, olive green, bronze
- China: jade green, imperial red, ink black, gold
- Maya: deep jungle green, obsidian black, turquoise, feather red
- Peru: terracotta, silver, mountain purple, gold

Please confirm you understand this style direction before I give you specific tasks.
```

---

## 💎 РАЗДЕЛ 2 — КОНЦЕПТ ФИШЕК

---

### 2.1 Набор фишек — Египет (стартовая страна)

```
Create a set of 6 gem/tile icons for a Match-3 mobile game, Egypt ancient civilization theme.
Art style: bright 2D casual mobile game icons, slightly glossy, clean shapes, warm color palette.
Each gem should be immediately recognizable at small size (approx 80x80px on mobile screen).

The 6 gem types with their visual concepts:
1. RED gem — golden Egyptian scarab beetle, shiny gold and red, top-down view
2. BLUE gem — lapis lazuli stone, deep royal blue with gold veins, faceted gem shape
3. GREEN gem — emerald, rich green, faceted with small hieroglyph engraving
4. YELLOW gem — sandstone piece, sandy warm yellow, slightly rough texture with cartouche symbol
5. PURPLE gem — amethyst crystal, purple faceted gem, small Eye of Horus
6. ORANGE gem — copper disc, warm orange-brown, stamped with an ankh symbol

All icons must:
- Have consistent size and weight visually
- Include a soft drop shadow
- Have a subtle glow/highlight on top
- Look satisfying to "match" and collect
- Style reference: Royal Match gem designs but with Egyptian cultural symbols

Render all 6 gems on a transparent or neutral dark background, arranged in a 2x3 grid.
```

---

### 2.2 Набор фишек — Греция

```
Create a set of 6 gem/tile icons for a Match-3 mobile game, Ancient Greece theme.
Art style: bright 2D casual mobile game icons, slightly glossy, clean shapes, Mediterranean color palette.

The 6 gem types:
1. RED/BRONZE — bronze coin, warm reddish-brown, stamped with Greek profile
2. BLUE/MARBLE — white marble fragment, cool blue-white, with classic meander pattern
3. GREEN/LAUREL — laurel wreath medallion, olive green, circular shape
4. YELLOW/COIN — gold Greek tetradrachm coin, shiny gold, with owl symbol
5. PURPLE/AMPHORA — small purple amphora, elegant shape, with wave ornament
6. ORANGE/FLAME — Olympic torch flame, warm orange, with classical column base

Same quality requirements as Egypt set: consistent size, drop shadow, top highlight, readable at small size.
Arrange all 6 on neutral background in 2x3 grid.
```

---

### 2.3 Суперфишки (все страны — Египет)

```
Create 5 special "super gem" power-up icons for a Match-3 game with Egyptian archaeology theme.
These appear when a player makes special matches (4 in a row, 5 in a row, L-shape, T-shape).
They must look noticeably more powerful and special than regular gems.

Art style: same bright 2D casual mobile style as the regular Egyptian gems, but with extra visual energy — glowing edges, special effects, more complex design.

The 5 super gems:
1. HORIZONTAL ARROW (clears whole row) — an Egyptian painter's brush, horizontal orientation, golden with blue lapis handle, energy trail effect going left and right
2. VERTICAL ARROW (clears whole column) — an archaeological scraper tool, vertical orientation, bronze metal, energy trail going up and down  
3. COLOR BOMB (clears all gems of one color) — golden amulet/talisman, circular, glowing with rainbow energy, surrounded by small orbiting gems
4. BOMB 3x3 (explosion) — a small archaeologist's shovel/pick, dark iron with gold trim, with a starburst explosion effect behind it
5. MEGA BOMB 5x5 (large explosion) — a bundle of dynamite sticks tied together, with Egyptian symbols carved on them, large fiery explosion effect

Each icon needs: glowing outline effect, particles/sparkles, feels powerful but still matches the Egyptian art style.
Display all 5 on dark archaeological stone texture background.
```

---

## 🏔️ РАЗДЕЛ 3 — ПРЕПЯТСТВИЯ

---

### 3.1 Визуал препятствий

```
Create visual concepts for 4 obstacle types for a Match-3 puzzle game with archaeological excavation theme.
Each obstacle goes on top of or around a regular gem tile. Style: 2D casual mobile, bright, readable.

The 4 obstacles (each with 2 damage states: fresh and cracked/damaged):

1. ICE — "Frozen ground layer"
   State 1 (HP 2): clean ice crystal layer, pale blue, slightly transparent, gem visible underneath through ice
   State 2 (HP 1): cracked ice with yellow cracks, more opaque, ready to shatter

2. BOX — "Hard rock/stone block"  
   State 1 (HP 3): solid sandstone block, sandy brown, faint hieroglyph carving on surface
   State 2 (HP 2): cracked block with visible damage lines
   State 3 (HP 1): heavily cracked, small pieces breaking off

3. CHAIN — "Chains wrapping the gem"
   State 1 (HP 2): thick bronze chain links wrapping around the gem, padlock symbol
   State 2 (HP 1): one chain broken, padlock open, gem partially visible

4. ROCK — "Monolithic stone"
   State 1 (HP 4): massive dark granite-like rock, immovable, small archaeological markings
   State 2 (HP 3): first crack
   State 3 (HP 2): multiple cracks, dust particles
   State 4 (HP 1): barely holding together, crumbling edges

Display each obstacle type in a row showing all damage states. Background: game board grid tile.
```

---

## 🎮 РАЗДЕЛ 4 — ИГРОВОЕ ПОЛЕ (GAMEPLAY SCREEN)

---

### 4.1 Полный скриншот игрового поля — Египет

```
Create a UI mockup/concept art for a mobile Match-3 puzzle game screen (portrait orientation, 390x844px iPhone size).

Game: "Lost Expedition" — archaeological excavation theme, Egypt chapter.

LAYOUT (top to bottom):
━━━ TOP BAR ━━━
- Left: pause button (⏸ icon in golden rounded square)
- Center: level title "Stage 3 — Level 2" in clean white font with golden outline
- Right: move counter showing "24" moves remaining (hourglass icon + number)
- Far right: coin counter (gold coin icon + "1,250")

━━━ GAME BOARD ━━━ (main area, square-ish, centered)
- 7x7 grid with DIAMOND shape (corners are hidden/empty, creating a diamond/rhombus visible area)
- Board background: sandy desert excavation pit texture, warm sandy tones
- Grid lines: subtle, thin, slightly golden
- Some cells have gems: mix of 6 Egyptian gem types (scarab, lapis, emerald, sandstone, amethyst, copper)
- 2 ice obstacles visible (pale blue crystalline overlay on some gems)
- 1 chain obstacle (bronze chains around a gem)
- One gem is selected/highlighted (slight glow + scale up effect)
- One horizontal arrow super gem visible (glowing brush icon)

━━━ BOTTOM PANEL ━━━
- Left side: objective tracker showing "Lapis Lazuli: 12/25" with gem icon + progress bar
- Right side: 4 boost buttons in a row (brush, scraper, bomb, magnifier icons), each with a small counter badge

VISUAL STYLE:
- Desert/sand color palette: warm golds, sandy browns, lapis blues
- Egyptian hieroglyph border decoration around the game board
- Warm ambient lighting from top
- Background (outside board): blurred desert with pyramids silhouette

Make it look like a polished mobile game screenshot, highly detailed, production-ready concept art.
```

---

### 4.2 Вариант — Греция

```
Same layout as previous Match-3 game screen mockup, but for the Ancient Greece chapter.

Changes from Egypt version:
- Color palette: Mediterranean — white, sky blue, olive green, marble textures
- Board background: archaeological dig in a marble ruin, white stone tiles with Greek meander border
- Gems: Greek-themed (bronze coin, marble, laurel, gold tetradrachm, amphora, torch)
- Background (outside board): blurred Greek seaside with white columns and Aegean sea
- Decorative border: Greek key/meander pattern in white and blue
- Title: "Greece — Stage 2 — Level 1"
- Obstacle theme: frozen gems show as cracked marble, chains appear as worn iron

Keep same UI layout and proportions. Production-ready mobile game mockup.
```

---

## 🗺️ РАЗДЕЛ 5 — КАРТА УРОВНЕЙ (STAGE MAP)

---

### 5.1 Экран карты — Египет

```
Create a UI mockup for a mobile game "Stage Map" screen (portrait orientation, 390x844px).

Game: "Lost Expedition" — the map shows progression through Egypt chapter levels.

LAYOUT:
- Vertically scrollable map screen
- Background: stylized top-down view of Egyptian desert landscape — sandy dunes, palm trees, Nile river glimpse, pyramid in distance
- Warm golden-sand color palette

LEVEL NODES (zigzag path from bottom to top):
- Show 6 stage nodes total
- Node design: rounded hexagonal buttons, 90x90px
- Bottom node (Stage 1): COMPLETED — golden color, shows "★★★" (3 stars) 
- Stage 2: COMPLETED — golden, "★★☆" (2 stars)
- Stage 3: COMPLETED — golden, "★☆☆" (1 star)  
- Stage 4: CURRENT/UNLOCKED — bright pulsing glow, "?" icon, slightly larger
- Stage 5: LOCKED — grey with padlock icon
- Stage 6 (BONUS): LOCKED — slightly different shape, gold trim, small artifact/trophy icon

CONNECTING PATH:
- Dotted golden path connecting nodes in zigzag (alternating left-right positions)
- Completed path segments: solid gold/bright
- Locked path segments: grey, dashed

COUNTRY HEADER:
- At top: "🏛️ EGYPT" banner with chapter artwork
- Golden decorative frame

BOTTOM UI:
- Player avatar + name
- Coin counter
- Lives counter (hearts: ❤️❤️❤️❤️❤️)

Make it look warm, inviting, satisfying — like Royal Match or Candy Crush Saga map screen but with Egyptian archaeological theme.
```

---

## 📋 РАЗДЕЛ 6 — ПОПАПЫ

---

### 6.1 Попап старта уровня (LevelTaskPopup)

```
Create a UI popup mockup for a mobile Match-3 game (popup size approx 320x480px on dark overlay background).

Popup: "Level Start / Task" popup — shown before each level begins.
Game theme: Lost Expedition, Egypt chapter.

POPUP LAYOUT:
- Rounded rectangle card, warm sandy/golden color, Egyptian border decoration
- TOP: character illustration — friendly female archaeologist in khaki outfit and pith helmet, holding a clipboard, slightly surprised/excited expression. Style: cute casual game character, 2D illustration, NOT anime.
- TITLE: "Stage 3 — Level 2" in bold, clean font
- OBJECTIVES SECTION (middle):
  - Row 1: lapis lazuli gem icon + "Collect Lapis Lazuli" + "0 / 25"
  - Row 2: ice crystal icon + "Clear the Ice" + "0 / 8"
  - Each row in a clean card/chip design
- BUTTON: large "PLAY ▶" button, golden gradient, Egyptian hieroglyph trim

The popup should look polished, inviting. The character should feel friendly and guide the player.
Dark semi-transparent overlay background behind the popup.
```

---

### 6.2 Экран поражения (LevelResultView)

```
Create a UI popup mockup for a mobile Match-3 game "Level Failed" screen.
Game theme: Lost Expedition, Egypt chapter.

LAYOUT (full-screen overlay, dark semi-transparent background):
- CENTER CARD: rounded rectangle, warm dark tone with subtle Egyptian pattern
- TOP: same female archaeologist character but SAD/disappointed expression — slumped shoulders, head down, a single tear maybe. Still cute, not tragic.
- TITLE TEXT: "Not Enough Moves!" — friendly font, NOT aggressive
- LIVES COUNTER: heart icons row showing "❤️❤️❤️☆☆" (3 out of 5 lives remaining), small label "Lives: 3/5"
- TWO BUTTONS:
  Left: "TRY AGAIN 🔄" — primary button, golden/warm color, costs 1 life (small heart -1 badge on button)
  Right: "MAP 🗺" — secondary button, outlined style, muted tone, no life cost
- Small text below: "Try again costs 1 life"

Tone: gentle, encouraging — "you can do it!" not punishing. Character's sad expression should be cute/charming.
```

---

### 6.3 Попап наград за этап (StageRewardPopup)

```
Create a UI popup mockup for a mobile Match-3 game "Stage Complete - Rewards" popup.
Game theme: Lost Expedition, Egypt chapter.

LAYOUT:
- Celebratory popup, bright, festive
- TOP: confetti/sparkles particle effects, golden stars burst
- TITLE: "Stage Complete! 🎉" in large celebratory font, golden color
- SUBTITLE: "Stage 3 — The Sand Dunes"
- REWARDS SECTION (middle): 3 reward items in a row, each as a card:
  Card 1: gold coin stack icon + "+200 Coins" label
  Card 2: brush tool icon (boost) + "Brush ×1" label  
  Card 3: magnifier icon (boost) + "Magnifier ×1" label
  Each card: white/light background, golden border, subtle glow
- CLAIM BUTTON: large, bright golden "CLAIM ✨" button, slightly pulsing glow effect

The popup should feel rewarding and satisfying — player should WANT to see this screen.
Background: dark overlay with golden particle rain effect.
```

---

## 👤 РАЗДЕЛ 7 — ПЕРСОНАЖ-АРХЕОЛОГ

---

### 7.1 Главный персонаж — концепт

```
Create a character concept sheet for the main character of a mobile casual game "Lost Expedition."

Character: Female archaeologist-explorer, the player's guide throughout the game.

DESIGN REQUIREMENTS:
- Age: appears 28-35 years old
- Style: cute but adult — NOT childish, NOT anime. Reference style: characters from Royal Match, June's Journey
- Outfit: khaki/beige field jacket, practical pants, brown boots, pith helmet (classic explorer hat), a canvas satchel/bag
- Features: warm friendly face, warm brown hair (partially tied up under helmet), small round glasses optional
- Personality shown through design: curious, enthusiastic, smart, adventurous

EXPRESSIONS (show 4 on the concept sheet):
1. HAPPY/EXCITED — arms up, big smile, holding artifact (used on map/reward screens)
2. FOCUSED/DETERMINED — studying a map or clipboard (used in level task popup)
3. SAD/DISAPPOINTED — slumped, pouty face (used on level failed screen)
4. TRIUMPHANT — holding artifact up, radiating light (used on stage/country complete screens)

STYLE: 2D flat illustration with clean outlines, vibrant but not neon colors, soft shading. 
Show character in full body on left, head/expressions on right.
Background: neutral light warm grey.
```

---

## 🎨 РАЗДЕЛ 8 — ОБЩАЯ СЦЕНА / KEY ART

---

### 8.1 Key Art — промо изображение

```
Create key art / promotional illustration for a mobile game "Lost Expedition."

Scene: Female archaeologist explorer (cute, adult, khaki outfit, pith helmet) standing at the edge of an archaeological excavation pit in the Egyptian desert. She's holding up a glowing golden artifact (scarab amulet). 

In the pit below her: colorful gem tiles arranged in a Match-3 grid pattern, glowing with magical energy. The gems are: ruby red, lapis blue, emerald green, sandy yellow, amethyst purple, copper orange.

Background: Egyptian desert at golden hour sunset, pyramids silhouetted in the distance, dramatic sky with warm oranges and purples.

STYLE: 
- Mobile game promotional art quality
- Bright, vibrant, cinematic lighting
- 2D illustration with painterly quality (not photorealistic)
- Aspect ratio: portrait 9:16 (mobile screen)
- Text space: leave clean area at top 15% for game logo placement

Mood: adventurous, exciting, magical discovery — this should make someone WANT to download and play the game.
```

---

## 📐 СОВЕТЫ ПО ИСПОЛЬЗОВАНИЮ

1. **Начинай с Раздела 1** — зафиксируй стиль, получи подтверждение от ChatGPT
2. **Один промпт = одна задача** — не объединяй несколько в один запрос
3. **Для итераций** добавляй: `"Keep the same style as before but change: [что изменить]"`
4. **Для вариантов** добавляй: `"Generate 3 different style variations of this"`
5. **Для других стран** (Греция/Китай/Майя/Перу) — берёшь промпты из Разделов 2-4 и добавляешь в конец: `"Apply the same structure but for [country] chapter with [cultural theme] instead of Egypt"`
6. **Размер** — всегда указывай `1024x1024` для иконок, `1024x1792` для портретных экранов

---

## 🖼️ ПРИОРИТЕТ ГЕНЕРАЦИИ (с чего начать)

| Приоритет | Задача | Раздел |
|-----------|--------|--------|
| 1 | Зафиксировать стиль | Раздел 1 |
| 2 | Концепт персонажа | Раздел 7.1 |
| 3 | Фишки Египет | Раздел 2.1 |
| 4 | Суперфишки | Раздел 2.3 |
| 5 | Полный скриншот геймплея | Раздел 4.1 |
| 6 | Карта уровней | Раздел 5.1 |
| 7 | Попапы | Раздел 6.1–6.3 |
| 8 | Key Art | Раздел 8.1 |
| 9 | Препятствия | Раздел 3.1 |
| 10 | Фишки других стран | Раздел 2.2 + адаптации |
