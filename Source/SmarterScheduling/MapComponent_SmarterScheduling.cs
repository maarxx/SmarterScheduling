using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SmarterScheduling;

internal class MapComponent_SmarterScheduling : MapComponent
{
    public enum ImmuneSensitivity
    {
        SENSITIVE,
        BALANCED,
        BRUTAL
    }

    public enum PawnState
    {
        SLEEP,
        JOY,
        WORK,
        MEDITATE,
        ANYTHING
    }

    public enum ScheduleType
    {
        WORK,
        MAXMOOD
    }

    public const string RECREATION_NAME = "Joy";
    public const string MEDIDATION_NAME = "Medi";

    public static JobGiver_OptimizeApparel apparelCheckerInstance;
    public static MethodInfo apparelCheckerMethod;
    private bool alreadyResetDoctorThisTick;
    private bool anyoneAwaitingTreatment;

    private bool anyoneNeedingTreatment;
    public Dictionary<Pawn, int> doctorResetTick;
    public bool doubleEat;

    public bool doubleSleep;

    public bool enabled;

    public bool enableLogging;
    private Pawn firstAwaiting;
    public ImmuneSensitivity immuneSensitivity;

    public bool joyHoldExtra;
    public Dictionary<Pawn, Area> lastPawnAreas;

    private Pawn laziestDoctor;

    public bool manageMeditation;
    public Area meditation;
    public Dictionary<Pawn, ScheduleType> pawnSchedules;

    public Dictionary<Pawn, PawnState> pawnStates;

    public Area recreation;
    public Dictionary<Pawn, bool> shouldResetPawnOnHungry;

    public int slowDown;
    public bool spoonFeeding;

    public MapComponent_SmarterScheduling(Map map) : base(map)
    {
        pawnStates = new Dictionary<Pawn, PawnState>();
        pawnSchedules = new Dictionary<Pawn, ScheduleType>();
        shouldResetPawnOnHungry = new Dictionary<Pawn, bool>();
        lastPawnAreas = new Dictionary<Pawn, Area>();
        doctorResetTick = new Dictionary<Pawn, int>();

        enabled = false;
        immuneSensitivity = ImmuneSensitivity.SENSITIVE;
        spoonFeeding = true;

        apparelCheckerInstance = new JobGiver_OptimizeApparel();
        apparelCheckerMethod = apparelCheckerInstance.GetType()
            .GetMethod("TryGiveJob", BindingFlags.NonPublic | BindingFlags.Instance);

        doubleSleep = false;
        doubleEat = false;

        manageMeditation = false;

        joyHoldExtra = false;

        enableLogging = false;

        slowDown = 0;
        //initPlayerAreas();
        //initPawnsIntoCollection();
        LongEventHandler.QueueLongEvent(ensureComponentExists, null, false, null);
    }

    public void doLogging(string s)
    {
        if (enableLogging)
        {
            Log.Message(s);
        }
    }

    public static void ensureComponentExists()
    {
        foreach (var m in Find.Maps)
        {
            if (m.GetComponent<MapComponent_SmarterScheduling>() == null)
            {
                m.components.Add(new MapComponent_SmarterScheduling(m));
            }
        }
    }

    public void initPlayerAreas(out Area savedArea, string areaName)
    {
        savedArea = null;
        foreach (var a in map.areaManager.AllAreas)
        {
            if (a.ToString() != areaName)
            {
                continue;
            }

            if (a.AssignableAsAllowed())
            {
                savedArea = a;
            }
            else
            {
                a.SetLabel($"{areaName}2");
            }
        }

        if (savedArea != null)
        {
            return;
        }

        map.areaManager.TryMakeNewAllowed(out var newArea);
        newArea.SetLabel(areaName);
        savedArea = newArea;
    }

