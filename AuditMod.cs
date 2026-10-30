using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using storagehub;
using TemporalStorageFilterFix;

namespace TemporalFilterAudit
{
    public sealed class AuditSystem : ModSystem
    {
        public override double ExecuteOrder() { return 999; }
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.ServerRunPhase(EnumServerRunPhase.GameReady, () => RunAudit(api));
        }

        private void RunAudit(ICoreAPI api)
        {
            string output = Environment.GetEnvironmentVariable("TEMPORAL_FILTER_AUDIT_OUTPUT");
            if (string.IsNullOrEmpty(output) || api.Side != EnumAppSide.Server) return;
            var rows = new List<object>();
            var errors = new List<string>();
            var original = FilterFixSystem.OriginalCategories;
            var samples = new List<object>();
            int unchangedClassifications = 0;
            int changedClassifications = 0;
            foreach (CollectibleObject col in api.World.Collectibles)
            {
                if (col == null || col.Code == null) continue;
                ItemStack stack = col is Item item ? new ItemStack(item) : new ItemStack((Block)col);
                string[] before = Match(original, api.World, stack, errors);
                string[] after = Match(FilterCategories.All, api.World, stack, errors);
                if (before.SequenceEqual(after)) unchangedClassifications++; else changedClassifications++;
                bool nutrition = false;
                bool perishable = false;
                try { nutrition = col.GetNutritionProperties(api.World, stack, null) != null; } catch { }
                try { var t = col.GetTransitionableProperties(api.World, stack, null); perishable = t != null && t.Any(x => x != null && x.Type == EnumTransitionType.Perish); } catch { }
                rows.Add(new { Code = col.Code.ToString(), Class = col.GetType().FullName, ItemClass = col.ItemClass.ToString(), Before = before, After = after, Override = Rules.Override(api.World, stack), Nutrition = nutrition, Perishable = perishable });
                if (col.CreativeInventoryStacks != null)
                    foreach (var group in col.CreativeInventoryStacks)
                        if (group.Stacks != null)
                            foreach (var sample in group.Stacks)
                            {
                                if (sample.ResolvedItemstack == null) sample.Resolve(api.World, "temporal-filter-audit");
                                ItemStack resolved = sample.ResolvedItemstack;
                                if (resolved == null) continue;
                                samples.Add(new { Code = resolved.Collectible.Code.ToString(), Before = Match(original, api.World, resolved, errors), After = Match(FilterCategories.All, api.World, resolved, errors) });
                            }
            }
            var tests = new List<string>();
            Check(api.World, tests, "game:pumpkin-fruit-4", "plants");
            Check(api.World, tests, "game:vegetable-pumpkin", "food-perishable");
            foreach (var flour in api.World.Items.Where(x => x.Code != null && x.Code.Domain == "game" && x.Code.Path.StartsWith("flour-")))
                Check(api.World, tests, flour.Code.ToString(), "food-preserved");
            Check(api.World, tests, "game:rawcheese-salted", "food-perishable");
            Check(api.World, tests, "game:spice-licorice-raw", "food-perishable");
            Check(api.World, tests, "game:metalnailsandstrips-copper", "metals");
            Check(api.World, tests, "game:pie-perfect", "food-perishable");
            Check(api.World, tests, "game:plantdebris-leafy", "plants");
            Check(api.World, tests, "game:salt", "food-preserved");
            ItemStack bucket = Stack(api.World, "game:woodbucket");
            ItemStack milk = Stack(api.World, "game:milkportion");
            ItemStack honey = Stack(api.World, "game:honeyportion");
            if (bucket != null && milk != null && honey != null && bucket.Block is BlockLiquidContainerBase liquid)
            {
                Assert(!FilterCategories.Accepts(api.World, bucket, new [] { "food-perishable", "food-preserved" }), "Empty bucket is not food", tests);
                liquid.SetContent(bucket, milk);
                Assert(FilterCategories.Accepts(api.World, bucket, new [] { "food-perishable" }) && !FilterCategories.Accepts(api.World, bucket, new [] { "food-preserved" }), "Milk bucket is perishable", tests);
                liquid.SetContent(bucket, honey);
                Assert(FilterCategories.Accepts(api.World, bucket, new [] { "food-preserved" }) && !FilterCategories.Accepts(api.World, bucket, new [] { "food-perishable" }), "Honey bucket is preserved", tests);
                liquid.SetContent(bucket, Stack(api.World, "game:waterportion"));
                Assert(!FilterCategories.Accepts(api.World, bucket, new [] { "food-perishable", "food-preserved" }), "Water bucket is not food", tests);
            }
            else tests.Add("FAIL: Bucket fixture unavailable");
            Assert(!FilterCategories.Accepts(api.World, null, new [] { "plants" }), "Filtered null stack rejected", tests);
            Assert(FilterCategories.Accepts(api.World, null, new string[0]), "Unfiltered behavior preserved", tests);
            Check(api.World, tests, "game:axehead-copper", "tools");
            foreach (string code in new [] { "game:woodbucket", "game:pie-perfect", "game:pumpkin-fruit-4", "game:flour-rye" })
            {
                ItemStack fixture = Stack(api.World, code);
                fixture.StackSize = 3;
                fixture.Attributes.SetString("audit-trait", "preserve this exact value");
                byte[] serialized = fixture.ToBytes();
                foreach (FilterCategory category in FilterCategories.All) category.Matches(api.World, fixture);
                Assert(serialized.SequenceEqual(fixture.ToBytes()), "Classification preserves item data: " + code, tests);
            }
            if (api.ModLoader.IsModEnabled("butchering"))
                Check(api.World, tests, "butchering:fishingbait-blood", "animal");
            if (api.ModLoader.IsModEnabled("primitivesurvival"))
            {
                Check(api.World, tests, "primitivesurvival:trussedmeat-redmeat-raw", "food-perishable");
                Check(api.World, tests, "primitivesurvival:trussedmeat-redmeat-smoked", "food-preserved");
            }
            if (api.ModLoader.IsModEnabled("rustboundmagic"))
            {
                Check(api.World, tests, "rustboundmagic:inventorybag-bagofholding", "clothing");
                Check(api.World, tests, "rustboundmagic:armorbody-basic-plain-tier1", "clothing");
            }
            var secondInstance = new FilterFixSystem();
            secondInstance.Start(api);
            secondInstance.Dispose();
            Check(api.World, tests, "game:flour-rye", "food-preserved");
            File.WriteAllText(output, JsonConvert.SerializeObject(new { Count = rows.Count, Changed = changedClassifications, Unchanged = unchangedClassifications, Errors = errors, Tests = tests, Rows = rows, CreativeSamples = samples }, Formatting.Indented), Encoding.UTF8);
            Console.WriteLine("TEMPORAL_FILTER_AUDIT_COMPLETE " + rows.Count + " collectibles; " + errors.Count + " errors; " + output);
            // This separate test-only mod runs solely in the isolated audit server, before world generation.
            Environment.Exit(errors.Count == 0 && !tests.Any(x => x.StartsWith("FAIL:")) ? 0 : 2);
        }

