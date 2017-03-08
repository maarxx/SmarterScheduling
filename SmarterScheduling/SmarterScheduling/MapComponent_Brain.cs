using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace SmarterScheduling
{
    class MapComponent_Brain : MapComponent
    {

        public enum PawnState
        {
            SLEEP,
            JOY,
            WORK,
            ANYTHING
        }

        public const float REST_THRESH_CRITICAL   = 0.05F ;

        public const float HUNGER_THRESH_CRITICAL = 0.25F ;

        public const float REST_THRESH_LOW        = 0.35F ;
        public const float REST_THRESH_HIGH       = 0.90F ;

        public const float JOY_THRESH_LOW         = 0.28F ;
        public const float JOY_THRESH_HIGH        = 0.90F ;

        public const string PSYCHE_NAME = "Psyche";

        public Dictionary<Pawn, PawnState> pawnStates;
        public Dictionary<Pawn, Area> lastPawnAreas;
        public Dictionary<Pawn, int> doctorFaults;
        public Faction playerFaction;
        public Area psyche;

        public int slowDown = 0;

        public MapComponent_Brain(Map map) : base(map)
        {
            this.pawnStates = new Dictionary<Pawn, PawnState>();
            this.lastPawnAreas = new Dictionary<Pawn, Area>();
            this.doctorFaults = new Dictionary<Pawn, int>();
            this.playerFaction = getPlayerFaction();
            initPsycheArea();
            initPawnsIntoCollection();
        }

        public void initPsycheArea()
        {
            this.psyche = null;
            foreach (Area a in map.areaManager.AllAreas)
            {
                if (a.ToString() == PSYCHE_NAME)
                {
                    if (a.AssignableAsAllowed(AllowedAreaMode.Humanlike))
                    {
                        this.psyche = a;
                        break;
                    }
                    else
                    {
                        a.SetLabel(PSYCHE_NAME + "2");
                    }
                }
            }
            if (this.psyche == null)
            {
                Area_Allowed newPsyche;
                map.areaManager.TryMakeNewAllowed(AllowedAreaMode.Humanlike, out newPsyche);
                newPsyche.SetLabel(PSYCHE_NAME);
                this.psyche = newPsyche;
            }
        }

        public void initPawnsIntoCollection()
        {
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (!pawnStates.ContainsKey(p))
                {
                    pawnStates.Add(p, PawnState.ANYTHING);
                }
                if (!lastPawnAreas.ContainsKey(p))
                {
                    lastPawnAreas.Add(p, null);
                }
                Area curPawnArea = p.playerSettings.AreaRestriction;
                if (curPawnArea == null || curPawnArea != psyche)
                {
                    lastPawnAreas[p] = curPawnArea;
                }
                if (!doctorFaults.ContainsKey(p))
                {
                    doctorFaults.Add(p, 0);
                }
            }
        }

        public Faction getPlayerFaction()
        {
            Faction playerFaction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.PlayerColony);
            if (playerFaction == null)
            {
                playerFaction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.PlayerTribe);
            }
            return playerFaction;
        }

        public bool isAnyoneNeedingTreatment()
        {
            foreach (Pawn p in map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                if (p.health.HasHediffsNeedingTendByColony())
                {
                    return true;
                }
            }
            return false;
        }

        public bool isAnyoneAwaitingTreatment()
        {
            foreach (Pawn p in map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                if (
                    p.health.HasHediffsNeedingTendByColony()
                    && p.CurJob.def.reportString == "lying down."
                    && !p.pather.Moving
                    && !map.reservationManager.IsReserved(p, this.playerFaction)
                    )
                {
                    return true;
                }
            }
            return false;
        }

        public bool isPawnGainingImmunity(Pawn p)
        {
            foreach (Hediff h in p.health.hediffSet.hediffs)
            {
                if (h is HediffWithComps)
                {
                    HediffWithComps hwc = (HediffWithComps) h;
                    foreach (HediffComp hc in hwc.comps)
                    {
                        if (hc is HediffComp_Immunizable)
                        {
                            HediffComp_Immunizable hci = (HediffComp_Immunizable) hc;
                            if (!hci.FullyImmune)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        // If "treatment" is anywhere in your
        // top 15 WorkGivers, you're a doctor.
        // Otherwise, you're not a doctor.
        public bool isPawnDoctor(Pawn p)
        {
            int i = 0;
            foreach (WorkGiver wg in p.workSettings.WorkGiversInOrderNormal)
            {
                String workGiverString = wg.def.verb + "," + wg.def.priorityInType;
                if (workGiverString == "treat,100")
                {
                    return true;
                }
                else if (workGiverString == "treat,70")
                {
                    return true;
                }
                if (i > 15)
                {
                    return false;
                }
                i++;
            }
            return false;
        }

        public bool isPawnCurrentlyTreating(Pawn p)
        {
            if (p.CurJob.def.reportString == "tending to TargetA.")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool isThereAParty()
        {
            foreach (Lord l in map.lordManager.lords)
            {
                if (l.faction == this.playerFaction)
                {
                    if (l.LordJob != null && l.LordJob is LordJob_Joinable_Party)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool tryToResetPawn(Pawn p)
        {
            if (   p.health.capacities.CanBeAwake
                && p.health.capacities.GetEfficiency(PawnCapacityDefOf.Moving) > 0
                && !p.health.InPainShock
                && !p.Drafted
                && !p.CurJob.playerForced
                )
            {
                p.jobs.StopAll(false);
                return true;
            }
            else
            {
                return false;
            }

        }

        public void setPawnState(Pawn p, PawnState state)
        {

            pawnStates[p] = state;
            TimeAssignmentDef newTad;

            if (state == PawnState.SLEEP)
            {
                newTad = TimeAssignmentDefOf.Sleep;
            }
            else if (state == PawnState.JOY)
            {
                newTad = TimeAssignmentDefOf.Joy;
            }
            else if (state == PawnState.WORK)
            {
                newTad = TimeAssignmentDefOf.Work;
            }
            else
            {
                newTad = TimeAssignmentDefOf.Anything;
            }

            for (int i = 0; i < 24; i++)
            {
                p.timetable.SetAssignment(i, newTad);
            }

        }

        public void restrictPawn(Pawn p, bool restrict)
        {
            if (restrict)
            {
                p.playerSettings.AreaRestriction = psyche;
            }
            else
            {
                p.playerSettings.AreaRestriction = lastPawnAreas[p];
            }
        }

        public override void MapComponentTick()
        {
            slowDown++;
            if (slowDown < 100)
            {
                return;
            }
            else
            {
                slowDown = 0;
            }

            initPsycheArea();
            initPawnsIntoCollection();

            bool anyoneNeedingTreatment = isAnyoneNeedingTreatment();
            bool anyoneAwaitingTreatment = false;
            Random randGen = null;
            
            if (anyoneNeedingTreatment)
            {
                anyoneAwaitingTreatment = isAnyoneAwaitingTreatment();
                if (anyoneAwaitingTreatment)
                {
                    randGen = new Random();
                }
            }

            bool party = isThereAParty();
            //bool alreadyResetDoctorThisTick = false;

            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                float MOOD_THRESH_CRITICAL = p.mindState.mentalBreaker.BreakThresholdExtreme + 0.02F;
                float MOOD_THRESH_LOW      = p.mindState.mentalBreaker.BreakThresholdMajor   + 0.02F;
                float MOOD_THRESH_HIGH     = p.mindState.mentalBreaker.BreakThresholdMinor   + 0.08F;

                bool gainingImmunity = isPawnGainingImmunity(p);

                bool isDoctor = false;
                if (anyoneNeedingTreatment)
                {
                    isDoctor = isPawnDoctor(p);
                } else
                {
                    doctorFaults[p] = 0;
                }

                if (gainingImmunity)
                {
                    setPawnState(p, PawnState.ANYTHING);
                    restrictPawn(p, false);
                }
                else if (p.needs.rest.CurLevel < REST_THRESH_CRITICAL || p.needs.food.CurLevel < HUNGER_THRESH_CRITICAL)
                {
                    setPawnState(p, PawnState.ANYTHING);
                    restrictPawn(p, false);
                }
                else if (p.needs.mood.CurLevel < MOOD_THRESH_CRITICAL)
                {
                    if (p.needs.rest.CurLevel < REST_THRESH_LOW)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        restrictPawn(p, false);
                    }
                    else if (p.needs.rest.GUIChangeArrow > 0)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        restrictPawn(p, false);
                    }
                    else
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawn(p, false);
                    }
                }
                else if (anyoneNeedingTreatment && isDoctor)
                {
                    if (anyoneAwaitingTreatment)
                    {
                        bool pawnTreating = isPawnCurrentlyTreating(p);
                        if (pawnTreating)
                        {
                            doctorFaults[p] = 0;
                            // Deliberately not setPawnState()
                            // Deliberately not restrictPawn()
                        }
                        else
                        {
                            doctorFaults[p] += randGen.Next(5);
                            if (doctorFaults[p] > 20)
                            {
                                setPawnState(p, PawnState.WORK);
                                restrictPawn(p, false);
                                tryToResetPawn(p);
                            }
                            else
                            {
                                setPawnState(p, PawnState.ANYTHING);
                                restrictPawn(p, false);
                            }
                        }
                    }
                    else
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        restrictPawn(p, false);
                    }
                }
                else if (party)
                {
                    setPawnState(p, PawnState.ANYTHING);
                    restrictPawn(p, false);
                }
                else if (p.needs.rest.CurLevel < REST_THRESH_LOW)
                {
                    setPawnState(p, PawnState.SLEEP);
                    restrictPawn(p, false);
                }
                else if (p.needs.rest.GUIChangeArrow > 0)
                {
                    setPawnState(p, PawnState.SLEEP);
                    restrictPawn(p, false);
                }
                else if (anyoneNeedingTreatment && p.health.HasHediffsNeedingTendByColony())
                {
                    setPawnState(p, PawnState.ANYTHING);
                    restrictPawn(p, false);
                }
                else if (pawnStates[p] == PawnState.SLEEP && !(p.needs.rest.GUIChangeArrow > 0) && p.needs.rest.CurLevel > REST_THRESH_HIGH)
                {
                    setPawnState(p, PawnState.JOY);
                    restrictPawn(p, true);
                }
                else if (pawnStates[p] == PawnState.JOY && p.needs.mood.CurLevel < MOOD_THRESH_HIGH)
                {
                    setPawnState(p, PawnState.JOY);
                    restrictPawn(p, true);
                }
                else if (pawnStates[p] == PawnState.JOY && p.needs.joy.CurLevel < JOY_THRESH_HIGH)
                {
                    setPawnState(p, PawnState.JOY);
                    restrictPawn(p, true);
                }
                else if (pawnStates[p] == PawnState.JOY && p.needs.mood.GUIChangeArrow > 0)
                {
                    setPawnState(p, PawnState.JOY);
                    restrictPawn(p, true);
                }
                else if (p.needs.mood.CurLevel < MOOD_THRESH_LOW)
                {
                    setPawnState(p, PawnState.JOY);
                    restrictPawn(p, true);
                }
                else if (p.needs.joy.CurLevel < JOY_THRESH_LOW)
                {
                    setPawnState(p, PawnState.JOY);
                    restrictPawn(p, true);
                }
                else
                {
                    setPawnState(p, PawnState.WORK);
                    restrictPawn(p, false);
                }

                if (
                       pawnStates[p] == PawnState.JOY
                    && p.CurJob.def.reportString == "lying down."
                    && !(p.needs.rest.GUIChangeArrow > 0)
                    && !p.health.HasHediffsNeedingTendByColony()
                )
                {
                    tryToResetPawn(p);
                }

            }
        }

    }
}

/*
// BEGIN DETOUR - we try to let the algorithm treat Doctors fairly,
            // but if somebody isn't getting medical treatment, then
            // we intervene and start handling Doctors differently
            // so that nobody dies.
            if (this.anyoneAwaitingTreatment)
            {
    if (!alreadyResetDoctorThisTick)
    {
        if (isPawnDoctor(p))
        {
            if (state == PawnState.WORK)
            {
                if (!isPawnCurrentlyTreating(p))
                {
                    bool success = tryToResetPawn(p);
                    if (success)
                    {
                        alreadyResetDoctorThisTick = true;
                    }
                }
            }
            if (state == PawnState.JOY)
            {
                doctorFaults[p] += 1;
                if (doctorFaults[p] > 8)
                {
                    setPawnState(p, PawnState.WORK);
                    bool success = tryToResetPawn(p);
                    if (success)
                    {
                        alreadyResetDoctorThisTick = true;
                    }
                    return;
                }
            }
        }
    }
}
            else
            {
    doctorFaults[p] = 0;
}
            // END DETOUR
*/
