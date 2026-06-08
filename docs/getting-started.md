# Abyss Protocol — 新手完整操作手冊（零 Unity 基礎）

這份手冊假設你**完全沒用過 Unity**。每一步、每個按鈕、會看到什麼畫面都寫清楚。
照順序做即可。遇到紅字錯誤就把文字貼給協作者修。

---

## 目錄
0. 安裝 Unity
1. 把專案加進 Unity Hub
2. 認識 Unity 介面
3. 匯入 TextMeshPro
4. 建立場景並掛啟動腳本
5. 按 Play 試玩
6. 跑自動測試
7. 存檔
8. （之後）出 WebGL 網頁版
9. 常見狀況排解
10. 名詞對照表

---

## 第 0 步：安裝 Unity（只做一次，約 30–60 分鐘）

Unity 分兩塊：**Unity Hub**（啟動器／管理器）＋ **Unity Editor**（真正的引擎）。

1. 瀏覽器開 **https://unity.com/download**。
2. 下載 **Unity Hub**，執行安裝檔，一直按 Next 到完成。
3. 打開 Unity Hub，會要求**登入**：
   - 有 Unity 帳號就登入；沒有就按 **Create account** 免費註冊，收信驗證後回來登入。
4. 登入後若要求選授權（License）：選 **Agree / Get a free personal license**（個人版，免費）。
5. 安裝 Editor：
   - Hub 左側點 **Installs（安裝）**。
   - 右上 **Install Editor**。
   - 在 **Official releases** 找 **2022.3.62f1**（找不到完全一樣的，挑任何 `2022.3.xx` LTS 都可以）。
   - 按它旁邊的 **Install**。
6. 跳出「Add modules（附加模組）」勾選清單時，**務必勾這幾項**：
   - ✅ **WebGL Build Support**（最終要出網頁版，現在勾省得之後重裝）
   - ✅ **Microsoft Visual Studio Community** 或 **Visual Studio Code Editor**（看程式用，二擇一）
   - 按 **Continue / Install**。
7. 開始下載（好幾 GB，依網速 20–40 分鐘）。**讓它跑完**，期間可以做別的事。

> 💡 如果之後想出 Windows 單機版，模組裡再加 **Windows Build Support** 即可。

---

## 第 1 步：把專案加進 Unity Hub（約 5 分鐘＋首次開啟等待）

專案已經在你電腦：`C:\Users\cynthia.chu\工作\Dice`，**不需要重新下載**。

1. Unity Hub 左側點 **Projects（專案）**。
2. 右上 **Add（新增）** 旁的小箭頭 ▾ → **Add project from disk**。
3. 選資料夾 `C:\Users\cynthia.chu\工作\Dice` → 按 **Add Project / 選擇資料夾**。
4. 清單出現一個專案（名稱可能顯示 **Dice**）。
5. 若該列顯示「找不到 Editor 版本」的黃色驚嘆號：
   - 點該列的版本欄，**改選你剛裝好的 2022.3** → 確定。
6. **點兩下專案**開啟。
7. ⏳ **第一次開會很久（3–10 分鐘）**，因為 Unity 要：
   - 下載 manifest.json 列的套件（TextMeshPro、UI、測試框架）
   - 生成 `Library/`、`ProjectSettings/` 其餘設定
   - 編譯所有 C# 腳本

   畫面可能卡在進度條或一片灰。**這是正常的，不要關掉**。

---

## 第 2 步：認識 Unity 介面（開好後先看一眼）

開好後畫面分成幾塊（位置可能略有不同）：

```
┌──────────────────────────────────────────────┐
│   ▶ ⏸ ⏭   ← 正上方中間：播放控制鍵             │
├───────────┬───────────────────────┬──────────┤
│ Hierarchy │     Scene / Game       │Inspector │
│ (物件清單) │   (編輯畫面/遊戲畫面)   │(屬性面板) │
│           │                       │          │
├───────────┴───────────────────────┴──────────┤
│ Project (檔案總管)        Console (訊息/錯誤)   │
└──────────────────────────────────────────────┘
```

