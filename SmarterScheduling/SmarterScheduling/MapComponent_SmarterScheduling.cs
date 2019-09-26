using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // TODO: Optimize this O(n) into (O1) by subscribing to Pawn_HealthTracker.CheckForStateChange
        // via Harmony Postfix, then query them at that time on whether they need tending, and maintain
        // a standing collection of all pawns needing tending.
        public void isAnyPendingTreatments(out bool needing, out bool awaiting, out Pawn firstAwaiting)
        {
            needing = false;
            awaiting = false;
            firstAwaiting = null;
            foreach (Pawn p in map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                if (HealthAIUtility.ShouldBeTendedNowByPlayer(p))
                {
                    needing = true;
                    if (WorkGiver_Tend.GoodLayingStatusForTend(p, null) && !map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer))
                    {
                        Log.Message("awaiting: " + p.Name.ToStringShort);
                        awaiting = true;
                        firstAwaiting = p;
                        return;
                    }
                }
            }
            IEnumerable<Pawn> animals = from p in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                                        where p.RaceProps.Animal
                                        select p;
            foreach (Pawn p in animals)
            {
                if (HealthAIUtility.ShouldBeTendedNowByPlayer(p))
                {
                    needing = true;
                    if (WorkGiver_Tend.GoodLayingStatusForTend(p, null) && !map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer))
                    {
                        Log.Message("awaiting: " + p.Name.ToStringShort);
                        awaiting = true;
                        firstAwaiting = p;
                        return;
                    }
                }
            }
            return;
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
            return doesPawnHaveWorkGiverAtPriority(p, new string[] { "tend to,100", "tend to,90" }, 15);
        }

        public bool isPawnHandler(Pawn p)
        {
            return doesPawnHaveWorkGiverAtPriority(p, new string[] { "tame,80", "training,70" }, 15);
        }

        public bool doesPawnHaveWorkGiverAtPriority(Pawn p, string[] workGiverStrings, int minPriority)
        {
            int i = 0;
            foreach (WorkGiver wg in p.workSettings.WorkGiversInOrderNormal)
            {
                String workGiverString = wg.def.verb + "," + wg.def.priorityInType;
                if (workGiverStrings.Contains(workGiverString))
                {
                    return true;
                }
                if (i > minPriority)
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
                if (l.faction == Faction.OfPlayer && lordIsPartyLord(l))
                {
                    return true;
                }
            }
            return false;
        }

        public bool pawnAttendingParty(Pawn p)
        {
            if (lordIsPartyLord(p.GetLord()))
            {
                return true;
            }
            return false;
        }

        public bool lordIsPartyLord(Lord l)
        {
            if (l != null
                && l.LordJob != null
                && (l.LordJob is LordJob_Joinable_Party || l.LordJob is LordJob_Joinable_MarriageCeremony)
               )
            {
                return true;
            }
            return false;
        }

        public bool isAnimalSleepingTime()
        {
            int hour = GenLocalDate.HourOfDay(map.Tile);
            return (hour >= 22 || hour <= 5);
        }

        public bool pawnCanMove(Pawn p)
        {
            return p.health.capacities.CanBeAwake
                   && p.health.capacities.GetLevel(PawnCapacityDefOf.Moving) > 0.16F
                   && !p.health.InPainShock;
        }

        public bool shouldDisruptPawn(Pawn p)
        {
            return pawnCanMove(p)
                   && !p.Drafted
                   && !p.CurJob.playerForced
                   && !p.CurJob.def.reportString.Equals("consuming TargetA.");
        }
        public bool tryToResetPawn(Pawn p)
        {
            if (shouldDisruptPawn(p))
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

        public void setPawnState(Pawn p, PawnState state, bool forcedPsyche = false)
        {
            pawnStates[p] = state;

            // Weird bug where, if they are in Joy+Psyche,
            // and we simultaneously update them to Sleep+Released,
            // they sometimes sleep in Psyche.
            // So we Release and then Sleep across two cycles.
            bool twoPhase = (state == PawnState.SLEEP) && (p.playerSettings.AreaRestriction == this.psyche);

            if (state == PawnState.JOY || forcedPsyche)
            {
                restrictPawnToPsyche(p);
            }
            else
            {
                considerReleasingPawn(p);
            }

            TimeAssignmentDef newTad;
            if (state == PawnState.SLEEP)
            {
                newTad = TimeAssignmentDefOf.Sleep;
            }
            else if (state == PawnState.WORK || forcedPsyche)
            {
                newTad = TimeAssignmentDefOf.Work;
            }
            else if (state == PawnState.JOY)
            {
                newTad = TimeAssignmentDefOf.Joy;
            }
            else
            {
                newTad = TimeAssignmentDefOf.Anything;
            }

            // See above "weird bug".
            if (twoPhase) { return; }

            for (int i = 0; i < 24; i++)
            {
                p.timetable.SetAssignment(i, newTad);
            }
        }

        public void restrictPawnToPsyche(Pawn p)
        {
            if (shouldDisruptPawn(p))
            {
                p.playerSettings.AreaRestriction = this.psyche;
            }
        }

        public void considerReleasingPawn(Pawn p)
        {
            p.playerSettings.AreaRestriction = this.lastPawnAreas[p];
        }

        public void updateDoctorResetTickCollection(Pawn p, bool isDoctor)
        {
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

        bool anyoneNeedingTreatment = false;
        bool anyoneAwaitingTreatment = false;
        Pawn firstAwaiting = null;

        Pawn laziestDoctor = null;
        bool alreadyResetDoctorThisTick = false;

        public void doctorSubroutine(Pawn p)
        {
            bool currentlyTreating = (p.CurJob.def.reportString == "tending to TargetA.");
            bool isReserved = map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer);
            if (currentlyTreating || isReserved || (p == firstAwaiting))
            {
                setPawnState(p, PawnState.ANYTHING);
                doctorNotLazy(p);
            }
            else
            {
                if (alreadyResetDoctorThisTick || !p.Equals(laziestDoctor))
                {
                    setPawnState(p, PawnState.ANYTHING);
                }
                else
                {
                    setPawnState(p, PawnState.WORK);

                    Log.Message("Resetting (" + p.Name.ToStringShort + ") for Doctor, current job: " + p.CurJob.ToString());
                    if (tryToResetPawn(p))
                    {
                        alreadyResetDoctorThisTick = true;
                    }
                    doctorNotLazy(p);
                }
            }
        }

        public override void MapComponentTick()
        {
            if (!enabled)
            {
                return;
            }

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

            bool isAnimalSleepTime = isAnimalSleepingTime();

            isAnyPendingTreatments(out anyoneNeedingTreatment, out anyoneAwaitingTreatment, out firstAwaiting);

            if (anyoneNeedingTreatment)
            {
                if (this.doctorResetTick.Count > 0)
                {
                    laziestDoctor = this.doctorResetTick.MinBy(kvp => kvp.Value).Key;
                }
            }

            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                float EXTREME_BREAK = p.mindState.mentalBreaker.BreakThresholdExtreme + 0.02f;
                float MAJOR_BREAK = p.mindState.mentalBreaker.BreakThresholdMajor + 0.02f;
                float MINOR_BREAK = p.mindState.mentalBreaker.BreakThresholdMinor + 0.02f;

                bool layingDown = (p.CurJob.def.reportString == "lying down.");
                bool sleeping = (p.needs.rest.GUIChangeArrow > 0);
                bool justWokeRested = !sleeping && (p.needs.rest.CurLevel > 0.95f);
                
                bool hungry = (p.needs.food.CurLevel < 0.29f);

                float rest = p.needs.rest.CurLevel;
                float joy = p.needs.rest.CurLevel;
                float mood = p.needs.mood.CurLevel;

                bool canSleep = (rest < 0.74f);

                bool stateSleep = (pawnStates[p] == PawnState.SLEEP);
                bool stateJoy = (pawnStates[p] == PawnState.JOY);
                bool stateWork = (pawnStates[p] == PawnState.WORK);

                bool gainingImmunity = isPawnGainingImmunity(p);

                bool isHandler = false;

                bool needsTreatment = false;
                bool isDoctor = false;
                if (anyoneNeedingTreatment)
                {
                    needsTreatment = HealthAIUtility.ShouldBeTendedNowByPlayer(p);
                    isDoctor = isPawnDoctor(p);
                    updateDoctorResetTickCollection(p, isDoctor);
                }

                if (gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE)
                {
                    setPawnState(p, PawnState.ANYTHING);
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
                else if (rest < 0.10f)
                {
                    setPawnState(p, PawnState.SLEEP);
                    if (anyoneNeedingTreatment && isDoctor) { doctorNotLazy(p); }
                }
                else if (anyoneAwaitingTreatment && isDoctor)
                {
                    if (mood < MAJOR_BREAK)
                    {
                        setPawnState(p, PawnState.JOY);
                        doctorNotLazy(p);
                    }
                    else
                    {
                        doctorSubroutine(p);
                    }
                }
                else if (party)
                {
                    setPawnState(p, PawnState.ANYTHING);
                    if (!pawnAttendingParty(p))
                    {
                        if (needsTreatment
                            || (gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE))
                        {
                            // Don't interrupt sick pawns for parties. Weirdo.
                        }
                        else
                        {
                            tryToResetPawn(p);
                        }
                    }
                }
                else if (rest < 0.32f)
                {
                    setPawnState(p, PawnState.SLEEP);
                }
                else if (mood < MAJOR_BREAK)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if (needsTreatment)
                {
                    setPawnState(p, PawnState.ANYTHING);
                }
                else if ((sleeping || stateSleep) && rest > 0.45f && mood < MINOR_BREAK)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if (p.needs.joy.CurLevel < 0.30f)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if (curSchedule == ScheduleType.MAXMOOD && p.needs.mood.CurLevel < MINOR_BREAK)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if (justWokeRested)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if (stateJoy && p.needs.joy.CurLevel < 0.90f)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if (stateJoy && p.needs.mood.CurLevel < MINOR_BREAK)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if (stateJoy && p.needs.mood.GUIChangeArrow > 0)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if ((canSleep || sleeping) && (stateJoy || stateSleep))
                {
                    setPawnState(p, PawnState.SLEEP);
                }
                else if (gainingImmunity && immuneSensitivity == ImmuneSensitivity.BALANCED)
                {
                    setPawnState(p, PawnState.ANYTHING);
                }
                else if (isAnimalSleepTime && (isHandler = isPawnHandler(p)) && canSleep)
                {
                    setPawnState(p, PawnState.SLEEP);
                }
                else if (isAnimalSleepTime && isHandler)
                {
                    setPawnState(p, PawnState.JOY);
                }
                else if (curSchedule == ScheduleType.MAXMOOD && canSleep)
                {
                    setPawnState(p, PawnState.SLEEP);
                }
                else if (curSchedule == ScheduleType.MAXMOOD)
                {
                    setPawnState(p, PawnState.JOY);
                }
                
                else
                {
                    setPawnState(p, PawnState.WORK);
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
