using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Entities;
using Game.Core.Map;
using Game.Core.Models;

namespace Game.Core.Rooms
{
    public sealed class RoomFactory
    {
        private static readonly ModelId[] EliteEncounters =
        {
            new ModelId("Encounter", "PrototypeEliteEncounter")
        };

        private static readonly ModelId BossEncounter = new ModelId("Encounter", "PrototypeBossEncounter");

        public AbstractRoom CreateRoomForMapPoint(Runs.RunState run, MapPoint point)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            return point.PointType switch
            {
                MapPointType.Monster => new CombatRoom(RoomType.Combat, point, PickEncounter(run.CurrentAct.EncounterIds, run), false),
                MapPointType.Elite => new CombatRoom(RoomType.Combat, point, PickEncounter(EliteEncounters, run), true),
                MapPointType.Treasure => new TreasureRoom(point),
                MapPointType.Event => new EventRoomPlaceholder(point),
                MapPointType.Rest => new RestSiteRoomPlaceholder(point),
                MapPointType.Shop => new ShopRoomPlaceholder(point),
                MapPointType.Boss => new BossRoom(point, ModelDb.Get<EncounterModel>(BossEncounter)),
                _ => throw new InvalidOperationException("Unsupported map point type: " + point.PointType)
            };
        }

        public CombatRoom CreateCombatRoom(MapPoint point, EncounterModel encounter, bool isElite, bool isBoss)
        {
            if (isBoss)
            {
                return new BossRoom(point, encounter);
            }

            return new CombatRoom(RoomType.Combat, point, encounter, isElite);
        }

        private static EncounterModel PickEncounter(IReadOnlyList<ModelId> candidates, Runs.RunState run)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new InvalidOperationException("Encounter candidate list is empty.");
            }

            return ModelDb.Get<EncounterModel>(run.Rng.Pick(candidates));
        }
    }
}
