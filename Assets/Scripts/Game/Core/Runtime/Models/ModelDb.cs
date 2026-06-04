using System.Collections.Generic;
using System.Linq;
using Game.Core.Logging;

namespace Game.Core.Models
{
    public static class ModelDb
    {
        private static readonly Dictionary<ModelId, AbstractModel> Models = new Dictionary<ModelId, AbstractModel>();

        public static int Count
        {
            get { return Models.Count; }
        }

        public static IEnumerable<AbstractModel> AllModels
        {
            get { return Models.Values; }
        }

        public static void Clear()
        {
            Models.Clear();
        }

        public static bool Contains(ModelId id)
        {
            return Models.ContainsKey(id);
        }

        public static void Register(AbstractModel model)
        {
            if (model == null)
            {
                throw new GameException("Cannot register a null model.");
            }

            if (!model.IsCanonical)
            {
                throw new GameException("Only canonical models can be registered.");
            }

            if (!Models.TryAdd(model.Id, model))
            {
                throw new GameException("Duplicate model id: " + model.Id);
            }
        }

        public static AbstractModel Get(ModelId id)
        {
            if (!Models.TryGetValue(id, out AbstractModel model))
            {
                throw new KeyNotFoundException(id.ToString());
            }

            return model;
        }

        public static T Get<T>(ModelId id) where T : AbstractModel
        {
            AbstractModel model = Get(id);
            if (model is not T typedModel)
            {
                throw new GameException(id + " is not a " + typeof(T).Name + ".");
            }

            return typedModel;
        }

        public static bool TryGet<T>(ModelId id, out T model) where T : AbstractModel
        {
            if (Models.TryGetValue(id, out AbstractModel rawModel) && rawModel is T typedModel)
            {
                model = typedModel;
                return true;
            }

            model = null;
            return false;
        }

        public static AbstractModel CreateMutable(ModelId id)
        {
            return Get(id).CloneMutable();
        }

        public static T CreateMutable<T>(ModelId id) where T : AbstractModel
        {
            return Get<T>(id).CloneMutable<T>();
        }

        public static IEnumerable<T> All<T>() where T : AbstractModel
        {
            return Models.Values.OfType<T>();
        }
    }
}
