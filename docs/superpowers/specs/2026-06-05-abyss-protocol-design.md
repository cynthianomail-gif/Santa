# Abyss Protocol — 完整設計文件
**日期：** 2026-06-05
**平台：** Unity 2D → WebGL
**風格：** 2D 黑暗手繪風（類《Inscryption》）

---

## 1. 遊戲概述

2D 黑暗風格撲克骰子博弈遊戲。玩家對抗 AI 撒旦荷官，透過擲骰組成撲克牌型決定勝負，並以押注檔位乘上牌型倍率計算本局獲得金額。無持久資源系統，每局結束後顯示獲得金額，Session 內記錄最高單局紀錄。

---

## 2. 技術架構決策

| 項目 | 決策 | 理由 |
|---|---|---|
| 引擎 | Unity 2D | Shader/Timeline/粒子特效品質上限高 |
| 發布平台 | WebGL | 零安裝門檻，方便分享與測試 |
| 場景結構 | 單場景 + State Machine | FG 僅是規則切換，避免場景切換破壞節奏 |
| 程式架構 | Pure C# Core + Unity Presentation | Core 可獨立測試，邏輯與 Unity 解耦 |
| UI 框架 | uGUI (Canvas) | WebGL 最穩定，AI Sprite 直接拖用 |
| 存檔 | 不需要（Session 內記憶） | 無持久資源，Session 結束即重置 |

---

## 3. 經濟系統

### 3.1 押注檔位
```
BetTiers  : [100, 200, 300, 500, 800, 1000]
DefaultBet: 100
```
玩家在 Idle 狀態選擇，進場後鎖定。無靈魂/資源系統，僅顯示本局獲得金額。

### 3.2 遊戲難度（骰子面數）

| 模式 | 難度倍率 |
|---|---|
| D6  | ×1.0 |
| D12 | ×2.5 |
| D20 | ×5.0 |

玩家與 AI 該局都使用同一面數的標準骰。

### 3.3 牌型賠率表

| 牌型 | HandRank 值 | 賠率倍數 |
|---|---|---|
| Five-of-a-Kind（五條） | 8 | ×50 |
| Four-of-a-Kind（四條） | 7 | ×15 |
| Large Straight（大順）| 6 | ×12 |
| Full House（葫蘆）    | 5 | ×10 |
| Small Straight（小順）| 4 | ×5  |
| Three-of-a-Kind（三條）| 3 | ×3 |
| Two Pairs（兩對）     | 2 | ×2  |
| One Pair（一對）      | 1 | ×1  |
| High Card（散牌）     | 0 | ×0  |

### 3.4 最終獲得公式

```
Final Payout = BaseBet × HandMultiplier × DifficultyMultiplier × SpecialDieMultiplier
```

- 玩家輸（HandRank 低於 AI，或 High Card）→ Payout = 0
- Tie（相同牌型）→ AI 勝，Payout = 0
- HighCard → Payout = 0（無論對手）

---

## 4. 骰子系統

### 4.1 標準骰深淵觸發區間

| 骰型 | 深淵觸發值 | 觸發機率 |
|---|---|---|
| D6  | 6 | 1/6 |
| D12 | 11, 12 | 2/12 = 1/6 |
| D20 | 17, 18, 19, 20 | 4/20 = 1/5 |

觸發後從以下 4 種效果隨機選 1：

| 效果 | 執行時機 | 執行者 |
|---|---|---|
| Wild | Evaluation 前，WildResolver 自動最優解 | 系統 |
| Scry | Effect_Trigger_Phase 最先，揭露 AI 點數 | 系統（顯示給玩家） |
| Destroyer | Evaluation 前，AI 最大值骰強制變 1；若同局多顆 Destroyer 則每顆各自獨立執行一次 | 系統 |
| Reroll | Effect_Trigger_Phase，playerRerollLimit += 1；玩家可選擇不用完 | 系統 |

### 4.2 特殊骰（玩家專用）

**雙刃骰 DoubleEdge D6**
面值分佈：`[×2, ×0.5, ×1, ×2, ×0.5, ×1]`（各 2 面）
不參與牌型判定，結算時提供 SpecialDieMultiplier。

**詛咒骰 Cursed D6**
面值分佈：`[×6, ×0, ×0, ×0, ×0, ×0]`
觸發 ×6 機率 1/6，否則本局歸零。

**FG 觸發骰 FGTrigger D6**
1 面為 FG（機率 1/6），觸發後進入 Free Game 模式。
General Mode 專屬，不提供 SpecialDieMultiplier（視為 ×1）。

### 4.3 遊戲模式骰子配置

