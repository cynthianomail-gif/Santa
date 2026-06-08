# Abyss Protocol — AI 美術生圖 Prompt 清單

只需 **5 張圖**（+ 1 套字型）即可完整呈現。為降低 AI 偏離風格，
所有圖共用同一段「風格錨點」前綴，並建議**同一模型、同一 seed、同一調色盤**生成。

---

## 0. 共用風格錨點（每張圖都貼這段在最前面）

```
2D hand-drawn macabre illustration, occult tarot / Inscryption-inspired,
aged parchment and dried-blood palette (#0E0D11 charcoal, #DBD2B8 parchment,
#8C0A10 blood red, #2A0A0C dark crimson), rough ink linework, grim dim lighting,
high contrast, painterly texture, no text, no numbers, no watermark
```

通用負面提示（negative prompt）：
```
bright colors, cartoon, anime, 3d render, photorealistic, text, letters, numbers,
watermark, signature, frame border (除非該圖需要), modern UI, glossy plastic
```

> 一致性訣竅：先生背景定調 → 鎖定 seed/調色盤 → 其餘圖沿用同 seed 與同一句風格錨點。

---

## 1. 背景 `background.png`

- **比例 / 尺寸**：16:9，**1920×1080**（或 2560×1440），不透明 PNG
- **用途**：Layer 0 賭桌底圖，畫面最底層
- **改版緣由**：舊版太暗（`candlelit gloom` + `heavy vignette`）。參考「地獄 21 點」
  （Inscryption 風暗黑塔羅賭桌）：**完全俯視角**、暗藍綠（petrol）邊緣漸層到中央暗血紅、
  做舊皮革氈面、手繪 grungy、整體暗但可讀、中央有暖光。維持與骰子一致的手繪血色羊皮紙風。
- **視角務必「正俯視」**：相機垂直朝下、平攤俯瞰，**沒有透視、沒有地平線、沒有側角**。

```
2D hand-painted macabre illustration, occult tarot / Inscryption-inspired, painterly
grungy texture, rough ink linework, high contrast, no text, no numbers, no watermark

a weathered occult blackjack card table seen from DIRECTLY OVERHEAD, strict top-down
bird's-eye flat-lay view, camera pointing straight down, no perspective and no horizon,
worn cracked leather and aged felt surface, a moody gradient from dark teal-green petrol
edges blending into a deep oxblood-red glowing center, faint occult sigils and a subtle
compass / ritual emblem etched in the middle, scattered dried blood stains and grime,
a soft warm amber glow pooling in the center, atmospheric and ominous but clearly
readable (NOT pitch black), gentle vignette only at the corners;
dice-game props painted directly onto the table around the EDGES and CORNERS ONLY:
a leather dice cup / shaker with a few spilled bone dice, a couple of stacks of
bone-and-obsidian gambling chips with blood-red sigils, and a small pile of scattered
bone dice, all seen from the same top-down angle with soft shadows;
keep the whole CENTER calm, empty and clutter-free for dice and UI
```
負面提示（背景專用，補在通用負面之後）：
```
perspective view, angled view, side view, 3d camera angle, horizon, vanishing point,
gold baroque, bright neon, pitch black, overly dark, low visibility, photorealistic 3d,
props in the center, clutter in the center, text, numbers
```
重點：**完全俯視角**（無透視）、**藍綠邊緣→中央血紅**的漸層（呼應參考圖）、做舊皮革氈面、
手繪 grungy 墨線質感、**暗但可讀**。道具（骰盅／籌碼／散落骰子）**直接畫進四周角落**、
**中央務必留空**（骰子/UI 會疊上去）。建議調色盤：petrol 藍綠邊 + 暗血紅中心
#5A1410 / #8C0A10、羊皮紙 #DBD2B8、暗緋 #2A0A0C。

---

## 2. 撒旦荷官之手 `dealer_hand.png`

- **比例 / 尺寸**：1:1，**1024×1024**，**透明去背 PNG**
- **用途**：Layer 1，固定畫面上方中央，會做上下浮動 Tween

