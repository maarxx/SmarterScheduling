using ModButtons;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SmarterScheduling
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = new Harmony("com.github.harmony.rimworld.maarx.smarterscheduling");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_ModButtons))]
    [HarmonyPatch("DoWindowContents")]
    class Patch_MainTabWindow_ModButtons_DoWindowContents
    {
        static void Prefix(MainTabWindow_ModButtons __instance, ref Rect canvas)
        {
            SmarterScheduling_RegisterToMainTab.ensureMainTabRegistered();
        }
    }

    class SmarterScheduling_RegisterToMainTab
    {
        private static MapComponent_SmarterScheduling getComponent()
        {
            return Find.CurrentMap.GetComponent<MapComponent_SmarterScheduling>();
        }

        public static bool wasRegistered = false;

        public static void ensureMainTabRegistered()
        {
            if (wasRegistered) { return; }

            Log.Message("Hello from SmarterScheduling_RegisterToMainTab ensureMainTabRegistered");

            List<List<ModButton_Text>> columns = MainTabWindow_ModButtons.columns;

            List<FloatMenuOption> menuImmuneSensitivty = new List<FloatMenuOption>();
            foreach (MapComponent_SmarterScheduling.ImmuneSensitivity immSen in Enum.GetValues(typeof(MapComponent_SmarterScheduling.ImmuneSensitivity)))
            {
                menuImmuneSensitivty.Add(new FloatMenuOption(immSen.ToString().ToLower().CapitalizeFirst(), delegate { getComponent().immuneSensitivity = immSen; }));
            }

            List<FloatMenuOption> menuResetAllSchedules = new List<FloatMenuOption>();
            foreach (MapComponent_SmarterScheduling.PawnState pawnState in Enum.GetValues(typeof(MapComponent_SmarterScheduling.PawnState)))
            {
                menuResetAllSchedules.Add(new FloatMenuOption(pawnState.ToString().ToLower().CapitalizeFirst(), delegate { getComponent().resetSelectedPawnsSchedules(pawnState); }));
            }

            List<FloatMenuOption> menuResetAllScheduleTypes = new List<FloatMenuOption>();
            foreach (MapComponent_SmarterScheduling.ScheduleType scheduleType in Enum.GetValues(typeof(MapComponent_SmarterScheduling.ScheduleType)))
            {
                menuResetAllScheduleTypes.Add(new FloatMenuOption(scheduleType.ToString().ToLower().CapitalizeFirst(), delegate { getComponent().resetSelectedPawnsScheduleTypes(scheduleType); }));
            }

            List<ModButton_Text> buttons = new List<ModButton_Text>();

            buttons.Add(new ModButton_Text(
                delegate
                {
                    string buttonLabel = "Entire Mod is Currently:" + Environment.NewLine;
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
                delegate {
                    getComponent().enabled = !getComponent().enabled;
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    return "Reset All Selected" + Environment.NewLine + "Pawn's Schedules to ...";
                },
                delegate {
                    Find.WindowStack.Add(new FloatMenu(menuResetAllSchedules));
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    return "Immunity Handling is:" + Environment.NewLine + getComponent().immuneSensitivity.ToString().ToLower().CapitalizeFirst();
                },
                delegate {
                    Find.WindowStack.Add(new FloatMenu(menuImmuneSensitivty));
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    string buttonLabel = "Hungry Patients should:" + Environment.NewLine;
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
                delegate {
                    getComponent().spoonFeeding = !getComponent().spoonFeeding;
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    return "Reset All Selected" + Environment.NewLine + "Pawn's Schedule Types to ...";
                },
                delegate {
                    Find.WindowStack.Add(new FloatMenu(menuResetAllScheduleTypes));
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    string buttonLabel = "Sleep Cycles Per Work:" + Environment.NewLine;
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
                delegate {
                    getComponent().doubleSleep = !getComponent().doubleSleep;
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    string buttonLabel = "Eat Cycles Per Work:" + Environment.NewLine;
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
                delegate {
                    getComponent().doubleEat = !getComponent().doubleEat;
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    string buttonLabel = "Meditation is" + Environment.NewLine;
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
                delegate {
                    getComponent().manageMeditation = !getComponent().manageMeditation;
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    string buttonLabel = "Joy Hold Extra is:" + Environment.NewLine;
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
                delegate {
                    getComponent().joyHoldExtra = !getComponent().joyHoldExtra;
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    string buttonLabel = "Debug Logging is:" + Environment.NewLine;
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
                delegate {
                    getComponent().enableLogging = !getComponent().enableLogging;
                }
            ));

            columns.Add(buttons);

            wasRegistered = true;
        }
    }
}