| 模式 | 玩家骰子 | AI 骰子 |
|---|---|---|
| General | 5 標準骰 + 1 FGTrigger | 5 標準骰 |
| Chaos   | 5 標準骰 + 1 DoubleEdge 或 Cursed | 5 標準骰 |

特殊骰選擇時機：下注階段（Idle），選定後該局鎖定。
FG 期間：FGTrigger 替換為 Chaos 型特殊骰（玩家二選一）。

### 4.4 牌型判定規則

- 僅計算 5 顆標準骰
- 特殊骰第 6 顆只提供倍率，不進入 PokerEvaluator
- Wild 解析後才送入 PokerEvaluator
- Tie（相同牌型）→ AI 勝

---

## 5. FG（Free Game）機制

**觸發條件：** General Mode 中 FGTrigger 骰擲出 FG 面。

**流程：**
1. 記錄觸發當下的 BaseBet → FGBaseBet（後續 5 局固定）
2. 進入 FG_TRANSITION：播放全螢幕墨水轉場動畫，同時呈現特殊骰選擇 UI（DoubleEdge / Cursed 二選一），玩家確認後鎖定
3. 進入 FG_Loop，重複標準回合流程共 5 次，FGTrigger 替換為玩家選擇的特殊骰
4. 每局獨立計算 Payout 並累計
5. 5 局結束後 Settlement 顯示總獲得金額 → Idle

**FG 期間：** 玩家不需重新選擇押注，使用 FGBaseBet 結算。

---

## 6. 遊戲狀態機

### 狀態流程圖

```
IDLE
 │ (選模式/骰型/押注/特殊骰)
 ▼
BASEGAME_ROLL
 │ AI 擲 5 骰（隱藏）
 │ 玩家擲 5+1 骰
 │ 若 FGTrigger 出 FG → pendingFG = true
 ▼
EFFECT_TRIGGER_PHASE
 │ 1. Scry → 揭露 AI 點數
 │ 2. Reroll → playerRerollLimit += 1
 ▼
PLAYER_REROLL_PHASE
 │ 玩家 Lock/Unlock 骰子，重擲未鎖骰子
 │ 次數：playerRerollLimit（預設 1）
 │ 用完或放棄 → 繼續
 ▼
EVALUATION
 │ 1. Wild → WildResolver 最優解
 │ 2. Destroyer → AI 最大骰變 1
 │ 3. PokerEvaluator 判定雙方牌型
 │ 4. 比較 HandRank（同階 AI 勝）
 │ 5. PayoutCalculator 計算獲得金額
 ├─ pendingFG = false ──→ SETTLEMENT → IDLE
 └─ pendingFG = true  ──→ FG_TRANSITION（玩家二選一特殊骰 UI）→ FG_LOOP(×5) → SETTLEMENT → IDLE
```

### GameContext 資料包

```csharp
class GameContext {
    DiceType        ActiveDiceType
    GameMode        Mode
    SpecialDiceKind SpecialDice       // 玩家選擇的特殊骰種類
    int             BaseBet
    Die[]           PlayerDice        // 5 顆標準骰
    SpecialDie      PlayerSpecialDie  // 第 6 顆
    Die[]           AIDice            // 5 顆
    int             PlayerRerollLimit // 預設 1
    bool            PendingFG
    int             FGRoundsRemaining // 倒數 5→0
    int             FGBaseBet
    int             SessionHighScore  // 本 Session 最高單局
    int             LastPayout
}
```

---

## 7. 專案資料夾結構

