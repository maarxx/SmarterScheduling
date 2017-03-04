using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace SmarterScheduling
{
    class MapComponent_Brain : MapComponent
    {

        public enum PawnState
        {
            SLEEP,
            JOY,
            WORK
        }

        public Dictionary<Pawn, PawnState> pawnStates;

        // Now defined per-pawn, please CTRL+F search for the same variable name.
        // Can be found further down in this same file.

        //public const float MOOD_THRESH_LOW  = 0.25F ;
        //public const float MOOD_THRESH_HIGH = 0.52F ;

        public const float REST_THRESH_LOW  = 0.35F ;
        public const float REST_THRESH_HIGH = 0.90F ;

        public const float JOY_THRESH_LOW   = 0.28F ;
        public const float JOY_THRESH_HIGH  = 0.90F ;

        public const string PSYCHE_NAME = "Psyche";

        public Area psyche;
        public Dictionary<Pawn, Area> lastPawnAreas;

        int slowDown = 0;

        public MapComponent_Brain(Map map) : base(map)
        {
            pawnStates = new Dictionary<Pawn, PawnState>();
            lastPawnAreas = new Dictionary<Pawn, Area>();
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
                    pawnStates.Add(p, PawnState.WORK);
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
            else
            {
                newTad = TimeAssignmentDefOf.Work;
            }

            for (int i = 0; i < 24; i++)
            {
                p.timetable.SetAssignment(i, newTad);
            }

            if (state == PawnState.JOY)
            {
                p.playerSettings.AreaRestriction = psyche;
            }
            else
            {
                p.playerSettings.AreaRestriction = lastPawnAreas[p];
            }
        }

        /*
        public PawnState getPawnState(Pawn p)
        {
            return pawnStates[p];
        }
        */

        public bool doesAnyoneNeedTreatment()
        {
            foreach (Pawn p in map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                Log.Message("PAWN: " + p.NameStringShort);
                if (p.health.HasHediffsNeedingTendByColony())
                {
                    return true;
                }
            }
            return false;
        }

        // If "treatment" is anywhere in your top 15 WorkGivers, you're a doctor.
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

            bool anyoneNeedTreatment = doesAnyoneNeedTreatment();

            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                float MOOD_THRESH_LOW = p.mindState.mentalBreaker.BreakThresholdMajor + 0.02F;
                float MOOD_THRESH_HIGH = p.mindState.mentalBreaker.BreakThresholdMinor + 0.08F;

                bool areDoctor = isPawnDoctor(p);
                if (anyoneNeedTreatment && areDoctor)
                {
                    setPawnState(p, PawnState.WORK);
                    continue;
                }

                if (p.needs.rest.CurLevel < REST_THRESH_LOW)
                {
                    setPawnState(p, PawnState.SLEEP);
                    continue;
                }
                else if (p.needs.rest.GUIChangeArrow > 0)
                {
                    setPawnState(p, PawnState.SLEEP);
                    continue;
                }
                else if (pawnStates[p] == PawnState.SLEEP && !(p.needs.rest.GUIChangeArrow > 0) && p.needs.rest.CurLevel > REST_THRESH_HIGH)
                {
                    setPawnState(p, PawnState.JOY);
                    continue;
                }
                else if (pawnStates[p] == PawnState.JOY && p.needs.mood.CurLevel < MOOD_THRESH_HIGH)
                {
                    //setPawnState(p, PawnState.JOY);
                    continue;
                }
                else if (pawnStates[p] == PawnState.JOY && p.needs.joy.CurLevel < JOY_THRESH_HIGH)
                {
                    //setPawnState(p, PawnState.JOY);
                    continue;
                }
                else if (pawnStates[p] == PawnState.JOY && p.needs.mood.GUIChangeArrow > 0)
                {
                    //setPawnState(p, PawnState.JOY);
                    continue;
                }
                else if (p.needs.mood.CurLevel < MOOD_THRESH_LOW)
                {
                    setPawnState(p, PawnState.JOY);
                    continue;
                }
                else if (p.needs.joy.CurLevel < JOY_THRESH_LOW)
                {
                    setPawnState(p, PawnState.JOY);
                    continue;
                }
                else
                {
                    setPawnState(p, PawnState.WORK);
                    continue;
                }
            }
        }

    }
}
