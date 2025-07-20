using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace WulaFallenEmpire
{
    public class JobDriver_FeedWulaPatient : JobDriver
    {
        private const TargetIndex FoodSourceInd = TargetIndex.A;
        private const TargetIndex PatientInd = TargetIndex.B;

        private Thing FoodSource => job.GetTarget(FoodSourceInd).Thing;
        private Pawn Patient => (Pawn)job.GetTarget(PatientInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 预留食物来源和病患
            if (!pawn.Reserve(FoodSource, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            if (!pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 失败条件：如果食物来源或病患被摧毁、为空或被禁止
            this.FailOn(() => FoodSource.DestroyedOrNull() || !FoodSource.IngestibleNow);
            this.FailOn(() => Patient.DestroyedOrNull());
            this.FailOn(() => !Patient.InBed()); // 确保病患在床上

            // Toil 1: 前往食物来源
            yield return Toils_Goto.GotoThing(FoodSourceInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(FoodSourceInd);

            // Toil 2: 拾取食物来源
            yield return Toils_Haul.StartCarryThing(FoodSourceInd); // 使用 StartCarryThing 拾取物品

            // Toil 3: 前往病患
            yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch)
                .FailOnDespawnedOrNull(PatientInd);

            // Toil 4: 喂食病患
            Toil feedToil = ToilMaker.MakeToil("FeedWulaPatient");
            feedToil.initAction = delegate
            {
                Pawn actor = feedToil.actor;
                Thing food = actor.carryTracker.CarriedThing; // 医生携带的食物

                if (food == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                // 获取乌拉能量需求
                Need_WulaEnergy energyNeed = Patient.needs.TryGetNeed<Need_WulaEnergy>();
                if (energyNeed == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }

                // 检查食物来源是否有自定义能量扩展
                ThingDefExtension_EnergySource ext = food.def.GetModExtension<ThingDefExtension_EnergySource>();
                if (ext == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }

                // 补充乌拉的能量
                energyNeed.CurLevel += ext.energyAmount;

                // 消耗物品
                food.Destroy(DestroyMode.Vanish); // 销毁医生携带的物品

                // 移除医生携带的物品
                actor.carryTracker.innerContainer.ClearAndDestroyContents();

                // 记录能量摄入 (可选)
                // Patient.records.AddTo(RecordDefOf.NutritionEaten, ext.energyAmount);
            };
            feedToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return feedToil;
        }
    }
}
