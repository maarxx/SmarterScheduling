using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace SmarterScheduling
{
    class MapComponent_SmarterScheduling : MapComponent
    {

        public enum PawnState
        {
            SLEEP,
            JOY,
            WORK,
            ANYTHING
        }

        public enum ImmuneSensitivity
        {
            SENSITIVE,
            BALANCED,
            BRUTAL
        }

        public enum ScheduleType
        {
            WORK,
            MAXMOOD
        }

        public const float REST_THRESH_CRITICAL = 0.05F ;

        public const float HUNGER_THRESH_LOW    = 0.29F ;

        public const float REST_THRESH_LOW      = 0.35F ;
        public const float REST_THRESH_CANSLEEP = 0.74F ;
        public const float REST_THRESH_HIGH     = 0.95F ;

        public const float JOY_THRESH_LOW       = 0.28F ;
        public const float JOY_THRESH_HIGH      = 0.90F ;

        public const string PSYCHE_NAME = "Psyche";

        public Dictionary<Pawn, PawnState> pawnStates;
        public Dictionary<Pawn, Area> lastPawnAreas;
        public Dictionary<Pawn, int> doctorResetTick;

        public Area psyche; 

        public int slowDown;

        public bool enabled;
        public ImmuneSensitivity immuneSensitivity;
        public bool spoonFeeding;

        public ScheduleType curSchedule;

        public MapComponent_SmarterScheduling(Map map) : base(map)
        {
            this.pawnStates = new Dictionary<Pawn, PawnState>();
            this.lastPawnAreas = new Dictionary<Pawn, Area>();
            this.doctorResetTick = new Dictionary<Pawn, int>();

            this.enabled = false;
            this.immuneSensitivity = ImmuneSensitivity.SENSITIVE;
            this.spoonFeeding = true;

            this.curSchedule = ScheduleType.WORK;

            this.slowDown = 0;
            //initPlayerAreas();
            //initPawnsIntoCollection();
            LongEventHandler.QueueLongEvent(ensureComponentExists, null, false, null);
        }

        public static void ensureComponentExists()
        {
            foreach (Map m in Find.Maps)
            {
                if (m.GetComponent<MapComponent_SmarterScheduling>() == null)
                {
                    m.components.Add(new MapComponent_SmarterScheduling(m));
                }
            }
        }

        public void initPlayerAreas()
        {
            this.psyche = null;
            foreach (Area a in map.areaManager.AllAreas)
            {
                if (a.ToString() == PSYCHE_NAME)
                {
                    if (a.AssignableAsAllowed())
                    {
                        this.psyche = a;
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
                map.areaManager.TryMakeNewAllowed(out newPsyche);
                newPsyche.SetLabel(PSYCHE_NAME);
                this.psyche = newPsyche;
            }
        }

        public void initPawnsIntoCollection()
        {
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (!lastPawnAreas.ContainsKey(p))
                {
                    lastPawnAreas.Add(p, null);
                }
                Area curPawnArea = p.playerSettings.AreaRestriction;
                if (curPawnArea == null || curPawnArea != psyche)
                {
                    lastPawnAreas[p] = curPawnArea;
                }
                if (!pawnStates.ContainsKey(p))
                {
                    pawnStates.Add(p, PawnState.ANYTHING);
                }
            }
        }

        public bool isAnyoneNeedingTreatment()
        {
            foreach (Pawn p in map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                if (p.health.HasHediffsNeedingTendByPlayer() && p.playerSettings.medCare > 0)
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
                    p.health.HasHediffsNeedingTendByPlayer()
                    && p.playerSettings.medCare > 0
                    && p.CurJob.def.reportString == "lying down."
                    && p.CurJob.targetA.Thing != null
                    && !p.pather.Moving
                    && !map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer)
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
                if (h.Visible && h is HediffWithComps)
                {
                    HediffWithComps hwc = (HediffWithComps) h;
                    foreach (HediffComp hc in hwc.comps)
                    {
                        if (hc is HediffComp_Immunizable)
                        {
                            HediffComp_Immunizable hci = (HediffComp_Immunizable) hc;
                            if (hci.Immunity > 0 && hci.Immunity < 1)
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
                if (workGiverString == "tend to,100")
                {
                    return true;
                }
                else if (workGiverString == "tend to,90")
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

        public void doctorNotLazy(Pawn p)
        {
            this.doctorResetTick[p] = Find.TickManager.TicksGame;
        }

        public bool isThereAParty()
        {
            foreach (Lord l in map.lordManager.lords)
            {
                if (l.faction == Faction.OfPlayer)
                {
                    if (l.LordJob != null && l.LordJob is LordJob_Joinable_Party)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool pawnCanMove(Pawn p)
        {
            return  p.health.capacities.CanBeAwake
                    && p.health.capacities.GetLevel(PawnCapacityDefOf.Moving) > 0.16F
                    && !p.health.InPainShock;
        }

        public bool tryToResetPawn(Pawn p)
        {
            if (    pawnCanMove(p)
                    && !p.Drafted
                    && !p.CurJob.playerForced
                    && !p.CurJob.def.reportString.Equals("consuming TargetA.")
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

        public void resetAllSchedules(PawnState state)
        {
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                setPawnState(p, state);
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

        public void restrictPawnToPsyche(Pawn p)
        {
            if (pawnCanMove(p))
            {
                p.playerSettings.AreaRestriction = this.psyche;
            }
        }

        public void considerReleasingPawn(Pawn p)
        {
            p.playerSettings.AreaRestriction = this.lastPawnAreas[p];
        }

        public override void MapComponentTick()
        {
            if (enabled)
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

                initPlayerAreas();
                initPawnsIntoCollection();

                bool party = isThereAParty();

                bool anyoneNeedingTreatment = isAnyoneNeedingTreatment();
                bool anyoneAwaitingTreatment = false;
                Pawn laziestDoctor = null;
                bool alreadyResetDoctorThisTick = false;
                if (anyoneNeedingTreatment)
                {
                    anyoneAwaitingTreatment = isAnyoneAwaitingTreatment();
                    if (this.doctorResetTick.Count > 0)
                    {
                        laziestDoctor = this.doctorResetTick.MinBy(kvp => kvp.Value).Key;
                    }
                }

                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    float MOOD_THRESH_CRITICAL = p.mindState.mentalBreaker.BreakThresholdExtreme + 0.02F;
                    float MOOD_THRESH_LOW = p.mindState.mentalBreaker.BreakThresholdMajor + 0.02F;
                    float MOOD_THRESH_HIGH = p.mindState.mentalBreaker.BreakThresholdMinor + 0.08F;

                    bool gainingImmunity = isPawnGainingImmunity(p);

                    bool layingDown = (p.CurJob.def.reportString == "lying down.");
                    bool sleeping = (p.needs.rest.GUIChangeArrow > 0);

                    bool hungry = (p.needs.food.CurLevel < HUNGER_THRESH_LOW);

                    bool tired = (p.needs.rest.CurLevel < REST_THRESH_LOW);
                    bool exhausted = (p.needs.rest.CurLevel < REST_THRESH_CRITICAL);

                    bool needsTreatment = false;
                    bool isDoctor = false;
                    bool currentlyTreating = false;
                    if (anyoneNeedingTreatment)
                    {
                        needsTreatment = p.health.HasHediffsNeedingTendByPlayer();
                        isDoctor = isPawnDoctor(p);
                        currentlyTreating = (p.CurJob.def.reportString == "tending to TargetA.");
                        if (isDoctor)
                        {
                            if (!this.doctorResetTick.ContainsKey(p))
                            {
                                this.doctorResetTick.Add(p, 0);
                            }
                        }
                        else
                        {
                            if (this.doctorResetTick.ContainsKey(p))
                            {
                                this.doctorResetTick.Remove(p);
                            }
                        }
                    }

                    if (gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE)
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                        if (anyoneNeedingTreatment && isDoctor) { doctorNotLazy(p); }
                    }
                    else if (exhausted)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        considerReleasingPawn(p);
                        if (anyoneNeedingTreatment && isDoctor) { doctorNotLazy(p); }
                    }
                    else if (hungry && !sleeping)
                    {
                        //setPawnState(p, PawnState.ANYTHING);
                        for (int i = 0; i < 24; i++)
                        {
                            p.timetable.SetAssignment(i, TimeAssignmentDefOf.Anything);
                        }

                        considerReleasingPawn(p);
                        if (anyoneNeedingTreatment && isDoctor) { doctorNotLazy(p); }
                    }
                    else if (p.needs.mood.CurLevel < MOOD_THRESH_CRITICAL)
                    {
                        if (tired)
                        {
                            setPawnState(p, PawnState.SLEEP);
                            considerReleasingPawn(p);
                        }
                        else if (sleeping)
                        {
                            setPawnState(p, PawnState.SLEEP);
                            considerReleasingPawn(p);
                        }
                        else
                        {
                            setPawnState(p, PawnState.JOY);
                            restrictPawnToPsyche(p);
                        }
                        if (anyoneNeedingTreatment && isDoctor) { doctorNotLazy(p); }
                    }
                    else if (anyoneNeedingTreatment && isDoctor)
                    {
                        if (currentlyTreating)
                        {
                            setPawnState(p, PawnState.ANYTHING);
                            considerReleasingPawn(p);
                            doctorNotLazy(p);
                        }
                        else
                        {
                            if (!anyoneAwaitingTreatment)
                            {
                                setPawnState(p, PawnState.ANYTHING);
                                considerReleasingPawn(p);
                            }
                            else
                            {
                                if (alreadyResetDoctorThisTick || !p.Equals(laziestDoctor))
                                {
                                    setPawnState(p, PawnState.ANYTHING);
                                    considerReleasingPawn(p);
                                }
                                else
                                {
                                    setPawnState(p, PawnState.WORK);
                                    considerReleasingPawn(p);

                                    if (tryToResetPawn(p))
                                    {
                                        alreadyResetDoctorThisTick = true;
                                    }
                                    doctorNotLazy(p);
                                }
                            }
                        }
                    }
                    else if (party)
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                        if (layingDown && !needsTreatment)
                        {
                            if ( !(gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE) )
                            {
                                tryToResetPawn(p);
                            }
                        }
                    }
                    else if (tired)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        considerReleasingPawn(p);
                    }
                    else if (sleeping)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        considerReleasingPawn(p);
                    }
                    else if (needsTreatment)
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                    }
                    else if (pawnStates[p] == PawnState.JOY && p.needs.mood.CurLevel < MOOD_THRESH_HIGH)
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawnToPsyche(p);
                    }
                    else if (pawnStates[p] == PawnState.JOY && p.needs.joy.CurLevel < JOY_THRESH_HIGH)
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawnToPsyche(p);
                    }
                    else if (pawnStates[p] == PawnState.JOY && p.needs.mood.GUIChangeArrow > 0)
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawnToPsyche(p);
                    }
                    else if (p.needs.mood.CurLevel < MOOD_THRESH_LOW)
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawnToPsyche(p);
                    }
                    else if (p.needs.joy.CurLevel < JOY_THRESH_LOW)
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawnToPsyche(p);
                    }
                    else if (gainingImmunity && ( immuneSensitivity == ImmuneSensitivity.SENSITIVE || immuneSensitivity == ImmuneSensitivity.BALANCED) )
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                    }
                    else if (p.needs.rest.CurLevel > REST_THRESH_HIGH && !(p.needs.rest.GUIChangeArrow > 0))
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawnToPsyche(p);
                    }
                    else if (curSchedule == ScheduleType.MAXMOOD && p.needs.rest.CurLevel < REST_THRESH_CANSLEEP)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        considerReleasingPawn(p);
                    }
                    else if (curSchedule == ScheduleType.MAXMOOD)
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawnToPsyche(p);
                    }
                    else
                    {
                        setPawnState(p, PawnState.WORK);
                        considerReleasingPawn(p);
                    }

                    if (layingDown && !sleeping && !needsTreatment)
                    {
                        if (!(gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE))
                        {
                            if (pawnStates[p] == PawnState.JOY)
                            {
                                tryToResetPawn(p);
                            }
                            else if (!spoonFeeding && hungry)
                            {
                                tryToResetPawn(p);
                            }
                        }
                    }

                }
            }
        }

    }
}
