using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using storagehub;

namespace TemporalStorageFilterFix
{
    public sealed class FilterFixSystem : ModSystem
    {
        private static readonly object Sync = new object();
        private static FilterCategory[] originals;
        private static FilterCategory[] replacements;
        private static int users;
        private bool registered;

        public override double ExecuteOrder() { return 1.1; }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            lock (Sync)
            {
                if (users == 0)
                {
                    originals = (FilterCategory[])FilterCategories.All.Clone();
                    replacements = new FilterCategory[originals.Length];
                    for (int i = 0; i < originals.Length; i++)
                    {
                        FilterCategory original = originals[i];
                        replacements[i] = new FilterCategory(original.Id, (world, stack) =>
                        {
                            string category = Rules.Override(world, stack);
                            return category == null ? original.Matches(world, stack) : category == original.Id;
                        });
                        FilterCategories.All[i] = replacements[i];
                    }
                }
                users++;
                registered = true;
            }
            api.Logger.Notification("[Temporal Storage Filter Fix] Classification corrections enabled.");
        }

        // Baseline is exposed for the separate development audit; it never moves inventories.
        public static FilterCategory[] OriginalCategories
        {
            get { return originals == null ? (FilterCategory[])FilterCategories.All.Clone() : (FilterCategory[])originals.Clone(); }
        }