    public void initPawnsIntoCollection()
    {
        foreach (var p in map.mapPawns.FreeColonistsSpawned)
        {
            if (!lastPawnAreas.ContainsKey(p))
            {
                lastPawnAreas.Add(p, null);
            }

            var curPawnArea = p.playerSettings.AreaRestriction;
            if (curPawnArea == null || curPawnArea != recreation && curPawnArea != meditation)
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
        foreach (var p in map.mapPawns.FreeColonistsAndPrisonersSpawned)
        {
            if (!HealthAIUtility.ShouldBeTendedNowByPlayer(p))
            {
                continue;
            }

            needing = true;
            if (!WorkGiver_Tend.GoodLayingStatusForTend(p, null) ||
                map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer))
            {
                continue;
            }

            Log.Message($"awaiting: {p.Name.ToStringShort}");
            awaiting = true;
            firstAwaiting = p;
            return;
        }

        var animals = from p in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
            where p.RaceProps.Animal
            select p;
        foreach (var p in animals)
        {
            if (!HealthAIUtility.ShouldBeTendedNowByPlayer(p))
            {
                continue;
            }

            needing = true;
            if (!WorkGiver_Tend.GoodLayingStatusForTend(p, null) ||
                map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer))
            {
                continue;
            }

            Log.Message($"awaiting: {p.Name.ToStringShort}");
            awaiting = true;
            firstAwaiting = p;
            return;
        }
    }

    public bool isPawnGainingImmunity(Pawn p)
    {
        foreach (var h in p.health.hediffSet.hediffs)
        {
            if (!h.Visible || h is not HediffWithComps hwc)
            {
                continue;
            }

            foreach (var hc in hwc.comps)
            {
                if (hc is not HediffComp_Immunizable hci)
                {
                    continue;
                }

                if (hci.Immunity is > 0 and < 1)
                {
                    return true;
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
        return doesPawnHaveWorkGiverAtPriority(p, new[] { "tend to,100", "tend to,90" }, 15);
    }

    public bool isPawnHandler(Pawn p)
    {
        return doesPawnHaveWorkGiverAtPriority(p, new[] { "tame,80", "train,70" }, 25);
    }

    public bool isPawnNightOwl(Pawn p)
    {
        foreach (var t in p.story.traits.allTraits)
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
        var i = 0;
        foreach (var wg in p.workSettings.WorkGiversInOrderNormal)
        {
            var workGiverString = $"{wg.def.verb},{wg.def.priorityInType}";
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
        doctorResetTick[p] = Find.TickManager.TicksGame;
    }

    public bool isThereAParty()
    {
        foreach (var l in map.lordManager.lords)
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
        if (l is { LordJob: { } } &&
            (l.LordJob is LordJob_Joinable_Party || l.LordJob is LordJob_Joinable_MarriageCeremony)
           )
        {
            return true;
        }

        return false;
    }

    public bool isAnimalSleepingTime()
    {
        var hour = GenLocalDate.HourOfDay(map.Tile);
        return hour >= 22 || hour <= 5;
    }

    public bool isDaytime()
    {
        var hour = GenLocalDate.HourOfDay(map.Tile);
        return hour is >= 11 and <= 18;
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
               && !p.Drafted
               && !p.CurJob.playerForced
               && !(dontDisruptEating && p.CurJob.def == JobDefOf.Ingest)
               && p.CurJob.def != JobDefOf.Wear
               && !(p.CurJob.def == JobDefOf.RemoveApparel && p.CurJob.targetA.Thing is Apparel { Wearer: { } });
    }

    public bool tryToResetPawn(Pawn p)
    {
        if (!shouldDisruptPawn(p))
        {
            return false;
        }

        p.jobs.StopAll();
        return true;
    }

    public void resetSelectedPawnsSchedules(PawnState state)
    {
        initPlayerAreas(out recreation, RECREATION_NAME);
        initPlayerAreas(out meditation, MEDIDATION_NAME);
        initPawnsIntoCollection();
        foreach (var obj in Find.Selector.SelectedObjects)
        {
            if (obj is not Pawn pawn)
            {
                continue;
            }

            setPawnState(pawn, state);
        }
    }

    public void resetSelectedPawnsScheduleTypes(ScheduleType type)
    {
        initPlayerAreas(out recreation, RECREATION_NAME);
        initPlayerAreas(out meditation, MEDIDATION_NAME);
        initPawnsIntoCollection();
        foreach (var obj in Find.Selector.SelectedObjects)
        {
            if (obj is not Pawn key)
            {
                continue;
            }

            pawnSchedules.SetOrAdd(key, type);
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

        for (var i = 0; i < 24; i++)
        {
            p.timetable.SetAssignment(i, newTad);
        }
    }

    public void restrictPawnToActivityArea(Pawn p, PawnState newState, bool forced = false)
    {
        if (!forced && !shouldDisruptPawn(p))
        {
            return;
        }

        p.playerSettings.AreaRestriction = newState == PawnState.MEDITATE ? meditation : recreation;
    }

    public void considerReleasingPawn(Pawn p)
    {
        p.playerSettings.AreaRestriction = lastPawnAreas[p];
    }

    public void updateDoctorResetTickCollection(Pawn p, bool isDoctor)
    {
        if (isDoctor)
        {
            if (!doctorResetTick.ContainsKey(p))
            {
                doctorResetTick.Add(p, 0);
            }
        }
        else
        {
            if (doctorResetTick.ContainsKey(p))
            {
                doctorResetTick.Remove(p);
            }
        }
    }

    public void doctorSubroutine(Pawn p, Pawn awaiting)
    {
        var currentlyTreating = p.CurJob.def.reportString == "tending to TargetA.";
        var isReserved = map.reservationManager.IsReservedByAnyoneOf(p, Faction.OfPlayer);
        if (currentlyTreating || isReserved || p == firstAwaiting)
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
                    Log.Message(
                        $"Was about to reset ({p.Name.ToStringShort}) for Doctor, but couldn't reach ({awaiting.Name.ToStringShort})");
                    doctorNotLazy(p);
                }
                else
                {
                    setPawnState(p, PawnState.WORK);

                    Log.Message($"Resetting ({p.Name.ToStringShort}) for Doctor, current job: {p.CurJob}");
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

        slowDown = 0;

        initPlayerAreas(out recreation, RECREATION_NAME);
        initPlayerAreas(out meditation, MEDIDATION_NAME);
        initPawnsIntoCollection();

        var party = isThereAParty();

        var isAnimalSleepTime = isAnimalSleepingTime();

        var isDay = isDaytime();

        isAnyPendingTreatments(out anyoneNeedingTreatment, out anyoneAwaitingTreatment, out firstAwaiting);

        if (anyoneNeedingTreatment)
        {
            alreadyResetDoctorThisTick = false;

            if (doctorResetTick.Count > 0)
            {
                laziestDoctor = doctorResetTick.MinBy(kvp => kvp.Value).Key;
            }
        }

        var pawns = map.mapPawns.FreeColonistsSpawned.ListFullCopy();
        foreach (var p in pawns)
        {
            var EXTREME_BREAK = p.mindState.mentalBreaker.BreakThresholdExtreme + 0.02f;
            var MAJOR_BREAK = p.mindState.mentalBreaker.BreakThresholdMajor + 0.02f;
            var MINOR_BREAK = p.mindState.mentalBreaker.BreakThresholdMinor + 0.02f;

            var layingDown = p.CurJob.def.reportString == "lying down.";
            var sleeping = p.needs.rest.GUIChangeArrow > 0;
            var justWokeRested = !sleeping && p.needs.rest.CurLevel > 0.95f;

            var hungry = p.needs.food.CurLevel < 0.31f;
            if (!hungry)
            {
                shouldResetPawnOnHungry[p] = true;
            }

            var shouldEatBeforeWork = p.needs.food.CurLevel < 0.70f;
            var invFood = FoodUtility.BestFoodInInventory(p);
            var hasFood = invFood != null;

            var rest = p.needs.rest.CurLevel;
            var joy = p.needs.rest.CurLevel;
            var mood = p.needs.mood.CurLevel;

            var changeClothesJob = apparelCheckerMethod.Invoke(apparelCheckerInstance, new object[] { p });
            var shouldChangeClothes = changeClothesJob != null;

            var canSleep = rest < 0.74f;

            var pawnSchedule = pawnSchedules.TryGetValue(p);

            var stateSleep = pawnStates[p] == PawnState.SLEEP;
            var stateJoy = pawnStates[p] == PawnState.JOY;
            var stateWork = pawnStates[p] == PawnState.WORK;
            var stateMeditate = pawnStates[p] == PawnState.MEDITATE;

            var shouldMeditate = p.HasPsylink && p.psychicEntropy.CurrentPsyfocus < p.psychicEntropy.TargetPsyfocus;

            var gainingImmunity = isPawnGainingImmunity(p);

            var isHandler = false;

            var recreationExists = recreation.TrueCount > 0;

            var currentlyTreating = false;
            var needsTreatment = false;
            var isDoctor = false;
            if (anyoneNeedingTreatment)
            {
                needsTreatment = HealthAIUtility.ShouldBeTendedNowByPlayer(p);
                isDoctor = isPawnDoctor(p);
                currentlyTreating = p.CurJob.def.reportString == "tending to TargetA.";
                updateDoctorResetTickCollection(p, isDoctor);
            }

            if (gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE");
                setPawnState(p, PawnState.ANYTHING);
                if (anyoneNeedingTreatment && isDoctor)
                {
                    doctorNotLazy(p);
                }
            }
            else if (hungry && !sleeping && !needsTreatment && !shouldChangeClothes)
            {
                doLogging($"{p.Name.ToStringShort}: hungry && !sleeping && !needsTreatment");
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

                if (anyoneNeedingTreatment && isDoctor)
                {
                    doctorNotLazy(p);
                }
            }
            else if (currentlyTreating)
            {
                doLogging($"{p.Name.ToStringShort}: currentlyTreating");
                setPawnState(p, PawnState.ANYTHING);
                doctorNotLazy(p);
            }
            else if (anyoneAwaitingTreatment && isDoctor)
            {
                doLogging($"{p.Name.ToStringShort}: anyoneAwaitingTreatment && isDoctor");
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
                doLogging($"{p.Name.ToStringShort}: party");
                setPawnState(p, PawnState.ANYTHING);
                if (!pawnAttendingParty(p))
                {
                    if (needsTreatment || gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE)
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
                doLogging($"{p.Name.ToStringShort}: shouldChangeClothes");
                setPawnState(p, PawnState.ANYTHING);
            }
            else if (rest < 0.32f)
            {
                doLogging($"{p.Name.ToStringShort}: rest < 0.32f");
                setPawnState(p, PawnState.SLEEP);
                //if (!(layingDown || sleeping)) { tryToResetPawn(p); }
                if (p.CurJobDef == JobDefOf.Meditate)
                {
                    tryToResetPawn(p);
                }
            }
            else if ((sleeping || stateSleep) && rest < 0.45f)
            {
                doLogging($"{p.Name.ToStringShort}: (sleeping || stateSleep) && rest < 0.45f");
                setPawnState(p, PawnState.SLEEP);
            }
            else if (mood < MAJOR_BREAK)
            {
                doLogging($"{p.Name.ToStringShort}: mood < MAJOR_BREAK");
                setPawnState(p, PawnState.JOY);
                if (recreationExists && !recreation[p.Position])
                {
                    tryToResetPawn(p);
                }
            }
            else if (needsTreatment)
            {
                doLogging($"{p.Name.ToStringShort}: needsTreatment");
                setPawnState(p, PawnState.ANYTHING);
            }
            else if ((sleeping || stateSleep) && rest > 0.45f && mood < MINOR_BREAK)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: (sleeping || stateSleep) && rest > 0.45f && mood < MINOR_BREAK");
                setPawnState(p, PawnState.JOY);
            }
            else if (p.needs.joy.CurLevel < 0.30f)
            {
                doLogging($"{p.Name.ToStringShort}: p.needs.joy.CurLevel < 0.30f");
                setPawnState(p, PawnState.JOY);
            }
            else if (pawnSchedule == ScheduleType.MAXMOOD && p.needs.mood.CurLevel < MINOR_BREAK)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: curSchedule == ScheduleType.MAXMOOD && p.needs.mood.CurLevel < MINOR_BREAK");
                setPawnState(p, PawnState.JOY);
            }
            else if (justWokeRested)
            {
                doLogging($"{p.Name.ToStringShort}: justWokeRested");
                setPawnState(p, PawnState.JOY);
            }
            else if (stateJoy && p.needs.joy.CurLevel < 0.80f)
            {
                doLogging($"{p.Name.ToStringShort}: stateJoy && p.needs.joy.CurLevel < 0.80f");
                setPawnState(p, PawnState.JOY);
            }
            else if (stateJoy && p.needs.joy.GUIChangeArrow > 0 && p.needs.joy.CurLevel < 1f)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: stateJoy && p.needs.joy.GUIChangeArrow > 0 && p.needs.joy.CurLevel < 1f");
                setPawnState(p, PawnState.JOY);
            }
            else if (manageMeditation && shouldMeditate)
            {
                doLogging($"{p.Name.ToStringShort}: manageMeditation && shouldMeditate");
                setPawnState(p, PawnState.MEDITATE);
            }
            else if ((stateJoy || stateMeditate) && joyHoldExtra && p.needs.beauty.GUIChangeArrow > 0)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: (stateJoy || stateMeditate) && joyHoldExtra && p.needs.beauty.GUIChangeArrow > 0");
                setPawnState(p, PawnState.JOY);
            }
            else if ((stateJoy || stateMeditate) && joyHoldExtra && p.needs.comfort.GUIChangeArrow > 0)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: (stateJoy || stateMeditate) && joyHoldExtra && p.needs.comfort.GUIChangeArrow > 0");
                setPawnState(p, PawnState.JOY);
            }
            else if ((stateJoy || stateMeditate) && p.needs.mood.GUIChangeArrow > 0)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: (stateJoy || stateMeditate) && p.needs.mood.GUIChangeArrow > 0");
                setPawnState(p, PawnState.JOY);
            }
            else if ((stateJoy || stateMeditate) && p.needs.mood.CurLevel < MINOR_BREAK)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: (stateJoy || stateMeditate) && p.needs.mood.CurLevel < MINOR_BREAK");
                setPawnState(p, PawnState.JOY);
            }
            else if (sleeping || doubleSleep && canSleep && (stateJoy || stateSleep || stateMeditate))
            {
                doLogging(
                    $"{p.Name.ToStringShort}: sleeping || (doubleSleep && canSleep && (stateJoy || stateSleep || stateMeditate))");
                setPawnState(p, PawnState.SLEEP);
            }
            else if (gainingImmunity && immuneSensitivity == ImmuneSensitivity.BALANCED)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: gainingImmunity && immuneSensitivity == ImmuneSensitivity.BALANCED");
                setPawnState(p, PawnState.ANYTHING);
            }
            else if (isAnimalSleepTime && (isHandler = isPawnHandler(p)))
            {
                doLogging($"{p.Name.ToStringShort}: isAnimalSleepTime && (isHandler = isPawnHandler(p))");
                setPawnState(p, canSleep ? PawnState.SLEEP : PawnState.JOY);
            }
            else if (!isHandler && isDay && isPawnNightOwl(p))
            {
                doLogging($"{p.Name.ToStringShort}: !isHandler && isDay && isPawnNightOwl(p)");
                setPawnState(p, canSleep ? PawnState.SLEEP : PawnState.JOY);
            }
            else if (pawnSchedule == ScheduleType.MAXMOOD && canSleep)
            {
                doLogging($"{p.Name.ToStringShort}: curSchedule == ScheduleType.MAXMOOD && canSleep");
                setPawnState(p, PawnState.SLEEP);
            }
            else if (pawnSchedule == ScheduleType.MAXMOOD)
            {
                doLogging($"{p.Name.ToStringShort}: curSchedule == ScheduleType.MAXMOOD");
                setPawnState(p, PawnState.JOY);
            }
            else if (doubleEat && p.needs.joy.CurLevel > 0.80f && p.needs.rest.CurLevel > 0.80f &&
                     shouldEatBeforeWork && hasFood)
            {
                doLogging(
                    $"{p.Name.ToStringShort}: doubleEat && p.needs.joy.CurLevel > 0.80f && p.needs.rest.CurLevel > 0.80f && shouldEatBeforeWork && hasFood");
                setPawnState(p, PawnState.ANYTHING);
                FoodUtility.IngestFromInventoryNow(p, invFood);
            }
            else
            {
                doLogging($"{p.Name.ToStringShort}: else [work]");
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

            if (!layingDown || sleeping || needsTreatment)
            {
                continue;
            }

            doLogging($"{p.Name.ToStringShort}: layingDown && !sleeping && !needsTreatment");
            if (gainingImmunity && immuneSensitivity == ImmuneSensitivity.SENSITIVE)
            {
                continue;
            }

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