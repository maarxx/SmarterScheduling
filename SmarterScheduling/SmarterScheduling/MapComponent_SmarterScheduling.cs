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

        public const float REST_THRESH_CRITICAL   = 0.05F ;

        public const float HUNGER_THRESH_CRITICAL = 0.25F ;

        public const float REST_THRESH_LOW        = 0.35F ;
        public const float REST_THRESH_HIGH       = 0.90F ;

        public const float JOY_THRESH_LOW         = 0.28F ;
        public const float JOY_THRESH_HIGH        = 0.90F ;

        public const string PSYCHE_NAME = "Psyche";
        public const string TOXIC_NAME_H = "ToxicH";
        public const string TOXIC_NAME_A = "ToxicA";

        public Dictionary<Pawn, PawnState> pawnStates;
        public Dictionary<Pawn, Area> lastPawnAreas;
        public Dictionary<Pawn, int> doctorResetTick;
        public Dictionary<Pawn, bool> toxicBounces;
        public Faction playerFaction;
        public Area psyche;
        public Area humanToxic;
        public Area animalToxic;

        public int slowDown;

        public bool enabled;

        public bool toxicFallout;
        public bool toxicLatch;

        public MapComponent_SmarterScheduling(Map map) : base(map)
        {
            this.pawnStates = new Dictionary<Pawn, PawnState>();
            this.lastPawnAreas = new Dictionary<Pawn, Area>();
            this.doctorResetTick = new Dictionary<Pawn, int>();
            this.toxicBounces = new Dictionary<Pawn, bool>();
            this.playerFaction = getPlayerFaction();
            this.enabled = false;
            this.toxicFallout = false;
            this.toxicLatch = false;
            this.slowDown = 0;
            //initPlayerAreas();
            //initPawnsIntoCollection();
        }

        public void initPlayerAreas()
        {
            this.psyche = null;
            this.humanToxic = null;
            this.animalToxic = null;
            foreach (Area a in map.areaManager.AllAreas)
            {
                if (a.ToString() == PSYCHE_NAME)
                {
                    if (a.AssignableAsAllowed(AllowedAreaMode.Humanlike))
                    {
                        this.psyche = a;
                    }
                    else
                    {
                        a.SetLabel(PSYCHE_NAME + "2");
                    }
                }
                else if (a.ToString() == TOXIC_NAME_H)
                {
                    if (a.AssignableAsAllowed(AllowedAreaMode.Humanlike))
                    {
                        this.humanToxic = a;
                    }
                    else
                    {
                        a.SetLabel(TOXIC_NAME_H + "2");
                    }
                }
                else if (a.ToString() == TOXIC_NAME_A)
                {
                    if (a.AssignableAsAllowed(AllowedAreaMode.Animal))
                    {
                        this.animalToxic = a;
                    }
                    else
                    {
                        a.SetLabel(TOXIC_NAME_A + "2");
                    }
                }
            }
            if (this.psyche == null)
            {
                Area_Allowed newPsyche;// = new Area_Allowed(map.areaManager, AllowedAreaMode.Humanlike, PSYCHE_NAME);
                map.areaManager.TryMakeNewAllowed(AllowedAreaMode.Humanlike, out newPsyche);
                newPsyche.SetLabel(PSYCHE_NAME);
                this.psyche = newPsyche;
            }
            if (this.humanToxic == null)
            {
                Area_Allowed newHumanToxic;// = new Area_Allowed(map.areaManager, AllowedAreaMode.Humanlike, TOXIC_NAME_H);
                map.areaManager.TryMakeNewAllowed(AllowedAreaMode.Humanlike, out newHumanToxic);
                newHumanToxic.SetLabel(TOXIC_NAME_H);
                this.humanToxic = newHumanToxic;
            }
            if (this.animalToxic == null)
            {
                Area_Allowed newAnimalToxic;// = new Area_Allowed(map.areaManager, AllowedAreaMode.Animal, TOXIC_NAME_A);
                map.areaManager.TryMakeNewAllowed(AllowedAreaMode.Animal, out newAnimalToxic);
                newAnimalToxic.SetLabel(TOXIC_NAME_A);
                this.animalToxic = newAnimalToxic;
            }
        }

        public void initPawnsIntoCollection()
        {
            foreach (Pawn p in map.mapPawns.PawnsInFaction(this.playerFaction))
            {
                if (!lastPawnAreas.ContainsKey(p))
                {
                    lastPawnAreas.Add(p, null);
                }
                if (!toxicBounces.ContainsKey(p))
                {
                    toxicBounces.Add(p, false);
                }
                Area curPawnArea = p.playerSettings.AreaRestriction;
                if (curPawnArea == null
                    || (curPawnArea != psyche
                        && curPawnArea != humanToxic
                        && curPawnArea != animalToxic
                        )
                    )
                {
                    lastPawnAreas[p] = curPawnArea;
                }
            }
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (!pawnStates.ContainsKey(p))
                {
                    pawnStates.Add(p, PawnState.ANYTHING);
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
                if (p.health.HasHediffsNeedingTendByColony() && p.playerSettings.medCare > 0)
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
                    && p.playerSettings.medCare > 0
                    && p.CurJob.def.reportString == "lying down."
                    && p.CurJob.targetA.Thing != null
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

        public bool isPawnAnimal(Pawn p)
        {
            if (p.needs.joy == null)
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

        public bool isToxicFallout()
        {
            return map.mapConditionManager.ConditionIsActive(MapConditionDefOf.ToxicFallout);
        }

        /*
        public bool isAnyOtherGenericEmergency()
        {
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (   p.needs.food.CurLevel == 0
                       && (   !p.health.capacities.CanBeAwake
                           || !(p.health.capacities.GetEfficiency(PawnCapacityDefOf.Moving) > 0)
                           || p.health.InPainShock
                           )
                    )
                {
                    return true;
                }
            }
            return false;
        }
        */

        public bool tryToResetPawn(Pawn p)
        {
            if (   p.health.capacities.CanBeAwake
                && p.health.capacities.GetEfficiency(PawnCapacityDefOf.Moving) > 0
                && !p.health.InPainShock
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

        /*
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
        */

        /*
        public void restrictPawn(Pawn p, Area a)
        {

        }
        */

        public void restrictPawnToPsyche(Pawn p)
        {
            p.playerSettings.AreaRestriction = this.psyche;
        }

        public void considerReleasingPawn(Pawn p)
        {
            if (this.toxicFallout)
            {
                foreach (Hediff h in p.health.hediffSet.hediffs)
                {
                    if (h.def.Equals(HediffDefOf.ToxicBuildup)) {
                        if (h.Severity < 0.25F)
                        {
                            this.toxicBounces[p] = false;
                            p.playerSettings.AreaRestriction = this.lastPawnAreas[p];
                            return;
                        }
                        else if (h.Severity > 0.35F || this.toxicBounces[p])
                        {
                            this.toxicBounces[p] = true;
                            if (isPawnAnimal(p))
                            {
                                p.playerSettings.AreaRestriction = this.animalToxic;
                                return;
                            }
                            else
                            {
                                p.playerSettings.AreaRestriction = this.humanToxic;
                                return;
                            }
                        }
                    }
                }
                this.toxicBounces[p] = false;
                p.playerSettings.AreaRestriction = this.lastPawnAreas[p];
                return;
            }
            else
            {
                p.playerSettings.AreaRestriction = this.lastPawnAreas[p];
            }
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

                bool anyoneNeedingTreatment = isAnyoneNeedingTreatment();
                bool anyoneAwaitingTreatment = false;
                bool alreadyResetDoctorThisTick = false;
                Pawn oldestDoctor = null;
                if (anyoneNeedingTreatment)
                {
                    anyoneAwaitingTreatment = isAnyoneAwaitingTreatment();
                    if (this.doctorResetTick.Count > 0)
                    {
                        oldestDoctor = this.doctorResetTick.MinBy(kvp => kvp.Value).Key;
                    }
                }

                this.toxicFallout = isToxicFallout();
                if (this.toxicFallout)
                {
                    this.toxicLatch = true;
                }

                //bool anyOtherGenericEmergency = isAnyOtherGenericEmergency();

                bool party = isThereAParty();

                if (this.toxicFallout || this.toxicLatch)
                {
                    foreach (Pawn p in map.mapPawns.PawnsInFaction(this.playerFaction))
                    {
                        if (isPawnAnimal(p))
                        {
                            if (toxicFallout)
                            {
                                considerReleasingPawn(p);
                            }
                            else
                            {
                                p.playerSettings.AreaRestriction = this.lastPawnAreas[p];
                            }
                        }
                    }
                    if (this.toxicFallout == false)
                    {
                        this.toxicLatch = false;
                    }
                }

                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    float MOOD_THRESH_CRITICAL = p.mindState.mentalBreaker.BreakThresholdExtreme + 0.02F;
                    float MOOD_THRESH_LOW = p.mindState.mentalBreaker.BreakThresholdMajor + 0.02F;
                    float MOOD_THRESH_HIGH = p.mindState.mentalBreaker.BreakThresholdMinor + 0.08F;

                    bool gainingImmunity = isPawnGainingImmunity(p);

                    bool isDoctor = false;
                    if (anyoneNeedingTreatment)
                    {
                        isDoctor = isPawnDoctor(p);
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

                    /*
                    if (anyOtherGenericEmergency)
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                    }
                    else
                    */
                    if (gainingImmunity)
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                    }
                    else if (p.needs.rest.CurLevel < REST_THRESH_CRITICAL)
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                    }
                    else if (p.needs.food.CurLevel < HUNGER_THRESH_CRITICAL && !(p.needs.rest.GUIChangeArrow > 0))
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                    }
                    else if (p.needs.mood.CurLevel < MOOD_THRESH_CRITICAL)
                    {
                        if (p.needs.rest.CurLevel < REST_THRESH_LOW)
                        {
                            setPawnState(p, PawnState.SLEEP);
                            considerReleasingPawn(p);
                        }
                        else if (p.needs.rest.GUIChangeArrow > 0)
                        {
                            setPawnState(p, PawnState.SLEEP);
                            considerReleasingPawn(p);
                        }
                        else
                        {
                            setPawnState(p, PawnState.JOY);
                            restrictPawnToPsyche(p);
                        }
                    }
                    else if (anyoneNeedingTreatment && isDoctor)
                    {
                        if (anyoneAwaitingTreatment)
                        {
                            if (!isPawnCurrentlyTreating(p))
                            {
                                if (!alreadyResetDoctorThisTick && p.Equals(oldestDoctor))
                                {
                                    setPawnState(p, PawnState.WORK);
                                    considerReleasingPawn(p);
                                    this.doctorResetTick[p] = Find.TickManager.TicksGame;
                                    if (tryToResetPawn(p))
                                    {
                                        alreadyResetDoctorThisTick = true;
                                    }
                                    else
                                    {
                                        oldestDoctor = this.doctorResetTick.MinBy(kvp => kvp.Value).Key;
                                    }
                                }
                                else
                                {
                                    setPawnState(p, PawnState.ANYTHING);
                                    considerReleasingPawn(p);
                                }
                            }
                            else
                            {
                                setPawnState(p, PawnState.ANYTHING);
                                considerReleasingPawn(p);
                            }
                        }
                        else
                        {
                            setPawnState(p, PawnState.ANYTHING);
                            considerReleasingPawn(p);
                        }
                    }
                    else if (party)
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                        if (
                            p.CurJob.def.reportString == "lying down."
                            && !p.health.HasHediffsNeedingTendByColony()
                            )
                        {
                            tryToResetPawn(p);
                        }
                    }
                    else if (p.needs.rest.CurLevel < REST_THRESH_LOW)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        considerReleasingPawn(p);
                    }
                    else if (p.needs.rest.GUIChangeArrow > 0)
                    {
                        setPawnState(p, PawnState.SLEEP);
                        considerReleasingPawn(p);
                    }
                    else if (anyoneNeedingTreatment && p.health.HasHediffsNeedingTendByColony())
                    {
                        setPawnState(p, PawnState.ANYTHING);
                        considerReleasingPawn(p);
                    }
                    //else if (pawnStates[p] == PawnState.SLEEP && !(p.needs.rest.GUIChangeArrow > 0) && p.needs.rest.CurLevel > REST_THRESH_HIGH)
                    else if (p.needs.rest.CurLevel > REST_THRESH_HIGH && !(p.needs.rest.GUIChangeArrow > 0))
                    {
                        setPawnState(p, PawnState.JOY);
                        restrictPawnToPsyche(p);
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
                    else
                    {
                        setPawnState(p, PawnState.WORK);
                        considerReleasingPawn(p);
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
}
