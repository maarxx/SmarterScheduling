using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI.Group;

namespace SmarterScheduling
{
    class MapComponent_SmarterScheduling : MapComponent
    {

        public void doLogging(string s)
        {
            if (enableLogging)
            {
                Log.Message(s);
            }
        }

        public enum PawnState
        {
            SLEEP,
            JOY,
            WORK,
            MEDITATE,
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

        public const string RECREATION_NAME = "Joy";
        public const string MEDIDATION_NAME = "Medi";

        public Dictionary<Pawn, PawnState> pawnStates;
        public Dictionary<Pawn, ScheduleType> pawnSchedules;
        public Dictionary<Pawn, bool> shouldResetPawnOnHungry;
        public Dictionary<Pawn, Area> lastPawnAreas;
        public Dictionary<Pawn, int> doctorResetTick;

        public Area recreation;
        public Area meditation;

        public int slowDown;

        public bool enabled;
        public ImmuneSensitivity immuneSensitivity;
        public bool spoonFeeding;

        public bool doubleSleep;
        public bool doubleEat;

        public bool manageMeditation;

        public bool joyHoldExtra;

        public static JobGiver_OptimizeApparel apparelCheckerInstance;
        public static MethodInfo apparelCheckerMethod;

        public bool enableLogging;

        public MapComponent_SmarterScheduling(Map map) : base(map)
        {
            this.pawnStates = new Dictionary<Pawn, PawnState>();
            this.pawnSchedules = new Dictionary<Pawn, ScheduleType>();
            this.shouldResetPawnOnHungry = new Dictionary<Pawn, bool>();
            this.lastPawnAreas = new Dictionary<Pawn, Area>();
            this.doctorResetTick = new Dictionary<Pawn, int>();

            this.enabled = false;
            this.immuneSensitivity = ImmuneSensitivity.SENSITIVE;
            this.spoonFeeding = true;

            apparelCheckerInstance = new JobGiver_OptimizeApparel();
            apparelCheckerMethod = apparelCheckerInstance.GetType().
                GetMethod("TryGiveJob", BindingFlags.NonPublic | BindingFlags.Instance);

            this.doubleSleep = false;
            this.doubleEat = false;

            this.manageMeditation = false;

            this.joyHoldExtra = false;

            this.enableLogging = false;

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

        public void initPlayerAreas(out Area savedArea, String areaName)
        {
            savedArea = null;
            foreach (Area a in map.areaManager.AllAreas)
            {
                if (a.ToString() == areaName)
                {
                    if (a.AssignableAsAllowed())
                    {
                        savedArea = a;
                    }
                    else
                    {
                        a.SetLabel(areaName + "2");
                    }
                }
            }
            if (savedArea == null)
            {
                Area_Allowed newArea;
                map.areaManager.TryMakeNewAllowed(out newArea);
                newArea.SetLabel(areaName);
                savedArea = newArea;
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
                if (curPawnArea == null || (curPawnArea != recreation && curPawnArea != meditation))
                {
                    lastPawnAreas[p] = curPawnArea;
                }
                if (!pawnStates.ContainsKey(p))
                {
                    pawnStates.Add(p, PawnState.ANYTHING);
                }
                if (!pawnSchedules.ContainsKey(p))
                {
                    pawnSchedules.Add(p, ScheduleType.WORK);
                }
                if (!shouldResetPawnOnHungry.ContainsKey(p))
                {
                    shouldResetPawnOnHungry.Add(p, true);
                }
            }
        }

        // TODO: Optimize this O(n) into O(1) by subscribing to Pawn_HealthTracker.CheckForStateChange
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
            return doesPawnHaveWorkGiverAtPriority(p, new string[] { "tame,80", "train,70" }, 25);
        }