- **Hierarchy**：目前場景裡有哪些物件。
- **Scene**：編輯用的 3D/2D 編輯視角；**Game**：實際遊戲畫面（按 Play 才有內容）。
- **Inspector**：點選某物件後，這裡顯示它的所有屬性與元件。
- **Project**：專案裡所有檔案（你的腳本、圖片都在這）。
- **Console**：訊息與**錯誤（紅字）**。找不到的話：上方選單 **Window → General → Console**。

---

## 第 3 步：匯入 TextMeshPro（文字才會顯示）

本遊戲的文字（按鈕、骰子點數）用 TextMeshPro 繪製，需要匯入一次資源。

- 開專案後**通常會自動跳出**一個視窗 **TMP Importer**，內有按鈕 **Import TMP Essentials**：
  - 按 **Import TMP Essentials** → 等跑完 → 關閉視窗。
- 如果沒自動跳出，手動做：
  - 上方選單 **Window → TextMeshPro → Import TMP Essential Resources** → 等完成。

> 漏做這步的症狀：遊戲畫面有方塊和骰子，但**沒有任何文字**。補做即可。

---

## 第 4 步：建立場景並掛上啟動腳本（最關鍵的一步）

我們的整個畫面是「**按 Play 由程式自動生出來**」的，所以只需要一個空物件掛上 `SceneBootstrap`。

### 4-1 建立新場景
1. 上方選單 **File → New Scene**。
2. 選 **Basic (Built-in)**（或 **Empty**）→ 按 **Create**。

### 4-2 存檔
1. 按 **Ctrl + S**。
2. 檔名打 `Main`。
3. 它預設存到 `Assets/`（或 `Assets/Scenes/`），按 **存檔 / Save**。

### 4-3 建立空物件
1. 在左側 **Hierarchy** 的空白處**按右鍵**。
2. 選 **Create Empty**。
3. 會出現一個叫 **GameObject** 的東西（名字無所謂）。

### 4-4 掛上 SceneBootstrap
1. **點一下**剛建立的 GameObject 選中它（右側 Inspector 會顯示它）。
2. Inspector 最下方按 **Add Component**。
3. 搜尋框輸入 `Scene Bootstrap`。
4. 點清單出現的 **Scene Bootstrap** → 它就被加到物件上。

> 你會看到 Inspector 裡 Scene Bootstrap 有幾個欄位（Background Sprite、Dealer Hand Sprite、Dice Skin…）。
> **全部留空就好**——程式會自動從 `Assets/Art/Resources/` 載入你的 AI 美術。

### 4-5 再存一次
按 **Ctrl + S**。

> 📌 場景在編輯狀態下看起來**只有一個空物件、畫面空空**，完全正常。內容是執行時生成的。

---

## 第 5 步：按 Play 試玩 🎮

1. 點畫面**正上方中間的三角形 ▶（Play 鍵）**。
2. 介面會自動切到 **Game** 分頁，出現遊戲：
   - 標題「深淵協議」、撒旦荷官之手、賭桌背景
   - **一般模式 / 混沌模式** 按鈕
   - **D6 / D12 / D20** 骰子難度
   - **100 / 200 / 300 / 500 / 800 / 1000** 押注
   - 底部 **與撒旦對賭** 開始按鈕
3. 試一輪流程：
   - 選好模式/骰子/押注 → 按 **與撒旦對賭**
   - 看骰子**滾動偽 3D 動畫**停在點數
   - 想保留的骰子**點它一下會鎖定**，按 **重擲** 重擲沒鎖的；或按 **確定** 直接結算
   - 看結算的牌型與本局獲得金額 → 按 **繼續** 回到選擇畫面
4. 玩完**再按一次 ▶** 停止（離開 Play 模式）。

### 判讀結果
- ✅ **能跑、骰子會動、有文字** → 核心與表現層都成功。
- ⚠️ **版面有點歪、重疊** → 正常，這是骨架階段，先確認流程能跑就好。
- ❌ **Game 畫面空白 / 一片黑 / 報紅字**：
  - 看下方 **Console**（Window → General → Console）。
  - 把**紅色錯誤的完整文字**複製或截圖貼給協作者修。
  - 表現層腳本是在沒有 Unity 的環境手寫的，可能有需要修的小錯，這很正常。

