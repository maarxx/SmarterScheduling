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

        public const float MOOD_THRESH_LOW =  0.35F ;
        public const float MOOD_THRESH_HIGH = 0.80F ;

        public const float REST_THRESH_LOW =  0.28F ;
        public const float REST_THRESH_HIGH = 0.95F ;

        public const float JOY_THRESH_LOW =   0.28F ;

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
                if (curPawnArea != null && curPawnArea != psyche)
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

            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
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
