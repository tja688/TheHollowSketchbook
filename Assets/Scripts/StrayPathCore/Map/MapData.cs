using System;
using System.Collections.Generic;

namespace StrayPathCore.Map
{
    [Serializable]
    public class MapData
    {
        public List<MapNode> PG1 = new List<MapNode>();
        public List<MapNode> PG2 = new List<MapNode>();
        public List<MapNode> PG3 = new List<MapNode>();
        public int Path1Difficulty;
        public int Path2Difficulty;
        public int Path3Difficulty;
    }
}
