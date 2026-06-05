# Abyss Protocol (深淵協議)

2D 黑暗風格撲克骰子博弈遊戲。Unity 2D → WebGL。

完整設計規格見 [docs/superpowers/specs/2026-06-05-abyss-protocol-design.md](docs/superpowers/specs/2026-06-05-abyss-protocol-design.md)。

## 架構

```
Assets/Scripts/
├── Core/          純 C# 邏輯層（零 UnityEngine 相依，noEngineReferences）
│   ├── Config/    GameConfig：列舉、倍率表、深淵觸發區間
│   ├── Util/      IRandom 抽象 + SystemRandom
│   ├── Dice/      Die / SpecialDie / WildResolver
│   ├── Evaluator/ PokerEvaluator
│   ├── Effects/   AbyssEffectHandler（Scry / Destroyer / Reroll）
│   ├── Economy/   PayoutCalculator
│   └── StateMachine/  GameStateMachine + 8 個狀態
└── Tests/         NUnit Edit Mode 測試（CoreLogicTests）
```

設計原則：資料流單向 `Core → Presentation`。Core 不知道 Unity 的存在，
表現層透過 `GameStateMachine` 的 C# event 接收狀態變化。

## 測試

### Unity 內（正式）
Window → General → Test Runner → EditMode → Run All。

### 命令列（無需開 Unity，快速驗證 Core 邏輯）
Core 層零 UnityEngine 相依，可用任何 C# 編譯器獨立編譯驗證：

```powershell
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /target:exe /out:"$env:TEMP\abyss_verify.exe" `
    /recurse:"Assets\Scripts\Core\*.cs" "Tooling\VerifyCore\Program.cs"
& "$env:TEMP\abyss_verify.exe"
```

目前 45 項驗證全數通過（牌型判定、Wild 最優解、賠率公式、深淵觸發、
特殊骰機率、完整 FSM 流程含 FG 連續 5 局）。

## 表現層（Presentation）

```
Assets/Scripts/Presentation/
├── SceneBootstrap.cs   程式建構整個場景（Canvas/面板/骰子區/系統元件）
├── GameManager.cs      Core FSM ↔ UI 橋接 + 動畫序列協程
├── Dice/
│   ├── DiceView.cs            單顆骰子偽 3D 動畫（旋轉+縮放，TMP 數字）
│   └── DiceRollerController.cs 管理 5+1 玩家骰 / 5 AI 骰
├── UI/
│   ├── UIManager.cs    面板切換 + Idle 選擇狀態
│   ├── ResultDisplay.cs 牌型 / 獲得金額
│   └── UiFactory.cs    程式建 uGUI 的輔助工廠
├── FX/FXController.cs  墨水閃現 + 荷官浮動 Tween
└── Art/
    ├── DiceSkin.cs       3 張骰子貼圖組（frame/locked/abyss）
    └── PlaceholderArt.cs 程式佔位圖（美術到位前可先跑）
```

## 如何跑起來（Unity）

1. 開新的空場景。
2. 建立一個空 GameObject，掛上 `SceneBootstrap`。
3. 按 Play —— 整個 Canvas/面板/骰子會由程式自動建構，可直接遊玩完整流程
   （Idle 選擇 → 擲骰偽 3D 動畫 → 鎖定/重擲 → 結算 → FG）。
4. **美術已就位**：5 張 AI 圖放在 `Assets/Art/Resources/`，`SceneBootstrap`
   會在 Play 時自動從 Resources 載入（background / dealer_hand / dice_frame /
   dice_locked / dice_abyss），無需手動拉 Inspector。若要替換，覆蓋同名檔即可，
   或在 Inspector 指派欄位覆寫。

> 註：表現層需要 **TextMeshPro**（首次會提示匯入 TMP Essentials）。
> 場景由程式建構，因此倉庫不含 `.unity` 檔；版面座標若需微調可在 Editor 內調整。

## 下一步（依設計文件第 11 節）

1. ✅ Core 層全部腳本 + 測試通過（45 項 headless 驗證）
2. ✅ 場景基礎（SceneBootstrap 程式建構 Canvas + Panel）
3. ✅ DiceView 偽 3D 擲骰動畫（程式驅動，3 張 AI 圖）
4. ✅ GameManager 與 UI 串接（MonoBehaviour 表現層）
5. ✅ FX 骨架（墨水閃現、Dealer 浮動）
6. ⬜ 匯入正式 AI 美術 + 哥德體 TMP 字型，調版面
7. ⬜ 在 Unity Test Runner 跑 EditMode 測試確認綠燈
8. ⬜ WebGL Build 測試與效能調優
```
