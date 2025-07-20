using RimWorld;
using Verse;
using Verse.AI;

namespace WulaFallenEmpire
{
    public class WorkGiver_Warden_FeedWula : WorkGiver_Scanner
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

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn prisoner = t as Pawn;

            // Basic validation, similar to vanilla's WorkGiver_Warden_Feed
            if (prisoner == null || prisoner == pawn || !prisoner.IsPrisonerOfColony || !prisoner.guest.CanBeBroughtFood)
            {
                return false;
            }

            // Our custom check: Is the Wula in shutdown?
            Need_WulaEnergy energyNeed = prisoner.needs.TryGetNeed<Need_WulaEnergy>();
            if (energyNeed == null || !energyNeed.IsShutdown)
            {
                return false;
            }

            // Vanilla check: Is the prisoner unable to feed themselves?
            if (!WardenFeedUtility.ShouldBeFed(prisoner))
            {
                return false;
            }

            // CRITICAL vanilla check: Can the warden reserve the prisoner?
            if (!pawn.CanReserve(prisoner, 1, -1, null, forced))
            {
                return false;
            }

            // Check for our energy source
            if (Ext == null || Ext.energySourceDef == null)
            {
                Log.ErrorOnce("WorkGiver_Warden_FeedWula is missing the DefModExtension with a valid energySourceDef.", def.GetHashCode());
                return false;
            }

            if (!FindBestEnergySourceFor(pawn, prisoner, out _, out _))
            {
                JobFailReason.Is("NoFood".Translate());
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn prisoner = (Pawn)t;
            if (FindBestEnergySourceFor(pawn, prisoner, out Thing energySource, out _))
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("WULA_FeedWulaPatient"), energySource, prisoner);
                job.count = 1;
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
