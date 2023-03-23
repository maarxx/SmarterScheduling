using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmarterScheduling
{
    public class ThingComp_SmarterScheduling : ThingComp
    {
        private Pawn Pawn => (Pawn)this.parent;
        public PawnState pawnState;
        public ScheduleType scheduleType;
        public Area area;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref pawnState, "pawnState");
            Scribe_Values.Look(ref scheduleType, "scheduleType");
            Scribe_Values.Look(ref area, "normalArea");
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref pawnState, "pawnState");
            Scribe_Values.Look(ref scheduleType, "scheduleType");
            Scribe_Values.Look(ref area, "normalArea");
        }

        // Hoisted from:
        // RimWorld.Pawn_GuestTracker
        // public void GuestTrackerTick()
        //public override void CompTick()
        //{
        //    Pawn pawn = Pawn;
        //    if (shouldFarmHemogen && ModsConfig.BiotechActive && pawn.Spawned && pawn.IsHashIntervalTick(750))
        //    {
        //        Need rest = pawn.needs.rest;
        //        if (rest != null
        //            && rest.CurLevel <= 0.4f
        //            && rest.GUIChangeArrow > 0
        //            && !pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss)
        //            && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) > 0.41f
        //            && pawn.BillStack != null
        //            && !pawn.BillStack.Bills.Any((Bill x) => x.recipe == RecipeDefOf.ExtractHemogenPack)
        //            && RecipeDefOf.ExtractHemogenPack.Worker.AvailableOnNow(pawn))
        //        {
        //            HealthCardUtility.CreateSurgeryBill(pawn, RecipeDefOf.ExtractHemogenPack, null, null, sendMessages: false);
        //        }
        //        else if (pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss)
        //                 || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.41f
        //                 || (rest != null && (rest.GUIChangeArrow <= 0 || rest.CurLevel >= 0.6f))
        //                )
        //        {
        //            List<Bill> billsToRemove = new List<Bill>();
        //            foreach (Bill b in pawn.BillStack.Bills)
        //            {
        //                if (b.recipe == RecipeDefOf.ExtractHemogenPack)
        //                {
        //                    billsToRemove.Add(b);
        //                }
        //            }
        //            foreach (Bill b in billsToRemove)
        //            {
        //                b.billStack.Delete(b);
        //            }
        //        }
        //    }
        //}

        //public override IEnumerable<Gizmo> CompGetGizmosExtra()
        //{
        //    foreach (Gizmo item in base.CompGetGizmosExtra())
        //    {
        //        yield return item;
        //    }
        //    Pawn pawn = Pawn;
        //    if (RecipeDefOf.ExtractHemogenPack.Worker.AvailableOnNow(pawn) && (pawn.IsColonist || pawn.IsPrisonerOfColony))
        //    {
        //        Command_Toggle command_Toggle2 = new Command_Toggle();
        //        command_Toggle2.defaultLabel = "Automatically Extract Hemogen";
        //        command_Toggle2.defaultDesc = "Automatically place the 'Extract Hemogen Pack' bill on this pawn whenever they meet the following conditions:\n\n- Pawn Already Resting\n- Rest Need Below 40%\n- No Blood Loss condition\n\nIf the bill is not completed by 60% rest, it will be removed, try again the next night. This ensures we only take it when pawns can sleep off the worst of it.";
        //        command_Toggle2.hotKey = null;
        //        command_Toggle2.icon = DefDatabase<ThingDef>.GetNamed("HemogenPack").uiIcon;
        //        command_Toggle2.isActive = (() => shouldFarmHemogen);
        //        command_Toggle2.toggleAction = delegate
        //        {
        //            shouldFarmHemogen = !shouldFarmHemogen;
        //        };
        //        yield return command_Toggle2;
        //    }
        //}
    }
}
