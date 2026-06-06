using System;
using System.Collections.Generic;
using Game.Core.Models;
using Game.Core.Random;

namespace Game.Core.Domain.Deck
{
    /// <summary>
    /// Central registry that maps card categories to their registered ModelIds.
    /// DungeonDeckBuilder queries this catalog to pick real content models
    /// instead of producing synthetic runtime-only ModelIds.
    ///
    /// L1 Model ID naming convention:
    ///   Monsters:   category="l{layer}", entry="{name}"       e.g. ("l1", "skeleton")
    ///   Traps:      category="l{layer}", entry="{name}-trap"  e.g. ("l1", "crossbow-trap")
    ///   Items:      category="item",   entry="{name}"         e.g. ("item", "healing-potion")
    ///   Room cards: category="room",   entry="{name}"         e.g. ("room", "food"), ("room", "mentor")
    ///   Relics:     category="relic",  entry="{name}"         e.g. ("relic", "thorn-skin")
    ///
    /// The catalog stores only ModelIds; the actual CardModel instances live in ModelDb.
    /// </summary>
    public sealed class RoomContentCatalog
    {
        private readonly Dictionary<string, List<ModelId>> _entries = new Dictionary<string, List<ModelId>>();

        public void Register(string category, ModelId modelId)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            if (!_entries.TryGetValue(category, out List<ModelId> list))
            {
                list = new List<ModelId>();
                _entries[category] = list;
            }

            if (!list.Contains(modelId))
            {
                list.Add(modelId);
            }
        }

        public void RegisterMonster(int tier, ModelId modelId)
        {
            Register("monster-" + tier, modelId);
        }

        public ModelId PickRandom(string category, IRng rng)
        {
            IReadOnlyList<ModelId> options = GetAvailable(category);
            if (options.Count == 0)
            {
                throw new InvalidOperationException("No models registered for category: " + category);
            }

            return options[rng.NextInt(0, options.Count)];
        }

        public bool TryPickRandom(string category, IRng rng, out ModelId result)
        {
            IReadOnlyList<ModelId> options = GetAvailable(category);
            if (options.Count == 0)
            {
                result = default;
                return false;
            }

            result = options[rng.NextInt(0, options.Count)];
            return true;
        }

        public IReadOnlyList<ModelId> GetAvailable(string category)
        {
            if (_entries.TryGetValue(category, out List<ModelId> list))
            {
                return list.AsReadOnly();
            }

            return Array.Empty<ModelId>();
        }

        public IReadOnlyList<ModelId> GetAvailableMonsters(int tier)
        {
            return GetAvailable("monster-" + tier);
        }

        public bool HasCategory(string category)
        {
            return _entries.ContainsKey(category) && _entries[category].Count > 0;
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
