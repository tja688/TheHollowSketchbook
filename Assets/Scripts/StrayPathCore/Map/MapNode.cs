using System;
using StrayPathCore.Core;

namespace StrayPathCore.Map
{
    [Serializable]
    public class MapNode
    {
        public int NodeID; // 101-115 for PG1, 201-215 for PG2, 301-315 for PG3
        public MapNodeType Type;
        public bool IsVisited;
        public int RowIndex;
        public int ColIndex;
    }
}