```
[風格錨點]
a single sinister croupier's hand of Satan, long clawed fingers, ashen grey
skin with faint red veins, tattered dark sleeve cuff, reaching downward as if
about to deal, dramatic underlighting, isolated on transparent background,
centered, no table, no background elements
```
重點：**透明背景、單一隻手、不要桌面/背景**，手腕朝上方便固定於頂部。

---

## 3. 骰子外框 `dice_frame.png`  ★最重要、要最穩定

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：Layer 2 骰子本體。**點數由程式以 TextMeshPro 疊字**，所以這張**不可有任何數字或點數**

```
[風格錨點]
a single blank occult die face, rounded square token, hand-inked border with
small skull/sigil corner ornaments, aged bone-and-parchment surface, empty
flat center with NO pips and NO numbers, subtle worn texture, isolated on
transparent background, centered, symmetrical
```
重點：**正方留白中心**（數字會疊在中央）、四角可有小裝飾、**務必無點數無數字**、上下左右對稱（旋轉動畫才好看）。

---

## 4. 鎖定疊加 `dice_locked.png`

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：玩家鎖定骰子時疊在右上角的小印記

```
[風格錨點]
a small occult lock sigil / rusted chain clasp emblem, blood-red and iron,
glowing faint crimson, isolated on transparent background, centered, icon style,
no die, no number
```
重點：當作**圖示疊層**，主體置中、四周透明；偏小、辨識度高即可。

---

## 5. 深淵觸發光效 `dice_abyss.png`

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：骰子落入深淵區間時疊在骰子上的詭異光暈

```
[風格錨點]
an eerie radial demonic glow / halo of crimson and black smoke, soft
edges fading to transparent, faint floating embers and occult runes in the
aura, isolated on transparent background, centered, NO die, NO number
```
重點：**放射狀、邊緣淡出透明**，純光效（疊在骰子上用 Additive/一般混合都可）。

---

## 6. 字型（點數 / 標題用，不需生圖）

下載免費哥德 / 手寫感字型，匯入後做成 **TextMeshPro Font Asset**：

| 用途 | 建議字型（Google Fonts，可商用） |
|---|---|
| 骰子點數、標題 | **UnifrakturMaguntia**、**Pirata One**、**MedievalSharp** |
| 一般 UI 內文 | **EB Garamond**、**Cormorant** |

> 中文若要哥德感較難，可標題用英文哥德字、內文用思源宋體等中文字型分開設定。

---

## 6. 底部工具列背景 `ui_bar_bg.png`

- **比例 / 尺寸**：**1920×120**，**不透明 PNG**（不需去背）
- **用途**：底部工具列的底圖，取代純色黑底

```
[風格錨點]
a wide horizontal decorative border panel for a dark tavern UI, aged dark wood
planks with faint carved occult sigils along the top edge, weathered iron nail
heads near corners, dried wax drip marks and faint bloodstains on the surface,
subtle parchment-toned worn center, heavy vignette on left and right edges,
solid opaque background, panoramic strip format 16:1 ratio
```

重點：**上緣有裝飾邊（和遊戲畫面的接縫）**、中央偏暗偏素（按鈕文字要能讀清楚）、不透明。

---

## 7. 按鈕邊框 `ui_button_frame.png`  ★ 需設定 9-Slice

- **比例 / 尺寸**：**256×64**，**透明去背 PNG**
- **用途**：底部列所有按鈕（模式/骰子/押注數值/重擲/確定）的邊框，以 9-Slice 縮放

```
[風格錨點]
a single wide rectangular button frame / border for a dark UI, hand-inked rough
sketch border lines, small skull or sigil ornaments at four corners only, aged
parchment texture fill in the center area, thin blood-red inner ruling line,
isolated on transparent background, centered, symmetrical, empty center space
for text, wide landscape orientation 4:1 ratio
```

重點：**四角有裝飾、中央完全空白**（文字會疊上去）、左右對稱、9-Slice 邊界設為 24px 四邊。

---

## 8. Spin 按鈕圓形美術 `ui_spin_button.png`

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：底部列的 Spin（▶）按鈕主體圖，取代純色圓形

```
[風格錨點]
a circular occult summoning seal, two concentric rings with runic inscriptions
and tiny sigils between them, small pentagram or eye motif in the very center,
blood-red and tarnished gold ink on dark stone, faint crimson inner glow,
isolated on transparent background, perfectly circular, centered
```

