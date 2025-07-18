using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib; // 引入Harmony库

// 定义一个新的HediffComp，用于标记可以赋予机械师能力的Hediff
public class HediffComp_MakesMechanitor : HediffComp
{
    // 这个组件本身不需要任何特殊逻辑，它的存在就是标记
}

// Harmony Patch类，用于修改MechanitorUtility.ShouldBeMechanitor方法
[HarmonyPatch(typeof(MechanitorUtility), "ShouldBeMechanitor")]
public static class MechanitorShouldBeMechanitorPatch
{
    // Postfix方法将在原始方法执行后运行
    // originalResult 是原始方法的返回值
    // pawn 是原始方法的参数
    public static void Postfix(Pawn pawn, ref bool __result)
    {
        // 如果原始方法已经返回true，则无需进一步检查
        if (__result)
        {
            return;
        }

        // 检查Biotech DLC是否激活且Pawn属于玩家安全派系
        if (ModsConfig.BiotechActive && pawn.Faction.IsPlayerSafe())
        {
            // 遍历Pawn的所有Hediff，检查是否存在HediffComp_MakesMechanitor组件
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.TryGetComp<HediffComp_MakesMechanitor>() != null)
                {
                    __result = true; // 如果找到，则将结果设置为true
                    return;
                }
            }
        }
    }
}
