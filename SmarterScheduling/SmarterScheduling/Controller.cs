using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            //WorldObject_Priorities.OnDefsLoaded();
        }

        public new static ModLogger Logger => _logger;

        public override void MapLoaded(Map map)
        {
            base.MapLoaded(map);

            // make sure each loaded map has our timekeeper component
            // this will inject the component into existing save games.
            //if (map.GetComponent<MapComponent_Timekeeper>() == null)
                //map.components.Add(new MapComponent_Timekeeper(map));
        }
    }
}
