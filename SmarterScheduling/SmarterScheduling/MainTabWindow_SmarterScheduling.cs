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

            MapComponent_SmarterScheduling component = Find.VisibleMap.GetComponent<MapComponent_SmarterScheduling>();
            bool curEnabled = component.enabled;
            bool curImmuneAlwaysAnything = component.immuneAlwaysAnything;

            for (int i = 0; i < 3; i++)
            {
                Rect nextButton = new Rect(canvas);
                nextButton.y = i * (BUTTON_HEIGHT + BUTTON_SPACE);
                nextButton.height = BUTTON_HEIGHT;

                string buttonLabel;
                switch (i)
                {
                    case 0:
                        buttonLabel = "Current Enabled is: ";
                        if (curEnabled)
                        {
                            buttonLabel += "ON";
                        }
                        else
                        {
                            buttonLabel += "OFF";
                        }
                        if (Widgets.ButtonText(nextButton, buttonLabel))
                        {
                            component.enabled = !curEnabled;
                        }
                        break;
                    case 1:
                        buttonLabel = "Reset all Pawn Schedules to Anything";
                        if (Widgets.ButtonText(nextButton, buttonLabel))
                        {
                            component.resetAllSchedules(MapComponent_SmarterScheduling.PawnState.ANYTHING);
                        }
                        break;
                    case 2:
                        buttonLabel = "Gaining Immunity is: ";
                        if (curImmuneAlwaysAnything)
                        {
                            buttonLabel += "Always Anything";
                        }
                        else
                        {
                            buttonLabel += "Sometimes Joy or Doctor";
                        }
                        if (Widgets.ButtonText(nextButton, buttonLabel))
                        {
                            component.immuneAlwaysAnything = !curImmuneAlwaysAnything;
                        }
                        break;
                }
            }
        }

    }
}
