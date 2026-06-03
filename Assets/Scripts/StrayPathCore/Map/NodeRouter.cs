using StrayPathCore.Core;
using StrayPathCore.Utils;

namespace StrayPathCore.Map
{
    public static class NodeRouter
    {
        public static string GetSceneName(MapNodeType nodeType)
        {
            switch (nodeType)
            {
                case MapNodeType.Battle:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    return "Battle";
                case MapNodeType.Shop:
                    return "Shop";
                case MapNodeType.Mystery:
                    return "Mystery";
                case MapNodeType.Campfire:
                    return "Campfire";
                case MapNodeType.Treasure:
                    return "Treasure";
                default:
                    return "Battle";
            }
        }

        public static int GetBattleType(MapNodeType nodeType)
        {
            switch (nodeType)
            {
                case MapNodeType.Battle:
                    return 1;
                case MapNodeType.Elite:
                    return 2;
                case MapNodeType.Boss:
                    return 3;
                default:
                    return 1;
            }
        }

        public static void RouteToNode(MapNodeType nodeType, int nodeID)
        {
            string sceneName = GetSceneName(nodeType);
            int battleType = GetBattleType(nodeType);

            var gsm = GameStateManager.Instance;
            gsm.NextSceneName = sceneName;
            gsm.CurrentRun.BattleType = battleType;

            int pathGroup = PathSystem.GetPathGroupFromNodeID(nodeID);

            GameEventBus.Instance.Publish(new NodeEnteredEvent
            {
                NodeType = nodeType,
                NodeID = nodeID,
                PathGroup = pathGroup
            });

            SceneTransitionManager.Instance.TransitionTo(sceneName);
        }
    }
}
