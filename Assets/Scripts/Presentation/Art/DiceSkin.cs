using System;
using UnityEngine;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 骰子外觀貼圖組。依「最少圖量」設計，只需 3 張 AI 生成圖：
    /// frame（外框）、locked（鎖定疊加）、abyss（深淵觸發光效）。
    /// 點數由 TextMeshPro 繪製，不需逐面貼圖。
    /// 留空時 DiceView 會以程式產生的佔位圖替代，方便美術到位前先跑流程。
    /// </summary>
    [Serializable]
    public sealed class DiceSkin
    {
        public Sprite Frame;    // D6 通用框（無 face 時退回用）
        public Sprite FrameD12; // D12 專屬框（菱形/多邊形）
        public Sprite FrameD20; // D20 專屬框（三角形）
        public Sprite Locked;
        public Sprite Abyss;
        /// <summary>萬用骰（Wild 效果）標記徽章；D6 用此圖示，D12/D20 沒有臉圖則退回顯示 "W" 文字。</summary>
        public Sprite WildIcon;
        /// <summary>
        /// D6 骰面貼圖，index 0 = 1點 … index 5 = 6點。
        /// 數量需 >= 6 且 maxFace == 6 才啟用；否則退回對應框圖 + 數字文字。
        /// </summary>
        public Sprite[] Faces = new Sprite[6];

        // ---- 特殊骰（第 6 顆，僅 Chaos）token 圖；留空則用紫色佔位框 ----
        public Sprite SpecialDouble; // 雙刃 DoubleEdge
        public Sprite SpecialCursed; // 詛咒 Cursed

        public Color FrameTint = new Color(0.86f, 0.82f, 0.72f);
        public Color NumberColor = new Color(0.12f, 0.05f, 0.05f);
        public Color AbyssGlow = new Color(0.75f, 0.05f, 0.10f);
    }
}
