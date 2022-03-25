using System;
using System.Collections.Generic;
using ModButtons;
using Verse;

namespace SmarterScheduling;

internal class SmarterScheduling_RegisterToMainTab
{
    public static bool wasRegistered;

    private static MapComponent_SmarterScheduling getComponent()
    {
        return Find.CurrentMap.GetComponent<MapComponent_SmarterScheduling>();
    }

    public static void ensureMainTabRegistered()
    {
        if (wasRegistered)
        {
            return;
        }

        Log.Message("Hello from SmarterScheduling_RegisterToMainTab ensureMainTabRegistered");

        var columns = MainTabWindow_ModButtons.columns;

        var menuImmuneSensitivty = new List<FloatMenuOption>();
        foreach (MapComponent_SmarterScheduling.ImmuneSensitivity immSen in Enum.GetValues(
                     typeof(MapComponent_SmarterScheduling.ImmuneSensitivity)))
        {
            menuImmuneSensitivty.Add(new FloatMenuOption(immSen.ToString().ToLower().CapitalizeFirst(),
                delegate { getComponent().immuneSensitivity = immSen; }));
        }

        var menuResetAllSchedules = new List<FloatMenuOption>();
        foreach (MapComponent_SmarterScheduling.PawnState pawnState in Enum.GetValues(
                     typeof(MapComponent_SmarterScheduling.PawnState)))
        {
            menuResetAllSchedules.Add(new FloatMenuOption(pawnState.ToString().ToLower().CapitalizeFirst(),
                delegate { getComponent().resetSelectedPawnsSchedules(pawnState); }));
        }

        var menuResetAllScheduleTypes = new List<FloatMenuOption>();
        foreach (MapComponent_SmarterScheduling.ScheduleType scheduleType in Enum.GetValues(
                     typeof(MapComponent_SmarterScheduling.ScheduleType)))
        {
            menuResetAllScheduleTypes.Add(new FloatMenuOption(scheduleType.ToString().ToLower().CapitalizeFirst(),
                delegate { getComponent().resetSelectedPawnsScheduleTypes(scheduleType); }));
        }

        var buttons = new List<ModButton_Text>
        {
            new ModButton_Text(
                delegate
                {
                    var buttonLabel = $"Entire Mod is Currently:{Environment.NewLine}";
                    if (getComponent().enabled)
                    {
                        buttonLabel += "ENABLED";
                    }
                    else
                    {
                        buttonLabel += "DISABLED";
                    }

                    return buttonLabel;
                },
                delegate { getComponent().enabled = !getComponent().enabled; }
            ),
            new ModButton_Text(
                () => $"Reset All Selected{Environment.NewLine}Pawn's Schedules to ...",
                delegate { Find.WindowStack.Add(new FloatMenu(menuResetAllSchedules)); }
            ),
            new ModButton_Text(
                () =>
                    $"Immunity Handling is:{Environment.NewLine}{getComponent().immuneSensitivity.ToString().ToLower().CapitalizeFirst()}",
                delegate { Find.WindowStack.Add(new FloatMenu(menuImmuneSensitivty)); }
            ),
            new ModButton_Text(
                delegate
                {
                    var buttonLabel = $"Hungry Patients should:{Environment.NewLine}";
                    if (getComponent().spoonFeeding)
                    {
                        buttonLabel += "Wait to be Fed";
                    }
                    else
                    {
                        buttonLabel += "Feed Themselves";
                    }

                    return buttonLabel;
                },
                delegate { getComponent().spoonFeeding = !getComponent().spoonFeeding; }
            ),
            new ModButton_Text(
                () => $"Reset All Selected{Environment.NewLine}Pawn's Schedule Types to ...",
                delegate { Find.WindowStack.Add(new FloatMenu(menuResetAllScheduleTypes)); }
            ),
            new ModButton_Text(
                delegate
                {
                    var buttonLabel = $"Sleep Cycles Per Work:{Environment.NewLine}";
                    if (getComponent().doubleSleep)
                    {
                        buttonLabel += "Double Sleep";
                    }
                    else
                    {
                        buttonLabel += "Single Sleep";
                    }

                    return buttonLabel;
                },
                delegate { getComponent().doubleSleep = !getComponent().doubleSleep; }
            ),
            new ModButton_Text(
                delegate
                {
                    var buttonLabel = $"Eat Cycles Per Work:{Environment.NewLine}";
                    if (getComponent().doubleEat)
                    {
                        buttonLabel += "Double Eat";
                    }
                    else
                    {
                        buttonLabel += "Single Eat";
                    }

                    return buttonLabel;
                },
                delegate { getComponent().doubleEat = !getComponent().doubleEat; }
            ),
            new ModButton_Text(
                delegate
                {
                    var buttonLabel = $"Meditation is{Environment.NewLine}";
                    if (getComponent().manageMeditation)
                    {
                        buttonLabel += "Managed Here";
                    }
                    else
                    {
                        buttonLabel += "Not Managed Here";
                    }

                    return buttonLabel;
                },
                delegate { getComponent().manageMeditation = !getComponent().manageMeditation; }
            ),
            new ModButton_Text(
                delegate
                {
                    var buttonLabel = $"Joy Hold Extra is:{Environment.NewLine}";
                    if (getComponent().joyHoldExtra)
                    {
                        buttonLabel += "Enabled";
                    }
                    else
                    {
                        buttonLabel += "Disabled";
                    }

                    return buttonLabel;
                },
                delegate { getComponent().joyHoldExtra = !getComponent().joyHoldExtra; }
            ),
            new ModButton_Text(
                delegate
                {
                    var buttonLabel = $"Debug Logging is:{Environment.NewLine}";
                    if (getComponent().enableLogging)
                    {
                        buttonLabel += "Enabled";
                    }
                    else
                    {
                        buttonLabel += "Disabled";
                    }

                    return buttonLabel;
                },
                delegate { getComponent().enableLogging = !getComponent().enableLogging; }
            )
        };

        columns.Add(buttons);

        wasRegistered = true;
    }
}