using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace WulaFallenEmpire
{
    // =========================================================================
    // 1. 抽象的引擎父类 (Abstract Base Class)
    //    它现在通过完全重写 DrawAt 方法来获得对绘制逻辑的控制权。
    // =========================================================================
    public abstract class Building_Wula_Engine_Base : Building_GravEngine
    {
        // 契约：所有子类都必须提供一个用于绘制的能量球贴图。
        // 这个部分保持不变。
        protected abstract CachedMaterial OrbMat { get; }

        // **关键修正**: 完全重写 DrawAt 方法
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // --- 第 1 部分：复制父类的 `base.DrawAt(drawLoc, flip)` 逻辑 ---
            // 这会负责绘制建筑本身的基础图形。
            base.DrawAt(drawLoc, flip);

            // --- 第 2 部分：复制并修改 `Building_GravEngine` 的绘制逻辑 ---
            if (base.Spawned)
            {
                // 这是原版引擎的“悬浮”动画逻辑，我们将其完整保留。
                if (Find.TickManager.TicksGame >= cooldownCompleteTick)
                {
                    drawLoc.z += 0.5f * (1f + Mathf.Sin((float)Math.PI * 2f * (float)GenTicks.TicksGame / 500f)) * 0.3f;
                }

                // 这是原版引擎的高度微调，我们也保留。
                drawLoc.y += 0.03658537f;

                // 设置缩放，这部分也来自原版代码。
                Vector3 s = new Vector3(def.graphicData.drawSize.x, 1f, def.graphicData.drawSize.y);

                // **最终修改**: 使用我们自己的 OrbMat 属性，而不是父类的私有变量！
                // 这使得子类可以自由决定能量球的外观。
                Graphics.DrawMesh(MeshPool.plane10Back, Matrix4x4.TRS(drawLoc, base.Rotation.AsQuat, s), this.OrbMat.Material, 0);
            }
        }
    }


    // =========================================================================
    // 2. 具体的暗能量引擎子类 (Concrete Child Class)
    //    这个类完全不需要修改，它已经正确地实现了父类的要求。
    // =========================================================================
    [StaticConstructorOnStartup]
    public class Building_Wula_DarkEnergy_Engine : Building_Wula_Engine_Base
    {
        private static readonly CachedMaterial _darkEnergyOrbMat = new CachedMaterial("Wula/Building/Wula_DarkEnergy_Engine_Orb", ShaderDatabase.Cutout);

        protected override CachedMaterial OrbMat => _darkEnergyOrbMat;
    }
}