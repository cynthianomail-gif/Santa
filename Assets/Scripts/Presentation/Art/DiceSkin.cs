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
        public Sprite Frame;
        public Sprite Locked;
        public Sprite Abyss;

        public Color FrameTint = new Color(0.86f, 0.82f, 0.72f); // 羊皮紙色
        public Color NumberColor = new Color(0.12f, 0.05f, 0.05f);
        public Color AbyssGlow = new Color(0.75f, 0.05f, 0.10f);
    }
}
