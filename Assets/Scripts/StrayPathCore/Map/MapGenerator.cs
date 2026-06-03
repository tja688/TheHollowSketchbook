using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Core;
using UnityEngine;

namespace StrayPathCore.Map
{
    /// <summary>
    /// 地图生成器 —— 按Act生成3条路径组，各15节点。
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        public static MapGenerator Instance { get; private set; }

        // Act1 列偏移
        private static readonly int[] PG1Columns_Act1 = { 3, 2, 1, 2, 1, 2, 3, 3, 2, 1, 2, 1, 2, 3, 3 };
        private static readonly int[] PG2Columns_Act1 = { 5, 5, 4, 5, 6, 5, 5, 5, 5, 4, 5, 6, 5, 5, 5 };
        private static readonly int[] PG3Columns_Act1 = { 7, 8, 9, 8, 9, 8, 7, 7, 8, 9, 8, 9, 8, 7, 7 };
        private static readonly int[] RowIndices = { 0, 2, 4, 6, 8, 10, 12, 18, 20, 22, 24, 26, 28, 30, 32 };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public MapData GenerateMap(int actID)
        {
            var map = new MapData();

            // 难度分配: 1/2/3 随机无放回
            var difficulties = new List<int> { 1, 2, 3 };
            Shuffle(difficulties);
            map.Path1Difficulty = difficulties[0];
            map.Path2Difficulty = difficulties[1];
            map.Path3Difficulty = difficulties[2];

            // 生成各路径
            map.PG1 = GeneratePathGroup(1, actID, map.Path1Difficulty);
            map.PG2 = GeneratePathGroup(2, actID, map.Path2Difficulty);
            map.PG3 = GeneratePathGroup(3, actID, map.Path3Difficulty);

            // 跨路径组洗牌
            ShuffleAllSubpaths(map);

            return map;
        }

        private List<MapNode> GeneratePathGroup(int pg, int actID, int difficulty)
        {
            var nodes = new List<MapNode>();
            var columns = pg switch
            {
                1 => PG1Columns_Act1,
                2 => PG2Columns_Act1,
                3 => PG3Columns_Act1,
                _ => PG1Columns_Act1
            };

            // 获取模板
            var template = FillListBasedOnDifficulty(difficulty, actID);
            int eliteCount = template[0];
            int campfireCount = template[1];
            int normalCount = template[2];
            int shopCount = template[3];
            int extraEvents = template[4];

            // 创建基础节点(索引0-14)
            var types = new List<MapNodeType>();
            for (int i = 0; i < 15; i++)
            {
                if (i == 7) types.Add(MapNodeType.Treasure); // 第8节点固定宝藏
                else if (i == 14) types.Add(MapNodeType.Boss); // 第15节点固定Boss
                else types.Add(MapNodeType.Battle); // 先全部设为Normal
            }

            // 分配子路径1(索引0-6)和子路径2(索引7-14)
            var subPath1Indices = Enumerable.Range(0, 7).Where(i => types[i] == MapNodeType.Battle).ToList();
            var subPath2Indices = Enumerable.Range(8, 7).Where(i => types[i] == MapNodeType.Battle).ToList();

            // 分配Elite
            AssignRandomNodes(types, subPath1Indices, subPath2Indices, MapNodeType.Elite, eliteCount);

            // 分配Campfire
            AssignRandomNodes(types, subPath1Indices, subPath2Indices, MapNodeType.Campfire, campfireCount);

            // 分配Shop
            if (shopCount > 0)
            {
                var allNormal = subPath1Indices.Concat(subPath2Indices).Where(i => types[i] == MapNodeType.Battle).ToList();
                if (allNormal.Count > 0)
                {
                    int idx = allNormal[Random.Range(0, allNormal.Count)];
                    types[idx] = MapNodeType.Shop;
                }
            }

            // 剩余Normal填充
            // 额外事件: 将部分Normal替换为Mystery
            if (extraEvents > 0)
            {
                var normalIndices = Enumerable.Range(0, 15).Where(i => types[i] == MapNodeType.Battle).ToList();
                for (int i = 0; i < extraEvents && normalIndices.Count > 0; i++)
                {
                    int pick = Random.Range(0, normalIndices.Count);
                    types[normalIndices[pick]] = MapNodeType.Mystery;
                    normalIndices.RemoveAt(pick);
                }
            }

            // 构建MapNode列表
            for (int i = 0; i < 15; i++)
            {
                int nodeID = pg * 100 + i + 1; // 101-115, 201-215, 301-315
                nodes.Add(new MapNode
                {
                    NodeID = nodeID,
                    Type = types[i],
                    IsVisited = false,
                    RowIndex = RowIndices[i],
                    ColIndex = columns[i]
                });
            }

            return nodes;
        }

