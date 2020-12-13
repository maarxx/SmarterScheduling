using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using static SmarterScheduling.MapComponent_SmarterScheduling;

namespace SmarterScheduling
{
    class Alert_PawnsMaxMood : Alert_Critical
    {
        public Alert_PawnsMaxMood()
        {
            this.defaultLabel = "Maxmood Enabled!";
            this.defaultExplanation = "You have some pawns set for MaxMood instead of Work. " +
                "They're only going to sleep and keep mood maximized, instead of working. " +
                "This is fine for short periods, but you probably want to send them back to work soon.";
        }
        public override AlertReport GetReport()
        {
            Dictionary<Pawn, ScheduleType> schedules = Find.CurrentMap.GetComponent<MapComponent_SmarterScheduling>().pawnSchedules;
            List<Pawn> affectedPawns = new List<Pawn>();
            foreach (KeyValuePair<Pawn, ScheduleType> kv in schedules)
            {
                if (kv.Value == ScheduleType.MAXMOOD)
                {
                    affectedPawns.Add(kv.Key);
                }
            }
            if (affectedPawns.Count > 0)
            {
                return AlertReport.CulpritsAre(affectedPawns);
            }
            return false;
        }
    }
}
