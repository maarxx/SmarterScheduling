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




        public static void blockPawnSchedule(Pawn p, TimeAssignmentDef tad)
        {
            for (int i = 0; i < 24; i++)
            {
                p.timetable.SetAssignment(i, tad);
            }
        }

        //public override void MapComponentTick()
        public void old_MapComponentTick()
        {
            //base.MapComponentTick();

            // This method ticks REALLY fast, no need for this fast. SLOW DOWN by 100.
            this.slowDown++;
            if (this.slowDown < 100)
            {
                return;
            }
            else
            {
                this.slowDown = 0;
            }

            Log.Message("Hello, slow!");

            // Find the Psyche area.
            foreach (Area a in map.areaManager.AllAreas)
            {
                if (a.ToString() == PSYCHE_NAME)
                {
                    this.psyche = a;
                    break;
                }
                this.psyche = null;
            }

            //String pawnName;
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                //pawnName = p.NameStringShort;

                //p.jobs.StopAll();
                // Okay that did exactly as expected, we'll need that later.

                /*
                //Print the Pawn's current Area (debug / testing).
                if (p.playerSettings.AreaRestriction != null)
                {
                    Log.Message(p.NameStringShort + " , " + p.playerSettings.AreaRestriction.ToString());
                }
                else
                {
                    Log.Message(p.NameStringShort + " , null area");
                }
                */

                // Make note of all Pawn's previously assigned Areas, to restore them later.
                Area curArea = p.playerSettings.AreaRestriction;
                if (curArea == null || curArea != this.psyche)
                {
                    //Log.Message("Saved Area: " + curArea.ToString() + " for Pawn: " + pawnName);
                    //lastAreas.Add(p, curArea);
                    lastAreas[p] = curArea;
                }

                // If overall Mood is tanked
                if (p.needs.mood.CurLevel < MOOD_THRESH)
                {
                    //Log.Message("Pawn :" + pawnName + " is critical Mood.");
                    if (this.psyche != null)
                    {
                        //Log.Message("Pawn: " + pawnName + " restricted to " + PSYCHE_NAME);
                        p.playerSettings.AreaRestriction = this.psyche;
                    }
                }
                // Mood isn't tanked
                else
                {
                    //Log.Message("Pawn :" + pawnName + " is non-critical Mood.");
                    // but might just be recovering
                    if (p.needs.mood.GUIChangeArrow > 0)
                    {
                        // Do nothing.
                    }
                    // Mood isn't tanked, Mood isn't recovering
                    else
                    {
                        // If in Psyche, release from Psyche
                        if (p.playerSettings.AreaRestriction == this.psyche)
                        {
                            Area result;
                            if (lastAreas.TryGetValue(p, out result))
                            {
                                p.playerSettings.AreaRestriction = result;
                            }
                            else
                            {
                                p.playerSettings.AreaRestriction = null;
                            }
                        }
                    }
                }

                // Rest or Joy is improving already
                if (p.needs.rest.GUIChangeArrow > 0 || p.needs.joy.GUIChangeArrow > 0)
                {
                    // Keep doing whatever the hell you are doing, buddy
                    blockPawnSchedule(p, TimeAssignmentDefOf.Anything);
                }
                // Rest or Joy isn't improving already
                else
                {
                    // If Rest is below threshold
                    if (p.needs.rest.CurLevel < REST_THRESH)// || p.CurJob.def.reportString == "lying down.")
                    {
                        // go to Sleep
                        blockPawnSchedule(p, TimeAssignmentDefOf.Sleep);
                    }
                    else
                    {
                        // If Mood is 
                        if (p.needs.mood.CurLevel < MOOD_THRESH)
                        {
                            blockPawnSchedule(p, TimeAssignmentDefOf.Joy);
                        }
                        else
                        {
                            if (p.needs.joy.CurLevel < JOY_THRESH)
                            {
                                blockPawnSchedule(p, TimeAssignmentDefOf.Joy);
                            }
                            else
                            {
                                blockPawnSchedule(p, TimeAssignmentDefOf.Work);
                            }
                        }
                    }
                }
            }
        }
    }
}