重點：**正圓、邊緣去背**、中央留一個可放 ▶ 文字的空間、整體偏暗不要搶眼。

---

## 9. 骰面貼圖（新增）`face_1.png` ~ `face_6.png`

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：替換 dice_frame 顯示於骰子主體，翻轉動畫中快速切換不同面，落地時定格在正確點數
- **重要**：這 6 張要跟 `dice_frame.png` 同款式，等於「有點數版本的 dice_frame」

### 共用底層（6 張都貼在最前面）

```
2D hand-drawn macabre illustration, occult tarot / Inscryption-inspired,
aged parchment and dried-blood palette (#0E0D11 charcoal, #DBD2B8 parchment,
#8C0A10 blood red, #2A0A0C dark crimson), rough ink linework, grim dim lighting,
high contrast, painterly texture, no text, no numbers, no watermark
```

負面提示：
```
bright colors, cartoon, anime, 3d render, photorealistic, text, letters, numbers,
watermark, signature, modern UI, glossy plastic
```

### 各面個別 Prompt

**`face_1.png`（1 點）**
```
[風格錨點]
a single occult die face showing ONE pip, rounded square token, hand-inked border
with small skull/sigil corner ornaments, aged bone-and-parchment surface, one
blood-red circular enamel pip centered in the middle, isolated on transparent
background, centered, symmetrical
```

**`face_2.png`（2 點）**
```
[風格錨點]
a single occult die face showing TWO pips, rounded square token, hand-inked border
with small skull/sigil corner ornaments, aged bone-and-parchment surface, two
blood-red circular enamel pips placed diagonally top-right and bottom-left,
isolated on transparent background, centered, symmetrical
```

**`face_3.png`（3 點）**
```
[風格錨點]
a single occult die face showing THREE pips, rounded square token, hand-inked border
with small skull/sigil corner ornaments, aged bone-and-parchment surface, three
blood-red circular enamel pips in a diagonal line from top-right to bottom-left,
isolated on transparent background, centered, symmetrical
```

**`face_4.png`（4 點）**
```
[風格錨點]
a single occult die face showing FOUR pips, rounded square token, hand-inked border
with small skull/sigil corner ornaments, aged bone-and-parchment surface, four
blood-red circular enamel pips one in each corner, isolated on transparent
background, centered, symmetrical
```

**`face_5.png`（5 點）**
```
[風格錨點]
a single occult die face showing FIVE pips, rounded square token, hand-inked border
with small skull/sigil corner ornaments, aged bone-and-parchment surface, five
blood-red circular enamel pips four in corners plus one centered in the middle,
isolated on transparent background, centered, symmetrical
```

**`face_6.png`（6 點）**
```
[風格錨點]
a single occult die face showing SIX pips, rounded square token, hand-inked border
with small skull/sigil corner ornaments, aged bone-and-parchment surface, six
blood-red circular enamel pips arranged in two vertical columns of three,
isolated on transparent background, centered, symmetrical
```

> 建議跟原本 5 張圖用同一個 seed，這樣骰面跟骰框/背景風格會最一致。

---

## 7. D12 骰框 `dice_frame_d12.png`

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：選 D12 時骰子的外框，中央留空疊數字

```
[風格錨點]
a single blank occult D12 die token, elongated diamond / rhombus shape with
pointed top and bottom, hand-inked border with small skull/sigil edge ornaments,
aged bone-and-parchment surface, empty flat center with NO pips and NO numbers,
subtle worn texture, isolated on transparent background, centered, symmetrical
```

重點：**菱形輪廓**（上下尖）、中央留空、四邊可有小裝飾，旋轉後仍對稱。

---

## 8. D20 骰框 `dice_frame_d20.png`

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：選 D20 時骰子的外框，中央留空疊數字

```
[風格錨點]
a single blank occult D20 die token, equilateral triangle shape with flat bottom
and pointed top, hand-inked border with small skull/sigil corner ornaments,
aged bone-and-parchment surface, empty flat center with NO pips and NO numbers,
subtle worn texture, isolated on transparent background, centered, symmetrical
```

