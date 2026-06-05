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

```
[風格錨點]
a dim abyssal gambling table viewed from player's seat, dark wood and worn
green-black felt, scattered dried blood stains and wax drips, faint occult
sigils etched into the table, heavy vignette darkening the edges, empty center
space left clear for dice and UI, atmospheric fog, candlelit gloom
```
重點：**中央留空**（骰子與 UI 會疊上去），暗角壓邊聚焦。

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

## 匯入後對應位置（已就位）

5 張圖已放在 `Assets/Art/Resources/`，並附 Sprite 匯入設定（`.meta`），
`SceneBootstrap` 會於 Play 時依檔名自動載入，無需手動指派：

| 圖 | 路徑 | 自動載入到 |
|---|---|---|
| background.png | `Assets/Art/Resources/` | `SceneBootstrap.BackgroundSprite` |
| dealer_hand.png | `Assets/Art/Resources/` | `SceneBootstrap.DealerHandSprite` |
| dice_frame.png | `Assets/Art/Resources/` | `DiceSkin.Frame` |
| dice_locked.png | `Assets/Art/Resources/` | `DiceSkin.Locked` |
| dice_abyss.png | `Assets/Art/Resources/` | `DiceSkin.Abyss` |

> 已隨檔附上的 `.meta` 設定 Texture Type = Sprite、Alpha Is Transparency = 1。
> 要換圖直接覆蓋同名檔即可；要改設定可在 Inspector 覆寫對應欄位。
