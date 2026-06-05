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
            for (int i = 0; i < StandardCount; i++)
            {
                _ai[i].PlayRoll(aiValues[i], false);
            }
            SetSpecialLabel(specialLabel);

            yield return WaitForAnimations();

            if (aiHidden)
            {
                for (int i = 0; i < StandardCount; i++) _ai[i].ShowHidden();
            }
        }

        /// <summary>只重擲未鎖定的玩家骰。</summary>
        public IEnumerator AnimatePlayerReroll(int[] playerValues, bool[] playerAbyss)
        {
            for (int i = 0; i < StandardCount; i++)
            {
                if (!_player[i].IsLocked)
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

        public void SetSpecialLabel(string label)
        {
            if (_specialLabel != null) _specialLabel.text = label;
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