重點：**等邊三角形輪廓**、中央留空、三角有手繪感的磨損邊緣。

---

## 11. 特殊骰圖示（第 6 顆特殊骰，僅 Chaos 模式，取代紫色佔位框）

> 註：FG（Free Game）系統已移除，原本的 `special_fg`（FG 觸發骰）不再需要。
> 特殊骰只剩 **雙刃 / 詛咒** 兩種，且只在 Chaos 模式出現；General 模式沒有特殊骰。

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：遊戲中右側「特殊骰槽」的骰子本體圖，取代目前的紫色圓角框。
  共 2 種：雙刃 / 詛咒。**程式仍會在中央疊一行小標籤**（如 `x2`、`x6`），
  所以**中央區域請偏空、留給文字**，識別用的圖騰放在邊框與四角。

### ★ 重要：去背 / 1:1 / 中央留空（踩過的雷）

- **必須真透明背景**：多數文生圖（Midjourney/DALL·E 等）即使寫 `transparent background` 仍輸出**白底**，
  直接用會在遊戲裡變成白色方塊。可靠做法二選一：
  1. 用支援透明 PNG 的工具（**Recraft transparent / Adobe Firefly / Ideogram / SD + rembg**）。
  2. 或先生在**純色背景**（純黑、純洋紅），再用 **remove.bg / Photoshop** 去背。
- **比例 1:1**：要在生圖工具的「比例設定」選 **1:1 正方**，靠 prompt 文字通常控不住，別生成寬圖。
- **中央務必留空**：遊戲會在骰子中央疊一行小標籤（`x2`、`x6`、`FG!`），
  所以**圖騰要靠邊框 / 四角 / 下緣**，中央保持平坦空白，**不要讓刀刃/圖案蓋住正中心**。

### 共用底層（3 張都貼在最前面）

```
2D hand-drawn macabre illustration, occult tarot / Inscryption-inspired,
aged parchment and dried-blood palette (#0E0D11 charcoal, #DBD2B8 parchment,
#8C0A10 blood red, #2A0A0C dark crimson), rough ink linework, grim dim lighting,
high contrast, painterly texture, no text, no numbers, no watermark
```

負面提示：
```
white background, solid background showing as a box, glossy metal, 3d render,
photorealistic chrome, wide aspect, emblem covering center, bright colors, cartoon,
anime, text, letters, numbers, watermark, signature, modern UI, busy cluttered center
```

### 各特殊骰個別 Prompt（中央留空版）

**`special_double.png`（雙刃 Double Edge — 高風險高報酬倍率骰）**
```
[風格錨點]
a single occult die token, flat front view, perfectly SQUARE 1:1 token centered and
filling the frame, the token surface split into a mirrored light-half / dark-half,
two slim double-edged daggers crossed as a small emblem in the LOWER portion only,
keeping the CENTER flat and empty for an overlaid label, hand-inked border with small
skull and pentagram corner ornaments, aged bone-and-parchment texture, isolated on a
plain solid flat background for easy cut-out, centered, symmetrical
```
重點：**雙刃對稱**、銀＋血紅、刀刃靠下、**中央留空**、1:1、純色好去背。

**`special_cursed.png`（詛咒 Cursed — x6 或 x0 的賭命骰）**
```
[風格錨點]
a single cursed occult die token, flat front view, perfectly SQUARE 1:1 token centered
and filling the frame, hand-inked border wrapped in thorny brambles with a grinning
skull at the TOP edge and small pentagram / sigil ornaments in the corners, dark violet
and blood-red corruption seeping inward from the EDGES only, faint sickly purple glow
around the rim, a small dripping curse sigil (a hexed eye or inverted-cross knot) at the
BOTTOM edge, NO daggers NO swords NO blades NO crossed weapons, aged bone surface, the
CENTER kept flat and empty for an overlaid label, isolated on a plain solid flat
background for easy cut-out, centered, symmetrical
```
重點：**骷髏（上）＋荊棘咒印＋暗紫腐蝕**靠邊、下緣放滴血咒印、**不要刀刃**、中央留空、1:1、純色好去背。

