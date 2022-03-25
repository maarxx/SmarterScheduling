using RimWorld;
using Verse;

namespace SmarterScheduling;

internal class Alert_SwitchedOff : Alert_Critical
{
    public Alert_SwitchedOff()
    {
        defaultLabel = "SmarterScheduling Disabled!";
        defaultExplanation =
            "You have SmarterScheduling mod installed, but it is currently switched off. This was probably temporary. Don't forget to turn it back on!";
    }

    public override AlertReport GetReport()
    {
        if (!Find.CurrentMap.GetComponent<MapComponent_SmarterScheduling>().enabled)
        {
            return true;
        }

        return false;
    }
}