        public override void Dispose()
        {
            lock (Sync)
            {
                if (!registered) return;
                registered = false;
                if (--users == 0)
                {
                    for (int i = 0; i < originals.Length; i++)
                        if (ReferenceEquals(FilterCategories.All[i], replacements[i])) FilterCategories.All[i] = originals[i];
                    originals = null;
                    replacements = null;
                }
            }
            base.Dispose();
        }
    }

    public static class Rules
    {
        public static string Override(IWorldAccessor world, ItemStack stack)
        {
            AssetLocation code = stack == null ? null : stack.Collectible == null ? null : stack.Collectible.Code;
            if (code == null) return null;
            if (stack.Collectible.GetCollectibleBehavior(typeof(CollectibleBehaviorWearable), true) != null) return "clothing";
            string path = code.Path;
            if (code.Domain == "alchemy")
            {
                if (Family(path, "cuttings")) return "plants";
                if (Family(path, "mortarpestle") || Family(path, "stirringspoon")) return "tools";
            }
            if (code.Domain == "substrate" && (Family(path, "sporeprint") || path == "wetsporepaper")) return "plants";
            if (code.Domain == "butchering")
            {
                if (path.StartsWith("dead", StringComparison.Ordinal) || Family(path, "ivory") ||
                    Family(path, "sinew") || Family(path, "strongbone") || Family(path, "fishingbait")) return "animal";
                if (Family(path, "butcherybag")) return "clothing";
                if (Family(path, "hook") || Family(path, "needle") || Family(path, "toolhead")) return "tools";
            }
            if (code.Domain == "primitivesurvival")
            {
                if (Family(path, "psfreshwaterfish") || Family(path, "pssaltwaterfish") || path == "caviar-curing") return "food-perishable";
                if (Family(path, "trussedmeat")) return path.EndsWith("-smoked", StringComparison.Ordinal) ? "food-preserved" : "food-perishable";
                if (Family(path, "fishinghook") || Family(path, "metalfishinghook") || Family(path, "fishinglure")) return "tools";
            }
            if (code.Domain == "rustboundmagic" && (Family(path, "inventorybag") || Family(path, "armorarm") ||
                Family(path, "armorbody") || Family(path, "armorhead") || Family(path, "armorlegs") ||
                Family(path, "armorneck") || Family(path, "armorwaist"))) return "clothing";
            if (code.Domain == "game")
            {
                string p = code.Path;

                // Storage categories only: these rules do not change nutrition or spoilage.
                if (Family(p, "pumpkin-fruit")) return "plants";
                if (p == "vegetable-pumpkin") return "food-perishable";
                if (Family(p, "flour")) return "food-preserved";

                if (Family(p, "fishraw") || Family(p, "rawcheese") || Family(p, "spice") ||
                    Family(p, "curdledmilkportion") || Family(p, "curdportion") || Family(p, "wheycurdportion") ||
                    Family(p, "pressedmash")) return "food-perishable";
                if (p == "salt") return "food-preserved";

                if (Family(p, "metalchain") || Family(p, "metallamellae") || Family(p, "metalnailsandstrips") ||
                    Family(p, "metalscale") || Family(p, "solderbar") || Family(p, "ironbloom")) return "metals";
                if (Family(p, "agedfirewood") || p == "oakbark" || Family(p, "bamboostakes")) return "wood";
                if (Family(p, "antler") || Family(p, "tusk") || p == "bonemeal") return "animal";
                if (Family(p, "fruitingbushrunner") || p == "cattailroot" || p == "cattailtops" ||
                    p == "papyrusroot" || p == "papyrustops" || p == "tuleroot" || p == "drygrass" ||
                    p == "flaxfibers" || Family(p, "plantdebris") || Family(p, "fruittree") ||
                    Family(p, "leaves") || Family(p, "leavesbranchy") || Family(p, "leavesbranchystatic") ||
                    Family(p, "leavesnarrow") || Family(p, "fallenleaves") || Family(p, "bambooleaves") ||
                    Family(p, "smallberrybush") || Family(p, "bigberrybush") || Family(p, "fruitingbush") ||
                    Family(p, "fern") || Family(p, "tallfern") || Family(p, "ferntree") ||
                    Family(p, "tallplant") || Family(p, "tallgrass") || Family(p, "wildvine") ||
                    Family(p, "wildvinestatic") || Family(p, "waterlily") || Family(p, "aquaticplant") ||
                    Family(p, "silvertorchcactus") || Family(p, "saguarocactus") || Family(p, "barrelcactus") ||
                    Family(p, "lichen") || Family(p, "hanginglichen") || Family(p, "hay")) return "plants";
                if (p == "flint" || p == "compost" || Family(p, "rawbrick") || Family(p, "burnedbrick") ||
                    Family(p, "refractorybrick") || p == "quicklime" || p == "lime" || p == "mortar") return "stone";
                if (Family(p, "backpack") || Family(p, "hunterbackpack") || Family(p, "miningbag") ||
                    Family(p, "miningbagsturdy") || Family(p, "linensack") || Family(p, "quiver") ||
                    Family(p, "glider") || Family(p, "basket")) return "clothing";
                if (Family(p, "bugnet") || Family(p, "plumbandsquare") || Family(p, "punchset") ||
                    Family(p, "inkandquill") || Family(p, "fishinghook") || Family(p, "padlock") || p == "sewingkit" ||
                    Family(p, "arrow") || Family(p, "bullets") || Family(p, "arrowhead") || Family(p, "axehead") ||
                    Family(p, "bladehead") || Family(p, "cleaverhead") || Family(p, "hammerhead") ||
                    Family(p, "helvehammerhead") || Family(p, "hoehead") || Family(p, "knifeblade") ||
                    Family(p, "pickaxehead") || Family(p, "prospectingpickhead") || Family(p, "sawblade") ||
                    Family(p, "scythehead") || Family(p, "shovelhead") || Family(p, "spearhead")) return "tools";
            }

            // Food metadata on liquid portions is nested, and the vessel's own
            // perish transitions do not describe its contents.
            if (stack.Collectible is BlockLiquidContainerBase liquid)
            {
                ItemStack[] contents = liquid.GetContents(world, stack.Clone());
                int slot = liquid.GetContainerSlotId(stack);
                ItemStack content = contents == null || contents.Length == 0 || slot < 0 ? null : contents[Math.Min(slot, contents.Length - 1)];
                string food = FoodCategory(world, content);
                if (food != null) return food;
            }
            if (stack.Collectible is BlockPie) return "food-perishable";
            if (stack.Collectible is BlockMeal || stack.Collectible is BlockCrock)
            {
                ItemStack[] contents = ((BlockContainer)stack.Collectible).GetContents(world, stack.Clone());
                string result = null;
                if (contents != null)
                    foreach (ItemStack content in contents)
                    {
                        string food = FoodCategory(world, content);
                        if (food == "food-perishable") return food;
                        if (food != null) result = food;
                    }
                if (result != null) return result;
            }
            var attributes = stack.Collectible.Attributes;
            var liquidProps = attributes == null ? null : attributes["waterTightContainerProps"];
            if (liquidProps != null && (liquidProps["nutritionPropsPerLitre"].Exists || liquidProps["nutritionPropsPerLitreWhenInMeal"].Exists))
                return Perishes(world, stack) ? "food-perishable" : "food-preserved";
            return null;
        }

        private static string FoodCategory(IWorldAccessor world, ItemStack stack)
        {
            if (stack == null || stack.Collectible == null) return null;
            // Contents are portions/ingredients; do not recurse into arbitrary containers.
            if (stack.Collectible is BlockContainer) return null;
            string special = Override(world, stack);
            if (special == "food-perishable" || special == "food-preserved") return special;
            var col = stack.Collectible;
            var attrs = col.Attributes;
            bool food = col.NutritionProps != null || col.GetNutritionProperties(world, stack, null) != null ||
                (attrs != null && (attrs["nutritionPropsWhenInMeal"].Exists || attrs["nutritionPropsWhenInMealByType"].Exists));
            return food ? (Perishes(world, stack) ? "food-perishable" : "food-preserved") : null;
        }

        private static bool Perishes(IWorldAccessor world, ItemStack stack)
        {
            var transitions = stack.Collectible.GetTransitionableProperties(world, stack, null);
            if (transitions != null)
                foreach (var transition in transitions)
                    if (transition != null && transition.Type == EnumTransitionType.Perish) return true;
            return false;
        }

        private static bool Family(string path, string family)
        {
            return path == family || path.StartsWith(family + "-", StringComparison.Ordinal);
        }
    }
}