> 建議跟 `dice_frame.png` 用同一個 seed，兩顆特殊骰彼此也用同 seed，風格才會一致。
> 生完記得確認：**(1) 背景真透明 (2) 正方 1:1 (3) 中央沒被圖案蓋住**。

---

## 12. 萬用骰標記徽章 `dice_wild.png`

- **比例 / 尺寸**：1:1，**256×256**，**透明去背 PNG**
- **用途**：玩家骰觸發「WILD（萬用骰）」深淵效果時，疊在骰子**左上角**的小徽章圖示
  （與右上角的 `dice_locked.png` 鎖定印記左右對稱、互不重疊，疊層後會再縮放到約骰子尺寸的 1/3）
- **重要**：只有 **D6**（有臉圖貼圖）會用這張圖；**D12 / D20** 沒有臉圖體系，
  程式會自動退回顯示文字 **"W"**（紫色、粗體），不需要也不會用到這張圖在那兩種骰型上

```
[風格錨點]
a small occult sigil emblem representing chaos / shapeshifting / wildcard — a
swirling kaleidoscope rune, or a stitched joker-mask glyph with a single all-seeing
eye at its center, hand-inked in violet-purple and tarnished gold ink with faint
amethyst glow, isolated on transparent background, centered, icon style, no die,
no number, no frame border
```

重點：當作**圖示疊層**（與 `dice_locked.png` 同類型），主體置中、四周透明、偏小但
辨識度要高（會被縮到骰子的 1/3 大小）；色調以**紫＋金**為主，和既有的血紅／暗紫
深淵色盤互補又能一眼區分（紫＝萬用、紅＝壓制、青＝揭示、金＝重擲）。

---

## 匯入後對應位置（已就位）

5 張圖已放在 `Assets/Art/Resources/`，並附 Sprite 匯入設定（`.meta`），
`SceneBootstrap` 會於 Play 時依檔名自動載入，無需手動指派：

| 圖 | 路徑 | 自動載入到 |
|---|---|---|
| background.png | `Assets/Art/Resources/` | `SceneBootstrap.BackgroundSprite` |
| dealer_hand.png | `Assets/Art/Resources/` | `SceneBootstrap.DealerHandSprite` |
| ui_bar_bg.png | `Assets/Art/Resources/` | 底部工具列背景 |
| ui_button_frame.png | `Assets/Art/Resources/` | 按鈕邊框（需在 Sprite Editor 設 9-Slice 邊界 24px） |
| ui_spin_button.png | `Assets/Art/Resources/` | Spin 按鈕圓形圖 |
| dice_frame.png | `Assets/Art/Resources/` | `DiceSkin.Frame`（D6 無 face 時退回用） |
| dice_frame_d12.png | `Assets/Art/Resources/` | `DiceSkin.FrameD12` |
| dice_frame_d20.png | `Assets/Art/Resources/` | `DiceSkin.FrameD20` |
| dice_locked.png | `Assets/Art/Resources/` | `DiceSkin.Locked` |
| dice_abyss.png | `Assets/Art/Resources/` | `DiceSkin.Abyss` |
| dice_wild.png | `Assets/Art/Resources/` | `DiceSkin.WildIcon`（萬用骰徽章；D6 用此圖，D12/D20 退回顯示 "W"） |
| face_1.png ~ face_6.png | `Assets/Art/Resources/` | `DiceSkin.Faces[0~5]`（僅 D6 啟用） |
| special_double.png | `Assets/Art/Resources/` | `DiceSkin.SpecialDouble`（Chaos 雙刃，沒放→紫框） |
| special_cursed.png | `Assets/Art/Resources/` | `DiceSkin.SpecialCursed`（Chaos 詛咒，沒放→紫框） |

> 已隨檔附上的 `.meta` 設定 Texture Type = Sprite、Alpha Is Transparency = 1。
> 要換圖直接覆蓋同名檔即可；要改設定可在 Inspector 覆寫對應欄位。
> D6：face_1~6 放齊 → 自動切骰面圖；未放齊 → 退回 dice_frame + 數字。
> D12/D20：各自用 dice_frame_d12 / dice_frame_d20 + 數字；框圖沒放也能跑（退回 dice_frame）。