---

## 第 6 步：跑自動測試（確認規則數學正確）

這會驗證骰子機率、牌型判定、賠率公式、FG 流程等。

1. 上方選單 **Window → General → Test Runner**。
2. 跳出的視窗點 **EditMode** 分頁。
3. 按 **Run All**。
4. 等幾秒，左側清單**全部出現綠色勾勾 ✓** 就代表邏輯全對（20+ 項）。
   - 若有紅色叉，點該項看下方訊息，貼給協作者。

---

## 第 7 步：存檔

- **Ctrl + S** 存場景。
- 之後每次改動都記得存。

---

## 第 8 步（之後才做）：出 WebGL 網頁版

等你玩順、美術調好再做。

1. 上方選單 **File → Build Settings（建置設定）**。
2. 左下 **Add Open Scenes**（把目前的 Main 場景加進建置清單，會出現在上方列表並打勾）。
3. 左側平台清單選 **WebGL** → 按 **Switch Platform**。
   - 第一次切 WebGL 會**重新匯入所有資源，很久**（10–30 分鐘），正常。
4. 按 **Build**（建置）→ 選一個**空資料夾**當輸出 → 等它編譯。
5. 完成後該資料夾會有網頁檔；要實際在瀏覽器測試，用 **Build And Run** 它會自動開本機伺服器預覽。

> WebGL 不能直接用 `file://` 打開 index.html，必須透過伺服器（Build And Run 會自動處理）。

---

## 第 9 步：常見狀況排解

| 症狀 | 原因 / 解法 |
|---|---|
| 按鈕、數字沒文字 | 沒匯入 TMP → 第 3 步 |
| Game 畫面全黑或空白 | 確認空物件有掛 **Scene Bootstrap**，且已按 ▶ |
| Console 一堆紅字 | 複製完整錯誤文字貼給協作者 |
| 骰子有方塊但沒圖案 | 美術沒載到：確認 `Assets/Art/Resources/` 裡有 5 張 png；或圖被當成普通 Texture，點該圖在 Inspector 把 **Texture Type** 設成 **Sprite (2D and UI)** 按 Apply |
| 版面歪斜/重疊 | 骨架階段正常，先求流程跑通 |
| 開啟時說版本不符 | 用你已安裝的 2022.3 開啟即可；或改 `ProjectSettings/ProjectVersion.txt` 第一行 |
| 首次開啟卡很久 | 正常，在下載套件＋編譯，耐心等 |
| 改了腳本沒生效 | Unity 會自動重新編譯，等右下角轉圈跑完再按 Play |

---

## 第 10 步：名詞對照表

| 英文 | 中文 / 是什麼 |
|---|---|
| Hierarchy | 場景物件清單（左側） |
| Inspector | 屬性面板（右側） |
| Project | 檔案總管（下方） |
| Console | 訊息與錯誤視窗 |
| Scene 視窗 | 編輯用視角 |
| Game 視窗 | 實際遊戲畫面 |
| GameObject | 場景裡的一個物件 |
| Component（元件） | 掛在物件上的功能（例如 Scene Bootstrap） |
| Add Component | 幫物件加功能 |
| Play ▶ | 執行遊戲 |
| Build | 把遊戲打包成可執行檔/網頁 |
| Sprite | 2D 圖片素材 |
| TextMeshPro (TMP) | Unity 的文字繪製系統 |

---

## 你現在的進度與下一步

- ✅ 程式（核心邏輯 + 表現層）都已完成並上傳 GitHub
- ✅ 5 張 AI 美術已就位，會自動載入
- 🔜 **你要做的：第 0 → 6 步，把專案開起來、按 Play 跑通、跑測試**
- 🔜 之後：微調版面、調整美術、出 WebGL

**最可能卡關的地方**：第 0 步安裝（檔案大要等）、第 5 步若有編譯紅字。
卡住就把畫面或錯誤貼給協作者，一步步解。
