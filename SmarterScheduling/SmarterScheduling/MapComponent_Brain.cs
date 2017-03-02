using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace SmarterScheduling
{
    class MapComponent_Brain : MapComponent
    {
        int slowDown = 0;

        public MapComponent_Brain(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            //Log.Message("Hello, FAST!");
            // WOW that is FAST lets slow it down a little.

            slowDown++;
            if (slowDown < 100)
            {
                return;
            }
            else
            {
                slowDown = 0;
            }

            Log.Message("Hello, slow! MapID:" + map.uniqueID + ", ColonistCount:" + map.mapPawns.FreeColonistsSpawnedCount);
            // OKAY that should be better.

            /*
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {

            }
            */
        }
    }
}
