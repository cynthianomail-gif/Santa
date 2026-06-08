using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 管理一局所有骰子的 DiceView：玩家 5 標準骰 + 1 特殊骰、AI 5 標準骰。
    /// 負責建構、播放擲骰動畫、處理鎖定點擊與 AI 揭露。
    /// </summary>
    public sealed class DiceRollerController : MonoBehaviour
    {
        public DiceSkin Skin = new DiceSkin();
        public RectTransform PlayerRow;
        public RectTransform AIRow;
        public RectTransform SpecialSlot;

        public float DiceSize = 120f;

        private DiceView[] _player;
        private DiceView[] _ai;
        private Image _specialBg;
        private TextMeshProUGUI _specialLabel;
        private int _maxFace = 6;
        private Action<int> _onPlayerDieClicked;

        private const int StandardCount = 5;

        /// <summary>建構（或重建）本局骰子。onPlayerDieClicked 傳回被點擊的玩家骰索引。</summary>
        public void BuildDice(int maxFace, Action<int> onPlayerDieClicked)
        {
            _maxFace = maxFace;
            _onPlayerDieClicked = onPlayerDieClicked;

            if (_player == null)
            {
                _player = new DiceView[StandardCount];
                for (int i = 0; i < StandardCount; i++)
                {
                    _player[i] = CreatePlayerDie(i);
                }
                BuildSpecialSlot();
            }

            if (_ai == null)
            {
                _ai = new DiceView[StandardCount];
                for (int i = 0; i < StandardCount; i++)
                {
                    _ai[i] = CreateDie(AIRow, "AIDie" + i);
                }
            }

            for (int i = 0; i < StandardCount; i++)
            {
                _player[i].Build(Skin, maxFace, DiceSize);
                _player[i].SetLocked(false);
                _ai[i].Build(Skin, maxFace, DiceSize);
            }
        }

        private DiceView CreatePlayerDie(int index)
        {
            DiceView view = CreateDie(PlayerRow, "PlayerDie" + index);
            view.Build(Skin, _maxFace, DiceSize);

            Button btn = view.gameObject.AddComponent<Button>();
            btn.targetGraphic = view.FrameImage;
            int captured = index;
            btn.onClick.AddListener(() =>
            {
                if (_onPlayerDieClicked != null) _onPlayerDieClicked(captured);
            });
            return view;
        }

        private DiceView CreateDie(RectTransform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(DiceSize, DiceSize);
            return go.AddComponent<DiceView>();
        }

        private void BuildSpecialSlot()
        {
            _specialBg = UiFactory.Image("SpecialDie", SpecialSlot,
                PlaceholderArt.RoundedFrame(new Color(0.25f, 0.05f, 0.3f),
                    new Color(0.05f, 0.02f, 0.05f)),
                Color.white);
            _specialBg.preserveAspect = true; // 維持 1:1，特殊骰圖不被壓縮
            RectTransform rt = (RectTransform)_specialBg.transform;
            rt.sizeDelta = new Vector2(DiceSize, DiceSize);

            _specialLabel = UiFactory.Text("SpecialLabel", _specialBg.transform, "",
                DiceSize * 0.32f, new Color(0.95f, 0.9f, 0.8f));
            UiFactory.Stretch((RectTransform)_specialLabel.transform);
        }

        // ---------------- 動畫 ----------------

        /// <summary>玩家全擲 + AI 全擲（AI 預設隱藏點數）。</summary>
        public IEnumerator AnimateInitialRoll(int[] playerValues, bool[] playerAbyss,
            int[] aiValues, bool aiHidden, string specialLabel)
        {
            for (int i = 0; i < StandardCount; i++)
            {
                _player[i].PlayRoll(playerValues[i], playerAbyss[i]);
            }
            // AI（撒旦）：隱藏時整段動畫維持 ?，不顯示真實點數；揭露模式才轉出點數。
            for (int i = 0; i < StandardCount; i++)
            {
                if (aiHidden) _ai[i].PlayRollHidden();
                else _ai[i].PlayRoll(aiValues[i], false);
            }
            SetSpecialLabel(specialLabel);

            yield return WaitForAnimations();
        }

        /// <summary>只重擲 rerolled[i] 為真（玩家選取重轉）的那幾顆骰子，其餘維持不動。</summary>
        public IEnumerator AnimatePlayerReroll(int[] playerValues, bool[] playerAbyss, bool[] rerolled)
        {
            for (int i = 0; i < StandardCount; i++)
            {
                if (rerolled != null && rerolled[i])
                {
                    _player[i].PlayRoll(playerValues[i], playerAbyss[i]);
                }
            }
            yield return WaitForAnimations();
        }

        /// <summary>揭露 AI 點數（Scry 或結算時）。</summary>
        public void RevealAI(int[] aiValues)
        {
            for (int i = 0; i < StandardCount; i++)
            {
                _ai[i].ShowImmediate(aiValues[i]);
            }
        }

        public void SetLockVisual(int index, bool locked)
        {
            if (_player != null && index >= 0 && index < _player.Length)
            {
                _player[index].SetLocked(locked);
            }
        }

        /// <summary>設定某顆玩家骰的「選取重轉」白框。</summary>
        public void SetRerollSelectedVisual(int index, bool selected)
        {
            if (_player != null && index >= 0 && index < _player.Length)
            {
                _player[index].SetRerollSelected(selected);
            }
        }

        /// <summary>清掉所有玩家骰的「選取重轉」白框。</summary>
        public void ClearRerollSelections()
        {
            if (_player == null) return;
            for (int i = 0; i < StandardCount; i++) _player[i].SetRerollSelected(false);
        }

        /// <summary>
        /// 依目前玩家點數計算重擲預報：對「重擲後有機會組成大牌的那一顆」開啟金色脈動。
        /// </summary>
        public void ApplyRerollForecast(int[] playerValues)
        {
            if (_player == null) return;
            RerollForecast fc = RerollForecaster.Evaluate(playerValues, _maxFace);
            for (int i = 0; i < StandardCount; i++)
            {
                // KeepMask 為 true=建議保留；要重轉的那顆是 !KeepMask，標記它。
                bool on = fc.HasForecast && fc.KeepMask != null && !fc.KeepMask[i];
                _player[i].SetForecast(on);
            }
        }

        /// <summary>關閉所有玩家骰子的重擲預報高亮。</summary>
        public void ClearRerollForecast()
        {
            if (_player == null) return;
            for (int i = 0; i < StandardCount; i++) _player[i].SetForecast(false);
        }

        // ---------------- 深淵效果觸發提示：在「實際發生變化」的骰子上做光爆閃 ----------------

        private static readonly Color WildFlashColor   = new Color(0.78f, 0.34f, 0.97f); // 紫：萬用骰
        private static readonly Color RevealFlashColor = new Color(0.35f, 0.82f, 0.95f); // 青：揭示
        private static readonly Color CrushFlashColor  = new Color(0.95f, 0.30f, 0.24f); // 紅：壓制

        /// <summary>對標記為 Wild 的玩家骰播放紫色觸發光爆，標示「這顆變成萬用骰」。</summary>
        public void FlashWildOnPlayerDice(bool[] mask)
        {
            if (_player == null || mask == null) return;
            for (int i = 0; i < StandardCount && i < mask.Length; i++)
                if (mask[i]) _player[i].PlayTriggerFlash(WildFlashColor);
        }

        /// <summary>對所有撒旦骰依序播放青色觸發光爆，標示「這些被揭示了」。</summary>
        public void FlashRevealOnAIDice()
        {
            if (_ai == null) return;
            for (int i = 0; i < StandardCount; i++) _ai[i].PlayTriggerFlash(RevealFlashColor);
        }

        /// <summary>對指定索引的撒旦骰播放紅色觸發光爆，標示「這顆被壓制歸一」。</summary>
        public void FlashCrushOnAIDie(int index)
        {
            if (_ai == null || index < 0 || index >= _ai.Length) return;
            _ai[index].PlayTriggerFlash(CrushFlashColor);
        }

        /// <summary>
        /// CRUSH 戲劇性單顆揭露：在其餘撒旦骰仍為「？」的狀態下，
        /// 直接揭露「這一顆」的最終點數並播放紅色觸發光爆，讓玩家清楚看到「就是這顆被壓制了」。
        /// </summary>
        public void RevealAndFlashCrush(int index, int value)
        {
            if (_ai == null || index < 0 || index >= _ai.Length) return;
            _ai[index].ShowImmediate(value);
            _ai[index].PlayTriggerFlash(CrushFlashColor);
        }

        /// <summary>依遮罩開關玩家骰的「萬用骰」標記徽章（D6 顯示圖示／D12-D20 顯示 "W"）。</summary>
        public void SetWildMarkers(bool[] mask)
        {
            if (_player == null) return;
            for (int i = 0; i < StandardCount; i++)
                _player[i].SetWildMarker(mask != null && i < mask.Length && mask[i]);
        }

        /// <summary>清除所有玩家骰的「萬用骰」標記徽章（新局開始時重置）。</summary>
        public void ClearWildMarkers()
        {
            if (_player == null) return;
            for (int i = 0; i < StandardCount; i++) _player[i].SetWildMarker(false);
        }

        public void SetSpecialLabel(string label)
        {
            if (_specialLabel != null) _specialLabel.text = label;
        }

        /// <summary>依特殊骰種類切換槽位圖；General（None）時隱藏整個特殊骰槽。</summary>
        public void SetSpecialVisual(SpecialDiceKind kind, string label)
        {
            bool hasSpecial = kind != SpecialDiceKind.None;
            if (_specialBg != null) _specialBg.gameObject.SetActive(hasSpecial);
            if (!hasSpecial)
            {
                SetSpecialLabel("");
                return;
            }
            Sprite s = SpecialSpriteFor(kind);
            if (_specialBg != null && s != null)
            {
                _specialBg.sprite = s;
                _specialBg.color = Color.white;
            }
            SetSpecialLabel(label);
        }

        private Sprite SpecialSpriteFor(SpecialDiceKind kind)
        {
            if (Skin == null) return null;
            switch (kind)
            {
                case SpecialDiceKind.DoubleEdge: return Skin.SpecialDouble;
                case SpecialDiceKind.Cursed:     return Skin.SpecialCursed;
                default:                         return null;
            }
        }

        /// <summary>結算時標記雙方牌組：未參與牌型的骰子壓灰（參與的維持正常，不加白框）。</summary>
        public void MarkHandDice(bool[] playerMask, bool[] aiMask)
        {
            for (int i = 0; i < StandardCount; i++)
            {
                bool p = playerMask != null && i < playerMask.Length && playerMask[i];
                _player[i].SetDim(!p);

                bool a = aiMask != null && i < aiMask.Length && aiMask[i];
                _ai[i].SetDim(!a);
            }
        }

        /// <summary>清除牌組壓灰，還原正常顯示。</summary>
        public void ClearHandMarks()
        {
            for (int i = 0; i < StandardCount; i++)
            {
                _player[i].SetDim(false);
                _ai[i].SetDim(false);
            }
        }

        /// <summary>所有骰子顯示為未揭露（初始畫面用）。</summary>
        public void ShowAllHidden()
        {
            if (_player != null)
                foreach (var d in _player) d.ShowHidden();
            if (_ai != null)
                foreach (var d in _ai) d.ShowHidden();
        }

        private IEnumerator WaitForAnimations()
        {
            bool any = true;
            while (any)
            {
                any = false;
                for (int i = 0; i < StandardCount; i++)
                {
                    if (_player[i].IsAnimating || _ai[i].IsAnimating) { any = true; break; }
                }
                yield return null;
            }
        }
    }
}
