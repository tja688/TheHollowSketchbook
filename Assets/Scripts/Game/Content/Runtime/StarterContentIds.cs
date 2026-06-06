using Game.Core;

namespace Game.Content.Runtime
{
    public static class StarterContentIds
    {
        public static readonly ModelId PlayerHero = new ModelId("player", "hero");

        public static readonly ModelId RouteCombat = new ModelId("route", "combat");
        public static readonly ModelId RouteGold = new ModelId("route", "gold");
        public static readonly ModelId RouteChest = new ModelId("route", "chest");
        public static readonly ModelId RouteStatUpgrade = new ModelId("route", "statupgrade");
        public static readonly ModelId RouteShop = new ModelId("route", "shop");
        public static readonly ModelId RouteEliteCombat = new ModelId("route", "elitecombat");
        public static readonly ModelId RouteBossCombat = new ModelId("route", "bosscombat");
        public static readonly ModelId RouteReward = new ModelId("route", "reward");
        public static readonly ModelId RouteRestaurant = new ModelId("route", "restaurant");

        public static class Traits
        {
            public static readonly ModelId Banner = new ModelId("trait", "banner");
            public static readonly ModelId Revenge = new ModelId("trait", "revenge");
            public static readonly ModelId Aggressive = new ModelId("trait", "aggressive");
            public static readonly ModelId Ambush = new ModelId("trait", "ambush");
            public static readonly ModelId ArmorBreak = new ModelId("trait", "armor-break");
            public static readonly ModelId Scatter = new ModelId("trait", "scatter");
            public static readonly ModelId ThornSkin = new ModelId("trait", "thorn-skin");
            public static readonly ModelId IronSkin = new ModelId("trait", "iron-skin");
            public static readonly ModelId Veteran = new ModelId("trait", "veteran");
            public static readonly ModelId Violence = new ModelId("trait", "violence");
            public static readonly ModelId FirstStrike = new ModelId("trait", "first-strike");
        }

        public static class Monsters
        {
            public static readonly ModelId Skeleton = new ModelId("monster", "skeleton");
            public static readonly ModelId ArmoredSkeleton = new ModelId("monster", "armored-skeleton");
            public static readonly ModelId BannerSkeleton = new ModelId("monster", "banner-skeleton");
            public static readonly ModelId RevengeSkeleton = new ModelId("monster", "revenge-skeleton");
            public static readonly ModelId TrackerSkeleton = new ModelId("monster", "tracker-skeleton");
            public static readonly ModelId AmbusherSkeleton = new ModelId("monster", "ambusher-skeleton");
            public static readonly ModelId WarSkeleton = new ModelId("monster", "war-skeleton");
            public static readonly ModelId BigSkeletonLord = new ModelId("monster", "big-skeleton-lord");
        }

        public static class Traps
        {
            public static readonly ModelId Crossbow = new ModelId("trap", "crossbow");
            public static readonly ModelId Spike = new ModelId("trap", "spike");
            public static readonly ModelId Teleport = new ModelId("trap", "teleport");
        }

        public static class Items
        {
            public static readonly ModelId HookRope = new ModelId("item", "hook-rope");
            public static readonly ModelId HealingPotion = new ModelId("item", "healing-potion");
            public static readonly ModelId ThrowingKnife = new ModelId("item", "throwing-knife");
            public static readonly ModelId ProtectionSpell = new ModelId("item", "protection-spell");
            public static readonly ModelId FlipCard = new ModelId("item", "flip-card");
            public static readonly ModelId LightCard = new ModelId("item", "light-card");
            public static readonly ModelId ViolenceCard = new ModelId("item", "violence-card");
            public static readonly ModelId FirstStrikeCard = new ModelId("item", "first-strike-card");
        }

        public static class RoomCards
        {
            public static readonly ModelId Gold = new ModelId("room", "gold");
            public static readonly ModelId StatUpgrade = new ModelId("room", "stat-upgrade");
            public static readonly ModelId Food = new ModelId("room", "food");
            public static readonly ModelId OrdinaryChest = new ModelId("room", "ordinary-chest");
            public static readonly ModelId BlueChest = new ModelId("room", "blue-chest");
            public static readonly ModelId GoldChest = new ModelId("room", "gold-chest");
            public static readonly ModelId MentorThornSkin = new ModelId("room", "mentor-thorn-skin");
            public static readonly ModelId MentorIronSkin = new ModelId("room", "mentor-iron-skin");
            public static readonly ModelId MentorVeteran = new ModelId("room", "mentor-veteran");
            public static readonly ModelId ShopAttack = new ModelId("room", "shop-attack");
            public static readonly ModelId ShopDefense = new ModelId("room", "shop-defense");
            public static readonly ModelId ShopMaxHp = new ModelId("room", "shop-max-hp");
            public static readonly ModelId ShopRandomItem = new ModelId("room", "shop-random-item");
            public static readonly ModelId ShopOrdinaryChest = new ModelId("room", "shop-ordinary-chest");
            public static readonly ModelId ActivePickupLawWand = new ModelId("room", "pickup-law-wand");
            public static readonly ModelId ActivePickupEndlessWaterBag = new ModelId("room", "pickup-endless-water-bag");
            public static readonly ModelId ActivePickupBloodShield = new ModelId("room", "pickup-blood-shield");
        }

        public static class Relics
        {
            public static readonly ModelId LivingFlesh = new ModelId("relic", "living-flesh");
            public static readonly ModelId WoodShield = new ModelId("relic", "wood-shield");
            public static readonly ModelId WoodSword = new ModelId("relic", "wood-sword");
            public static readonly ModelId LawWand = new ModelId("relic", "law-wand");
            public static readonly ModelId EndlessWaterBag = new ModelId("relic", "endless-water-bag");
            public static readonly ModelId ItemStockpile = new ModelId("relic", "item-stockpile");
            public static readonly ModelId BloodShield = new ModelId("relic", "blood-shield");
            public static readonly ModelId VillageGoodSword = new ModelId("relic", "village-good-sword");
        }
    }
}
