﻿using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace SmarterScheduling
{
    class MapComponent_Brain : MapComponent
    {
        public MapComponent_Brain(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            Log.Message("Hello, world!");
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {

            }
        }
    }
}