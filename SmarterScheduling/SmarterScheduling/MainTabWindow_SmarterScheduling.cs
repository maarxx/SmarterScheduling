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
            bool curImmuneSensitivity = component.immuneSensitivity;
            bool curSpoonFeeding = component.spoonFeeding;

            for (int i = 0; i < 4; i++)
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
                        buttonLabel = "Immunity Handling is: ";
                        if (curImmuneSensitivity)
                        {
                            buttonLabel += "Sensitive";
                        }
                        else
                        {
                            buttonLabel += "Efficient";
                        }
                        if (Widgets.ButtonText(nextButton, buttonLabel))
                        {
                            component.immuneSensitivity = !curImmuneSensitivity;
                        }
                        break;
                    case 3:
                        buttonLabel = "Patient Feeding is: ";
                        if (curSpoonFeeding)
                        {
                            buttonLabel += "Spoon Feed by Others";
                        }
                        else
                        {
                            buttonLabel += "Feed Your Damn Selves";
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
