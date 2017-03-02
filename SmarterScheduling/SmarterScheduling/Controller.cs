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

        public new static ModLogger Logger => _logger;

        public Controller() : base() { _logger = base.Logger; }

        /*
        public override void DefsLoaded()
        {
            base.DefsLoaded();
            WorldObject_Brain.OnDefsLoaded();
        }
        */

        /*
        public override void MapLoaded(Map map)
        {
            Log.Message("Hello, world!");
        }
        */
    }
}