        private void AssignRandomNodes(List<MapNodeType> types, List<int> subPath1, List<int> subPath2, MapNodeType type, int count)
        {
            var available = subPath1.Concat(subPath2).Where(i => types[i] == MapNodeType.Battle).ToList();
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int pick = Random.Range(0, available.Count);
                types[available[pick]] = type;
                available.RemoveAt(pick);
            }
        }

        private List<int> FillListBasedOnDifficulty(int difficulty, int actID)
        {
            // 返回: [Elite, Campfire, Normal, Shop, ExtraEvents]
            switch (difficulty)
            {
                case 2: // Medium
                    if (actID == 3) return new List<int> { 2, 2, 6, 1, 1 };
                    int elite2 = Random.Range(1, 3);
                    return new List<int> { elite2, 2, 6, 1, 1 };
                case 3: // Hard
                    return new List<int> { 2, 2, 6, 1, 1 };
                default: // Easy
                    switch (actID)
                    {
                        case 2: int elite1 = Random.Range(0, 2); return new List<int> { elite1, 2, 6, 1, 1 };
                        case 3: return new List<int> { 1, 2, 6, 1, 1 };
                        default: return new List<int> { 0, 2, 6, 1, 1 };
                    }
            }
        }

        private void ShuffleAllSubpaths(MapData map)
        {
            // 子路径1: PG1[0..6], PG2[0..6], PG3[0..6]
            // 子路径2: PG1[7..14], PG2[7..14], PG3[7..14]
            // 随机将3个subPath1分配给PG1/PG2/PG3
            // 随机将3个subPath2分配给PG1/PG2/PG3
            // 简化实现: 对每个子路径内部shuffle
            ShuffleSubPath(map.PG1, 0, 6);
            ShuffleSubPath(map.PG1, 7, 14);
            ShuffleSubPath(map.PG2, 0, 6);
            ShuffleSubPath(map.PG2, 7, 14);
            ShuffleSubPath(map.PG3, 0, 6);
            ShuffleSubPath(map.PG3, 7, 14);
        }

        private void ShuffleSubPath(List<MapNode> nodes, int start, int end)
        {
            // 保持Treasure(7)和Boss(14)位置不变，只shuffle其他节点
            var toShuffle = new List<int>();
            for (int i = start; i <= end; i++)
            {
                if (i == 7 || i == 14) continue;
                toShuffle.Add(i);
            }
            if (toShuffle.Count <= 1) return;

            // Fisher-Yates shuffle on indices
            for (int i = toShuffle.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int idx1 = toShuffle[i];
                int idx2 = toShuffle[j];
                var temp = nodes[idx1].Type;
                nodes[idx1].Type = nodes[idx2].Type;
                nodes[idx2].Type = temp;
            }
        }

        // ==================== 持久化 ====================

        public void SaveMapToRunState(MapData map, int actID)
        {
            var run = GameStateManager.Instance.CurrentRun;
            var pg1 = map.PG1.Select(n => (int)n.Type).ToList();
            var pg2 = map.PG2.Select(n => (int)n.Type).ToList();
            var pg3 = map.PG3.Select(n => (int)n.Type).ToList();

            switch (actID)
            {
                case 1: run.IconArrayPG1_Act1 = pg1; run.IconArrayPG2_Act1 = pg2; run.IconArrayPG3_Act1 = pg3; break;
                case 2: run.IconArrayPG1_Act2 = pg1; run.IconArrayPG2_Act2 = pg2; run.IconArrayPG3_Act2 = pg3; break;
                case 3: run.IconArrayPG1_Act3 = pg1; run.IconArrayPG2_Act3 = pg2; run.IconArrayPG3_Act3 = pg3; break;
            }
        }

        public MapData LoadMapFromRunState(int actID)
        {
            var run = GameStateManager.Instance.CurrentRun;
            List<int> pg1, pg2, pg3;
            switch (actID)
            {
                case 1: pg1 = run.IconArrayPG1_Act1; pg2 = run.IconArrayPG2_Act1; pg3 = run.IconArrayPG3_Act1; break;
                case 2: pg1 = run.IconArrayPG1_Act2; pg2 = run.IconArrayPG2_Act2; pg3 = run.IconArrayPG3_Act2; break;
                case 3: pg1 = run.IconArrayPG1_Act3; pg2 = run.IconArrayPG2_Act3; pg3 = run.IconArrayPG3_Act3; break;
                default: return null;
            }
            if (pg1 == null || pg1.Count == 0) return null;

            var map = new MapData();
            map.PG1 = RebuildPathGroup(1, pg1);
            map.PG2 = RebuildPathGroup(2, pg2);
            map.PG3 = RebuildPathGroup(3, pg3);
            return map;
        }

        private List<MapNode> RebuildPathGroup(int pg, List<int> types)
        {
            var nodes = new List<MapNode>();
            var columns = pg switch
            {
                1 => PG1Columns_Act1,
                2 => PG2Columns_Act1,
                3 => PG3Columns_Act1,
                _ => PG1Columns_Act1
            };
            for (int i = 0; i < types.Count && i < 15; i++)
            {
                nodes.Add(new MapNode
                {
                    NodeID = pg * 100 + i + 1,
                    Type = (MapNodeType)types[i],
                    IsVisited = GameStateManager.Instance.CurrentRun.PathHistory.Contains(pg * 100 + i + 1),
                    RowIndex = RowIndices[i],
                    ColIndex = columns[i]
                });
            }
            return nodes;
        }

        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
