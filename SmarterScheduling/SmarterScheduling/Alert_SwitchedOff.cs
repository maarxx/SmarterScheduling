using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmarterScheduling
{
    class Alert_SwitchedOff : Alert_Critical
    {
        public Alert_SwitchedOff()
        {
            this.defaultLabel = "SmarterScheduling Disabled!";
            this.defaultExplanation = "You have SmarterScheduling mod installed, but it is currently switched off. This was probably temporary. Don't forget to turn it back on!";
        }
        public override AlertReport GetReport()
        {
            if (!Find.CurrentMap.GetComponent<MapComponent_SmarterScheduling>().enabled)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
