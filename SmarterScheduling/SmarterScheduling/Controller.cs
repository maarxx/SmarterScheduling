using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using HugsLib;
using HugsLib.Utils;

namespace SmarterScheduling
{
    public class Controller : ModBase
    {
        public override string ModIdentifier => "SmarterScheduling";

        private static ModLogger _logger;

        public Controller() : base() { _logger = base.Logger; }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            WorldObject_Brain.OnDefsLoaded();
        }

        public new static ModLogger Logger => _logger;

        public override void MapLoaded(Map map)
        {
            base.MapLoaded(map);

            // Fluffy did this. I don't know what it is.
            // I'll leave it commented here for now in case I need it.
            // Holy crap I have no idea what I'm doing please send help.

            //if (map.GetComponent<MapComponent_Timekeeper>() == null)
                //map.components.Add(new MapComponent_Timekeeper(map));
        }
    }
}
