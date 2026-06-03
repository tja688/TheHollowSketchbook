using System.Collections.Generic;

namespace StrayPathCore.Map
{
    public static class PathSystem
    {
        public static int GetPathGroupFromNodeID(int nodeID)
        {
            return nodeID / 100;
        }

        public static bool IsValidMove(int targetNodeID, int currentPID, int currentPG, List<int> pathHistory)
        {
            if (currentPG == 0)
            {
                return IsStartNode(targetNodeID);
            }

            int targetPG = GetPathGroupFromNodeID(targetNodeID);
            if (targetPG != currentPG)
                return false;

            if (pathHistory != null && pathHistory.Contains(targetNodeID))
                return false;

            return targetNodeID == currentPID + 1;
        }

        public static bool IsStartNode(int nodeID)
        {
            return nodeID == 100 || nodeID == 200 || nodeID == 300;
        }

        public static int GetNextNodeID(int currentNodeID)
        {
            return currentNodeID + 1;
        }

        public static int GetTreasureNodeID(int pathGroup)
        {
            return pathGroup * 100 + 8;
        }

        public static int GetBossNodeID(int pathGroup)
        {
            return pathGroup * 100 + 15;
        }
    }
}