```
Assets/
├── Scripts/
│   ├── Core/                        ← 純 C#，禁止 using UnityEngine
│   │   ├── Config/
│   │   │   └── GameConfig.cs        (列舉、倍率表、常數)
│   │   ├── Dice/
│   │   │   ├── Die.cs               (骰子單體：Roll、深淵判定)
│   │   │   ├── SpecialDie.cs        (DoubleEdge / Cursed / FGTrigger)
│   │   │   └── WildResolver.cs      (Wild 最優解貪心算法)
│   │   ├── Evaluator/
│   │   │   └── PokerEvaluator.cs    (5 int → HandRank)
│   │   ├── Effects/
│   │   │   └── AbyssEffectHandler.cs (Scry / Destroyer / Reroll 執行)
│   │   ├── Economy/
│   │   │   └── PayoutCalculator.cs  (賠率公式)
│   │   └── StateMachine/
│   │       ├── IGameState.cs
│   │       ├── GameContext.cs
│   │       ├── GameStateMachine.cs
│   │       └── States/
│   │           ├── IdleState.cs
│   │           ├── BaseGameRollState.cs
│   │           ├── EffectTriggerState.cs
│   │           ├── PlayerRerollState.cs
│   │           ├── EvaluationState.cs
│   │           ├── FGTransitionState.cs
│   │           ├── FGLoopState.cs
│   │           └── SettlementState.cs
│   │
│   ├── Presentation/                ← MonoBehaviour
│   │   ├── DiceView.cs              (單顆骰子：動畫 + TMP 數字)
│   │   ├── DiceRollerController.cs  (統管 6 顆 DiceView)
│   │   ├── UIManager.cs             (Panel 顯示/隱藏)
│   │   ├── ResultDisplay.cs         (牌型 + 獲得金額顯示)
│   │   └── FXController.cs          (墨水特效、Dealer 動畫)
│   │
│   └── Tests/
│       └── CoreLogicTests.cs        (Unity Edit Mode Tests)
│
├── Art/
│   ├── Sprites/
│   │   ├── dice_frame.png           ← AI 生成 #1（骰子外框）
│   │   ├── dice_locked.png          ← AI 生成 #2（鎖定疊加）
│   │   ├── dice_abyss.png           ← AI 生成 #3（深淵光效）
│   │   ├── background.png           ← Layer 0 底圖
│   │   └── dealer_hand.png          ← Layer 1 莊家手
│   └── Fonts/
│       └── gothic_tmp.asset         (哥德體 TextMeshPro)
│
└── Prefabs/
    ├── DicePrefab.prefab
    └── UI/
        ├── Panel_Idle.prefab
        ├── Panel_Game.prefab
        └── Panel_FG.prefab
```

---

## 8. 場景 Hierarchy

```
[Scene: AbyssProtocol]
├── GameManager
│   └── GameStateMachine.cs
├── Canvas (Screen Space - Overlay)
│   ├── Panel_Idle              (模式/骰型/押注選擇)
│   ├── Panel_BetSelector       (6 個押注檔位按鈕)
│   ├── Panel_Game              (主遊戲畫面，常駐)
│   │   ├── DiceArea_Player
│   │   ├── DiceArea_AI
│   │   ├── Panel_AbyssEffect
│   │   └── Panel_Result
│   ├── Panel_FG
│   └── Panel_FX               (全螢幕特效疊層)
├── DiceSpawner
│   └── DiceRollerController.cs
├── Background (SpriteRenderer)
└── DealerHand (SpriteRenderer)
```

---

## 9. DiceView 動畫規格

**所需 AI 圖片：3 張**（dice_frame / dice_locked / dice_abyss）
**點數顯示：** TextMeshPro + 哥德體字型

**Roll 動畫時序（純 Code 驅動）：**

| 階段 | 時長 | 行為 |
|---|---|---|
| 播放 | 0.8s | RotationZ 快速隨機 ±180°；ScaleX = \|sin(time×15)\| 模擬翻轉 |
| 收尾 | 0.2s | Ease-out → RotationZ snap 0；ScaleX → 1 |
| 落地 | 0.15s | ScaleY: 1.0 → 0.85 → 1.0（squash & stretch） |
| 結果 | — | TMP 數字 FadeIn；若深淵觸發疊加 dice_abyss.png |

---

## 10. 測試計畫（Unity Edit Mode Tests）

```
PokerEvaluator
  [3,3,3,7,7] → FullHouse
  [1,2,3,4,5] → LargeStraight
  [1,2,3,4,6] → SmallStraight（4 連續存在）
  [5,5,5,5,5] → FiveOfAKind
  [1,2,4,6,6] → OnePair

WildResolver（貪心法：優先嘗試五條→四條→葫蘆→順子→三條，複數 Wild 以相同策略疊加）
  [3,3,3,7,Wild] D12 → 解為 FullHouse（Wild=7）或 FourOfAKind（Wild=3），取最高
  [Wild,Wild,7,7,7] D6 → 解為 FiveOfAKind（兩 Wild 都設 7）

PayoutCalculator
  bet=100, FullHouse, D12, mult=1.0 → 2500
  bet=500, HighCard, D6,  mult=1.0 → 0
  bet=100, FiveOfAKind, D20, mult=6.0 → 150000

AbyssEffect
  D6 觸發區間 = {6}
  D12 觸發區間 = {11, 12}
  Destroyer 將 AI 最大骰強制為 1

SpecialDie 機率
  Cursed ×6 出現率約 1/6（600 次取樣）
  FGTrigger FG 出現率約 1/6（600 次取樣）
```

---

## 11. 開發優先順序（建議）

1. Core 層全部腳本 + Edit Mode Tests 通過
2. Unity 場景基礎建立（Hierarchy + Panel 結構）
3. DiceView 動畫（單顆 Roll 動畫驗收）
4. GameStateMachine 與 UI 串接
5. FX（墨水特效、Dealer 動畫）
6. FG 機制完整流程
7. WebGL Build 測試與效能調優
