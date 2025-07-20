using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace WulaFallenEmpire
{
    public class WorkGiver_FeedWulaPatient : WorkGiver_Scanner
    {
        private WorkGiverDefExtension_FeedWula ext;

        private WorkGiverDefExtension_FeedWula Ext
        {
            get
            {
                if (ext == null)
                {
                    ext = def.GetModExtension<WorkGiverDefExtension_FeedWula>();
                }
                return ext;
            }
        }

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDef.Named("WulaSpecies"));

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            // Mimic vanilla: Scan all pawns in bed.
            return pawn.Map.mapPawns.AllPawns.Where(p => p.InBed());
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn patient = t as Pawn;

            // Basic validation, similar to vanilla
            if (patient == null || patient == pawn || !patient.InBed() || patient.def.defName != "WulaSpecies")
            {
                return false;
            }

            // Our custom check: Is the Wula in shutdown?
            Need_WulaEnergy energyNeed = patient.needs.TryGetNeed<Need_WulaEnergy>();
            if (energyNeed == null || !energyNeed.IsShutdown)
            {
                return false;
            }

            // CRITICAL vanilla check: If the patient is a prisoner, this is a warden's job, not a doctor's.
            // This prevents conflicts between two different work types trying to do the same thing.
            if (WardenFeedUtility.ShouldBeFed(patient))
            {
                return false;
            }

            // CRITICAL vanilla check: Can the doctor reserve the patient?
            // This prevents multiple doctors from trying to feed the same patient at the same time.
            if (!pawn.CanReserve(patient, 1, -1, null, forced))
            {
                return false;
            }

            // Check for our energy source
            if (Ext == null || Ext.energySourceDef == null)
            {
                Log.ErrorOnce("WorkGiver_FeedWulaPatient is missing the DefModExtension with a valid energySourceDef.", def.GetHashCode());
                return false;
            }

            if (!FindBestEnergySourceFor(pawn, patient, out _, out _))
            {
                // Mimic vanilla: Provide a reason for failure.
                JobFailReason.Is("NoFood".Translate()); // Using vanilla translation key for simplicity
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn patient = (Pawn)t;
            if (FindBestEnergySourceFor(pawn, patient, out Thing energySource, out _))
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("WULA_FeedWulaPatient"), energySource, patient);
                job.count = 1; // Energy cores are single-use.
                return job;
            }
            return null;
        }

        private bool FindBestEnergySourceFor(Pawn getter, Pawn eater, out Thing foodSource, out ThingDef foodDef)
        {
            foodSource = null;
            foodDef = null;

            if (Ext == null || Ext.energySourceDef == null)
            {
                return false;
            }

            // CRITICAL vanilla check is embedded here: CanReserve(x) on the food source itself.
            foodSource = GenClosest.ClosestThingReachable(
                getter.Position,
                getter.Map,
                ThingRequest.ForDef(Ext.energySourceDef),
                PathEndMode.OnCell,
                TraverseParms.For(getter),
                9999f,
                (Thing x) => !x.IsForbidden(getter) && getter.CanReserve(x)
            );

            if (foodSource != null)
            {
                foodDef = foodSource.def;
                return true;
            }

            return false;
        }
    }
}