        public bool isPawnNightOwl(Pawn p)
        {
            foreach (Trait t in p.story.traits.allTraits)
            {
                if (t.def.defName == "NightOwl")
                {
                    return true;
                }
            }
            return false;
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

        public bool isDaytime()
        {
            int hour = GenLocalDate.HourOfDay(map.Tile);
            return (hour >= 11 && hour <= 18);
        }

        public bool pawnCanMove(Pawn p)
        {
            return p.health.capacities.CanBeAwake
                   && p.health.capacities.GetLevel(PawnCapacityDefOf.Moving) > 0.16F
                   && !p.health.InPainShock;
        }

        public bool shouldDisruptPawn(Pawn p, bool dontDisruptEating = true)
        {
            return pawnCanMove(p)
                   && !(p.Drafted)
                   && !(p.CurJob.playerForced)
                   && !(dontDisruptEating && p.CurJob.def == JobDefOf.Ingest)
                   && !(p.CurJob.def == JobDefOf.Wear)
                   && !(p.CurJob.def == JobDefOf.RemoveApparel && p.CurJob.targetA.Thing is Apparel && ((Apparel)p.CurJob.targetA.Thing).Wearer != null);
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

        public void resetSelectedPawnsSchedules(PawnState state)
        {
            initPlayerAreas(out this.recreation, RECREATION_NAME);
            initPlayerAreas(out this.meditation, MEDIDATION_NAME);
            initPawnsIntoCollection();
            foreach (object obj in Find.Selector.SelectedObjects)
            {
                if (obj is Pawn)
                {
                    Pawn p = (Pawn)obj;
                    setPawnState(p, state);
                }
            }
        }

        public void resetSelectedPawnsScheduleTypes(ScheduleType type)
        {
            initPlayerAreas(out this.recreation, RECREATION_NAME);
            initPlayerAreas(out this.meditation, MEDIDATION_NAME);
            initPawnsIntoCollection();
            foreach (object obj in Find.Selector.SelectedObjects)
            {
                if (obj is Pawn)
                {
                    Pawn p = (Pawn)obj;
                    pawnSchedules.SetOrAdd(p, type);
                }
            }
        }

        public void setPawnState(Pawn p, PawnState state, bool doRestriction = true)
        {
            pawnStates[p] = state;

            if (doRestriction)
            {
                if (state == PawnState.JOY || state == PawnState.MEDITATE)
                {
                    restrictPawnToActivityArea(p, state);
                }
                else
                {
                    considerReleasingPawn(p);
                }
            }

            TimeAssignmentDef newTad;
            if (state == PawnState.SLEEP)
            {
                newTad = TimeAssignmentDefOf.Sleep;
            }
            else if (state == PawnState.WORK)
            {
                newTad = TimeAssignmentDefOf.Work;
            }
            else if (state == PawnState.JOY)
            {
                newTad = TimeAssignmentDefOf.Joy;
            }
            else if (state == PawnState.MEDITATE)
            {
                newTad = TimeAssignmentDefOf.Meditate;
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

        public void restrictPawnToActivityArea(Pawn p, PawnState newState, bool forced = false)
        {
            if (forced || shouldDisruptPawn(p))
            {
                if (newState == PawnState.MEDITATE)
                {
                    p.playerSettings.AreaRestriction = this.meditation;
                }
                else
                {
                    p.playerSettings.AreaRestriction = this.recreation;
                }
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

        public void doctorSubroutine(Pawn p, Pawn awaiting)
        {
            bool currentlyTreating = (p.CurJob.def.reportString == "tending to TargetA.");
            bool isReserved = map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer);
            if (currentlyTreating || isReserved || (p == firstAwaiting))
            {
                //setPawnState(p, PawnState.ANYTHING);
                doctorNotLazy(p);
            }
            else
            {
                if (alreadyResetDoctorThisTick || !p.Equals(laziestDoctor))
                {
                    //setPawnState(p, PawnState.ANYTHING);
                }
                else
                {
                    //if (!p.CanReach(awaiting.Position, Verse.AI.PathEndMode.Touch, Danger.Deadly))
                    if (p.playerSettings.AreaRestriction != null && !p.playerSettings.AreaRestriction[awaiting.Position])
                    {
                        Log.Message("Was about to reset (" + p.Name.ToStringShort + ") for Doctor, but couldn't reach (" + awaiting.Name.ToStringShort + ")");
                        doctorNotLazy(p);
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

            initPlayerAreas(out this.recreation, RECREATION_NAME);
            initPlayerAreas(out this.meditation, MEDIDATION_NAME);
            initPawnsIntoCollection();

            bool party = isThereAParty();

            bool isAnimalSleepTime = isAnimalSleepingTime();

            bool isDay = isDaytime();

            isAnyPendingTreatments(out anyoneNeedingTreatment, out anyoneAwaitingTreatment, out firstAwaiting);

            if (anyoneNeedingTreatment)
            {
                alreadyResetDoctorThisTick = false;

                if (this.doctorResetTick.Count > 0)
                {
                    laziestDoctor = this.doctorResetTick.MinBy(kvp => kvp.Value).Key;
                }
            }

            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned.ListFullCopy();
            foreach (Pawn p in pawns)
            {
                float EXTREME_BREAK = p.mindState.mentalBreaker.BreakThresholdExtreme + 0.02f;
                float MAJOR_BREAK = p.mindState.mentalBreaker.BreakThresholdMajor + 0.02f;
                float MINOR_BREAK = p.mindState.mentalBreaker.BreakThresholdMinor + 0.02f;

                bool layingDown = (p.CurJob.def.reportString == "lying down.");
                bool sleeping = (p.needs.rest.GUIChangeArrow > 0);
                bool justWokeRested = !sleeping && (p.needs.rest.CurLevel > 0.95f);
                
                bool hungry = (p.needs.food.CurLevel < 0.31f);
                if (!hungry) { shouldResetPawnOnHungry[p] = true; }

                bool shouldEatBeforeWork = (p.needs.food.CurLevel < 0.70f);
                Thing invFood = FoodUtility.BestFoodInInventory(p);
                bool hasFood = (invFood != null);

                float rest = p.needs.rest.CurLevel;
                float joy = p.needs.rest.CurLevel;
                float mood = p.needs.mood.CurLevel;

                object changeClothesJob = apparelCheckerMethod.Invoke(apparelCheckerInstance, new object[] { p });
                bool shouldChangeClothes = changeClothesJob != null;

                bool canSleep = (rest < 0.74f);

                ScheduleType pawnSchedule = pawnSchedules.TryGetValue(p, ScheduleType.WORK);

                bool stateSleep = (pawnStates[p] == PawnState.SLEEP);
                bool stateJoy = (pawnStates[p] == PawnState.JOY);
                bool stateWork = (pawnStates[p] == PawnState.WORK);
                bool stateMeditate = (pawnStates[p] == PawnState.MEDITATE);

                bool shouldMeditate = p.HasPsylink && p.psychicEntropy.CurrentPsyfocus < p.psychicEntropy.TargetPsyfocus;

                bool gainingImmunity = isPawnGainingImmunity(p);

                bool isHandler = false;

                bool recreationExists = recreation.TrueCount > 0;

                bool currentlyTreating = false;
                bool needsTreatment = false;
                bool isDoctor = false;
                if (anyoneNeedingTreatment)
                {
                    needsTreatment = HealthAIUtility.ShouldBeTendedNowByPlayer(p);
                    isDoctor = isPawnDoctor(p);
                    currentlyTreating = (p.CurJob.def.reportString == "tending to TargetA.");
                    updateDoctorResetTickCollection(p, isDoctor);
                }

                if (gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE)
                {
                    doLogging(p.Name.ToStringShort + ": " + "gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE");
                    setPawnState(p, PawnState.ANYTHING);
                    if (anyoneNeedingTreatment && isDoctor) { doctorNotLazy(p); }
                }
                else if (hungry && !sleeping && !needsTreatment && !shouldChangeClothes)
                {
                    doLogging(p.Name.ToStringShort + ": " + "hungry && !sleeping && !needsTreatment");
                    setPawnState(p, PawnState.JOY, false);
                    if (shouldDisruptPawn(p, false))
                    {
                        if (recreation[p.Position])
                        {
                            shouldResetPawnOnHungry[p] = false;
                        }

                        if (recreationExists && shouldResetPawnOnHungry[p])
                        {
                            restrictPawnToActivityArea(p, PawnState.JOY, true);
                        }
                        else
                        {
                            considerReleasingPawn(p);
                        }

                    }
                    if (anyoneNeedingTreatment && isDoctor) { doctorNotLazy(p); }
                }
                else if (currentlyTreating)
                {
                    doLogging(p.Name.ToStringShort + ": " + "currentlyTreating");
                    setPawnState(p, PawnState.ANYTHING);
                    doctorNotLazy(p);
                }
                else if (anyoneAwaitingTreatment && isDoctor)
                {
                    doLogging(p.Name.ToStringShort + ": " + "anyoneAwaitingTreatment && isDoctor");
                    if (rest < 0.10f)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        doctorNotLazy(p);
                    }
                    else if ((sleeping || stateSleep) && rest < 0.15f)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        doctorNotLazy(p);
                    }
                    else if (mood < MAJOR_BREAK)
                    {
                        setPawnState(p, PawnState.JOY);
                        doctorNotLazy(p);
                    }
                    else
                    {
                        setPawnState(p, PawnState.ANYTHING);
                    }

                    doctorSubroutine(p, firstAwaiting);
                }
                else if (party)
                {
                    doLogging(p.Name.ToStringShort + ": " + "party");
                    setPawnState(p, PawnState.ANYTHING);
                    if (!pawnAttendingParty(p))
                    {
                        if (needsTreatment || (gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE))
                        {
                            // Don't interrupt sick pawns for parties. Weirdo.
                        }
                        else
                        {
                            tryToResetPawn(p);
                        }
                    }
                }
                else if (shouldChangeClothes)
                {
                    doLogging(p.Name.ToStringShort + ": " + "shouldChangeClothes");
                    setPawnState(p, PawnState.ANYTHING);
                }
                else if (rest < 0.32f)
                {
                    doLogging(p.Name.ToStringShort + ": " + "rest < 0.32f");
                    setPawnState(p, PawnState.SLEEP);
                    //if (!(layingDown || sleeping)) { tryToResetPawn(p); }
                    if (p.CurJobDef == JobDefOf.Meditate) { tryToResetPawn(p); }
                }
                else if ((sleeping || stateSleep) && rest < 0.45f)
                {
                    doLogging(p.Name.ToStringShort + ": " + "(sleeping || stateSleep) && rest < 0.45f");
                    setPawnState(p, PawnState.SLEEP);
                }
                else if (mood < MAJOR_BREAK)
                {
                    doLogging(p.Name.ToStringShort + ": " + "mood < MAJOR_BREAK");
                    setPawnState(p, PawnState.JOY);
                    if (recreationExists && !recreation[p.Position]) { tryToResetPawn(p); }
                }
                else if (needsTreatment)
                {
                    doLogging(p.Name.ToStringShort + ": " + "needsTreatment");
                    setPawnState(p, PawnState.ANYTHING);
                }
                else if ((sleeping || stateSleep) && rest > 0.45f && mood < MINOR_BREAK)
                {
                    doLogging(p.Name.ToStringShort + ": " + "(sleeping || stateSleep) && rest > 0.45f && mood < MINOR_BREAK");
                    setPawnState(p, PawnState.JOY);
                }
                else if (p.needs.joy.CurLevel < 0.30f)
                {
                    doLogging(p.Name.ToStringShort + ": " + "p.needs.joy.CurLevel < 0.30f");
                    setPawnState(p, PawnState.JOY);
                }
                else if (pawnSchedule == ScheduleType.MAXMOOD && p.needs.mood.CurLevel < MINOR_BREAK)
                {
                    doLogging(p.Name.ToStringShort + ": " + "curSchedule == ScheduleType.MAXMOOD && p.needs.mood.CurLevel < MINOR_BREAK");
                    setPawnState(p, PawnState.JOY);
                }
                else if (justWokeRested)
                {
                    doLogging(p.Name.ToStringShort + ": " + "justWokeRested");
                    setPawnState(p, PawnState.JOY);
                }
                else if (stateJoy && p.needs.joy.CurLevel < 0.80f)
                {
                    doLogging(p.Name.ToStringShort + ": " + "stateJoy && p.needs.joy.CurLevel < 0.80f");
                    setPawnState(p, PawnState.JOY);
                }
                else if (stateJoy && p.needs.joy.GUIChangeArrow > 0 && p.needs.joy.CurLevel < 1f)
                {
                    doLogging(p.Name.ToStringShort + ": " + "stateJoy && p.needs.joy.GUIChangeArrow > 0 && p.needs.joy.CurLevel < 1f");
                    setPawnState(p, PawnState.JOY);
                }
                else if (manageMeditation && shouldMeditate)
                {
                    doLogging(p.Name.ToStringShort + ": " + "manageMeditation && shouldMeditate");
                    setPawnState(p, PawnState.MEDITATE);
                }
                else if ((stateJoy || stateMeditate) && joyHoldExtra && p.needs.beauty.GUIChangeArrow > 0)
                {
                    doLogging(p.Name.ToStringShort + ": " + "(stateJoy || stateMeditate) && joyHoldExtra && p.needs.beauty.GUIChangeArrow > 0");
                    setPawnState(p, PawnState.JOY);
                }
                else if ((stateJoy || stateMeditate) && joyHoldExtra && p.needs.comfort.GUIChangeArrow > 0)
                {
                    doLogging(p.Name.ToStringShort + ": " + "(stateJoy || stateMeditate) && joyHoldExtra && p.needs.comfort.GUIChangeArrow > 0");
                    setPawnState(p, PawnState.JOY);
                }
                else if ((stateJoy || stateMeditate) && p.needs.mood.GUIChangeArrow > 0)
                {
                    doLogging(p.Name.ToStringShort + ": " + "(stateJoy || stateMeditate) && p.needs.mood.GUIChangeArrow > 0");
                    setPawnState(p, PawnState.JOY);
                }
                else if ((stateJoy || stateMeditate) && p.needs.mood.CurLevel < MINOR_BREAK)
                {
                    doLogging(p.Name.ToStringShort + ": " + "(stateJoy || stateMeditate) && p.needs.mood.CurLevel < MINOR_BREAK");
                    setPawnState(p, PawnState.JOY);
                }
                else if (sleeping || (doubleSleep && canSleep && (stateJoy || stateSleep || stateMeditate)))
                {
                    doLogging(p.Name.ToStringShort + ": " + "sleeping || (doubleSleep && canSleep && (stateJoy || stateSleep || stateMeditate))");
                    setPawnState(p, PawnState.SLEEP);
                }
                else if (gainingImmunity && immuneSensitivity == ImmuneSensitivity.BALANCED)
                {
                    doLogging(p.Name.ToStringShort + ": " + "gainingImmunity && immuneSensitivity == ImmuneSensitivity.BALANCED");
                    setPawnState(p, PawnState.ANYTHING);
                }
                else if (isAnimalSleepTime && (isHandler = isPawnHandler(p)))
                {
                    doLogging(p.Name.ToStringShort + ": " + "isAnimalSleepTime && (isHandler = isPawnHandler(p))");
                    if (canSleep)
                    {
                        setPawnState(p, PawnState.SLEEP);
                    }
                    else
                    {
                        setPawnState(p, PawnState.JOY);
                    }
                }
                else if (!isHandler && isDay && isPawnNightOwl(p))
                {
                    doLogging(p.Name.ToStringShort + ": " + "!isHandler && isDay && isPawnNightOwl(p)");
                    if (canSleep)
                    {
                        setPawnState(p, PawnState.SLEEP);
                    }
                    else
                    {
                        setPawnState(p, PawnState.JOY);
                    }
                }
                else if (pawnSchedule == ScheduleType.MAXMOOD && canSleep)
                {
                    doLogging(p.Name.ToStringShort + ": " + "curSchedule == ScheduleType.MAXMOOD && canSleep");
                    setPawnState(p, PawnState.SLEEP);
                }
                else if (pawnSchedule == ScheduleType.MAXMOOD)
                {
                    doLogging(p.Name.ToStringShort + ": " + "curSchedule == ScheduleType.MAXMOOD");
                    setPawnState(p, PawnState.JOY);
                }
                else if (doubleEat && p.needs.joy.CurLevel > 0.80f && p.needs.rest.CurLevel > 0.80f && shouldEatBeforeWork && hasFood)
                {
                    doLogging(p.Name.ToStringShort + ": " + "doubleEat && p.needs.joy.CurLevel > 0.80f && p.needs.rest.CurLevel > 0.80f && shouldEatBeforeWork && hasFood");
                    setPawnState(p, PawnState.ANYTHING);
                    FoodUtility.IngestFromInventoryNow(p, invFood);
                }
                else
                {
                    doLogging(p.Name.ToStringShort + ": " + "else [work]");
                    if (!stateWork && (p.CurJobDef.defName == "Meditate" || p.CurJobDef.defName == "Reign"))
                    {
                        setPawnState(p, PawnState.WORK);
                        tryToResetPawn(p);
                    }
                    else
                    {
                        setPawnState(p, PawnState.WORK);
                    }
                }

                if (layingDown && !sleeping && !needsTreatment)
                {
                    doLogging(p.Name.ToStringShort + ": " + "layingDown && !sleeping && !needsTreatment");
                    if (!(gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE))
                    {
                        if (pawnStates[p] == PawnState.JOY)
                        {
                            //tryToResetPawn(p);
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
