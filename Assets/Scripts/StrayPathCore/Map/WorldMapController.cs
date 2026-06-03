using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Core;
using StrayPathCore.Relic;
using UnityEngine;

namespace StrayPathCore.Map
{
    /// <summary>
    /// 世界地图场景主控制器 —— 管理地图加载、节点交互、状态保存/恢复。
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        public static WorldMapController Instance { get; private set; }

        public MapData CurrentMap { get; private set; }
        public int CurrentAct => GameStateManager.Instance.CurrentRun.Act;

        [Header("Systems")]
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private ActSystem actSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeMap();
        }

        public void InitializeMap()
        {
            var run = GameStateManager.Instance.CurrentRun;

            // 尝试从RunState恢复地图
            CurrentMap = mapGenerator?.LoadMapFromRunState(CurrentAct);

            // 无存档时生成新地图
            if (CurrentMap == null)
            {
                CurrentMap = mapGenerator?.GenerateMap(CurrentAct);
                if (CurrentMap != null)
                {
                    mapGenerator?.SaveMapToRunState(CurrentMap, CurrentAct);
                }
            }
            else
            {
                // 恢复已访问节点
                RestoreVisitedNodes();
            }

            // 触发世界状态遗物
            var relicManager = RelicManager.Instance;
            relicManager?.ExecuteWorldStateRelics(MapNodeType.Battle);
        }

        public bool CanSelectNode(int nodeID)
        {
            var run = GameStateManager.Instance.CurrentRun;
            int pg = PathSystem.GetPathGroupFromNodeID(nodeID);
            int pid = run.CurrentPID;
            int currentPG = run.CurrentPlayerPathGroup;

            return PathSystem.IsValidMove(nodeID, pid, currentPG, run.PathHistory);
        }

        public void OnNodeSelected(int nodeID)
        {
            if (!CanSelectNode(nodeID)) return;

            var run = GameStateManager.Instance.CurrentRun;
            int pg = PathSystem.GetPathGroupFromNodeID(nodeID);

            // 锁定路径组
            if (run.CurrentPlayerPathGroup == 0)
            {
                run.CurrentPlayerPathGroup = pg;
            }

            // 更新PID
            run.CurrentPID = nodeID;
            run.PathHistory.Add(nodeID);

            // 标记节点已访问
            MarkNodeVisited(nodeID);

            // 保存状态
            SaveMapState();

            // 获取节点类型并路由
            MapNodeType nodeType = GetNodeType(nodeID);
            NodeRouter.RouteToNode(nodeType, nodeID);
        }

        public MapNodeType GetNodeType(int nodeID)
        {
            int pg = PathSystem.GetPathGroupFromNodeID(nodeID);
            int index = (nodeID % 100) - 1;
            var path = pg switch
            {
                1 => CurrentMap?.PG1,
                2 => CurrentMap?.PG2,
                3 => CurrentMap?.PG3,
                _ => null
            };
            if (path != null && index >= 0 && index < path.Count)
                return path[index].Type;
            return MapNodeType.Battle;
        }

        public void MarkNodeVisited(int nodeID)
        {
            int pg = PathSystem.GetPathGroupFromNodeID(nodeID);
            int index = (nodeID % 100) - 1;
            var path = pg switch
            {
                1 => CurrentMap?.PG1,
                2 => CurrentMap?.PG2,
                3 => CurrentMap?.PG3,
                _ => null
            };
            if (path != null && index >= 0 && index < path.Count)
                path[index].IsVisited = true;
        }

        public void SaveMapState()
        {
            var run = GameStateManager.Instance.CurrentRun;
            run.MapScroll = 0; // 由表现层设置
            GameStateManager.Instance.SaveRunState();
        }

        public void RestoreMapState()
        {
            var run = GameStateManager.Instance.CurrentRun;
            // 恢复滚动位置等
        }

        public void RestoreVisitedNodes()
        {
            var history = GameStateManager.Instance.CurrentRun.PathHistory;
            foreach (int nodeID in history)
            {
                MarkNodeVisited(nodeID);
            }
        }

        public List<MapNode> GetCurrentPath()
        {
            int pg = GameStateManager.Instance.CurrentRun.CurrentPlayerPathGroup;
            if (pg == 0) return null;
            return pg switch
            {
                1 => CurrentMap?.PG1,
                2 => CurrentMap?.PG2,
                3 => CurrentMap?.PG3,
                _ => null
            };
        }

        public void OnReturnedFromSubScene()
        {
            // 从子场景返回后的处理
            var run = GameStateManager.Instance.CurrentRun;
            if (run.Defeated)
            {
                // 战败处理
                actSystem?.CalculateScore();
                // 返回标题画面
            }
            else if (run.CurrentPID >= PathSystem.GetBossNodeID(run.CurrentPlayerPathGroup))
            {
                // 击败了Boss，推进Act
                actSystem?.AdvanceAct();
                if (!actSystem.IsFinalActComplete())
                {
                    InitializeMap(); // 重新生成新Act地图
                }
            }
        }
    }
}
