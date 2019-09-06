using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace SmarterScheduling
{
    class MainTabWindow_SmarterScheduling : MainTabWindow
    {

        private const float BUTTON_HEIGHT = 50f;
        private const float BUTTON_SPACE = 10f;


        public MainTabWindow_SmarterScheduling()
        {
            //base.forcePause = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                //return base.InitialSize;
                return new Vector2(250f, 400f);
            }
        }

        public override MainTabWindowAnchor Anchor =>
            MainTabWindowAnchor.Right;

        public override void DoWindowContents(Rect canvas)
        {
            base.DoWindowContents(canvas);

            MapComponent_SmarterScheduling component = Find.CurrentMap.GetComponent<MapComponent_SmarterScheduling>();
            bool curEnabled = component.enabled;
            MapComponent_SmarterScheduling.ImmuneSensitivity curImmuneSensitivity = component.immuneSensitivity;
            bool curSpoonFeeding = component.spoonFeeding;

            List<FloatMenuOption> menuImmuneSensitivty = new List<FloatMenuOption>();
            foreach (MapComponent_SmarterScheduling.ImmuneSensitivity immSen in Enum.GetValues(typeof(MapComponent_SmarterScheduling.ImmuneSensitivity)))
            {
                menuImmuneSensitivty.Add(new FloatMenuOption(immSen.ToString().ToLower().CapitalizeFirst(), delegate { component.immuneSensitivity = immSen; }));
            }

            List<FloatMenuOption> menuResetAllSchedules = new List<FloatMenuOption>();
            foreach (MapComponent_SmarterScheduling.PawnState pawnState in Enum.GetValues(typeof(MapComponent_SmarterScheduling.PawnState)))
            {
                menuResetAllSchedules.Add(new FloatMenuOption(pawnState.ToString().ToLower().CapitalizeFirst(), delegate { component.resetAllSchedules(pawnState); }));
            }

            Text.Font = GameFont.Small;
            for (int i = 0; i < 4; i++)
            {
                Rect nextButton = new Rect(canvas);
                nextButton.y = i * (BUTTON_HEIGHT + BUTTON_SPACE);
                nextButton.height = BUTTON_HEIGHT;

                string buttonLabel;
                switch (i)
                {
                    case 0:
                        buttonLabel = "Entire Mod is Currently:" + Environment.NewLine;
                        if (curEnabled)
                        {
                            buttonLabel += "ENABLED";
                        }
                        else
                        {
                            buttonLabel += "DISABLED";
                        }
                        if (Widgets.ButtonText(nextButton, buttonLabel))
                        {
                            component.enabled = !curEnabled;
                        }
                        break;
                    case 1:
                        buttonLabel = "Reset All Pawn's" + Environment.NewLine + "Schedules to ...";
                        if (Widgets.ButtonText(nextButton, buttonLabel))
                        {
                            Find.WindowStack.Add(new FloatMenu(menuResetAllSchedules));
                        }
                        break;
                    case 2:
                        buttonLabel = "Immunity Handling is:" + Environment.NewLine + curImmuneSensitivity.ToString().ToLower().CapitalizeFirst();
                        if (Widgets.ButtonText(nextButton, buttonLabel))
                        {
                            Find.WindowStack.Add(new FloatMenu(menuImmuneSensitivty));
                        }
                        break;
                    case 3:
                        buttonLabel = "Hungry Patients should:" + Environment.NewLine;
                        if (curSpoonFeeding)
                        {
                            buttonLabel += "Wait to be Fed";
                        }
                        else
                        {
                            buttonLabel += "Feed Themselves";
                        }
                        if (Widgets.ButtonText(nextButton, buttonLabel))
                        {
                            component.spoonFeeding = !curSpoonFeeding;
                        }
                        break;
                }
            }
        }

    }
}