        private static ItemStack Stack(IWorldAccessor world, string code)
        {
            Item item = world.GetItem(new AssetLocation(code));
            if (item != null) return new ItemStack(item);
            Block block = world.GetBlock(new AssetLocation(code));
            return block == null ? null : new ItemStack(block);
        }

        private static void Check(IWorldAccessor world, List<string> tests, string code, string category)
        {
            var stack = Stack(world, code);
            if (stack == null) { tests.Add("FAIL: Missing fixture " + code); return; }
            string[] actual = FilterCategories.All.Where(x => x.Matches(world, stack)).Select(x => x.Id).ToArray();
            Assert(actual.Length == 1 && actual[0] == category, code + " -> " + category + " (actual: " + string.Join(",", actual) + ")", tests);
        }

        private static void Assert(bool condition, string message, List<string> tests)
        {
            tests.Add((condition ? "PASS: " : "FAIL: ") + message);
        }

        private static string[] Match(FilterCategory[] categories, IWorldAccessor world, ItemStack stack, List<string> errors)
        {
            var matches = new List<string>();
            foreach (FilterCategory cat in categories)
            {
                try { if (cat.Matches(world, stack)) matches.Add(cat.Id); }
                catch (Exception ex) { errors.Add(stack.Collectible.Code + " / " + cat.Id + ": " + ex.GetType().Name + " " + ex.Message); }
            }
            return matches.ToArray();
        }
    }
}
