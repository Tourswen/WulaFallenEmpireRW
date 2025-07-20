using System.Collections.Generic;
using System.Linq; // Added for FirstOrDefault
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld; // For ThingDefOf, StatDefOf, etc.

namespace WulaFallenEmpire
{
    public class JobDriver_IngestWulaEnergy : JobDriver
    {
        private const TargetIndex IngestibleSourceInd = TargetIndex.A;

        private Thing IngestibleSource => job.GetTarget(IngestibleSourceInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 尝试预留能量核心
            if (pawn.Faction != null)
            {
                Thing ingestibleSource = IngestibleSource;
                int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(ingestibleSource, pawn, job.count);
                if (!pawn.Reserve(ingestibleSource, job, 10, maxAmountToPickup, null, errorOnFailed))
                {
                    return false;
                }
                job.count = maxAmountToPickup; // 更新job.count以匹配实际预留数量
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 失败条件：如果能量核心被摧毁、为空或被禁止
            this.FailOn(() => IngestibleSource.DestroyedOrNull() || !IngestibleSource.IngestibleNow);

            // Toil 1: 前往能量核心
            yield return Toils_Goto.GotoThing(IngestibleSourceInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(IngestibleSourceInd);

            // Toil 2: 拾取能量核心并放入carryTracker
            yield return Toils_Haul.StartCarryThing(IngestibleSourceInd);

            // Toil 3: “摄取”能量核心 (模拟咀嚼过程，可以是一个简单的延迟)
            Toil chewToil = ToilMaker.MakeToil("ChewWulaEnergy");
            chewToil.initAction = delegate
            {
                // 设定一个短暂的“咀嚼”时间
                pawn.jobs.curDriver.ticksLeftThisToil = 60; // 1秒
            };
            chewToil.defaultCompleteMode = ToilCompleteMode.Delay;
            yield return chewToil;

            // Toil 4: 最终处理能量摄取
            Toil finalizeToil = ToilMaker.MakeToil("FinalizeWulaEnergyIngest");
            finalizeToil.initAction = delegate
            {
                Pawn actor = finalizeToil.actor;
                // 从Pawn的carryTracker中获取能量核心
                Thing thing = actor.carryTracker.CarriedThing;

                if (thing == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                // 获取乌拉能量需求
                Need_WulaEnergy energyNeed = actor.needs.TryGetNeed<Need_WulaEnergy>();
                if (energyNeed == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }

                // 检查食物来源是否有自定义能量扩展
                ThingDefExtension_EnergySource ext = thing.def.GetModExtension<ThingDefExtension_EnergySource>();
                if (ext == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }

                // 补充乌拉的能量
                energyNeed.CurLevel += ext.energyAmount;

                // 消耗物品
                thing.Destroy(DestroyMode.Vanish);

                // 记录能量摄入 (可选，如果需要类似 NutritionEaten 的记录)
                // actor.records.AddTo(RecordDefOf.NutritionEaten, ext.energyAmount);
            };
            finalizeToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalizeToil;
        }
    }
}
