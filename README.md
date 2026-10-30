# Temporal Storage Filter Fix 1.0.0

Local companion patch for Vintage Story 1.22.7 and Temporal Storage 2.1.3.

Keep TemporalStorage_2.1.3.zip installed. Install TemporalStorageFilterFix_1.0.0.zip alongside it and fully restart the game. Multiplayer servers and their clients should both install the companion patch.

Existing links, category IDs, hub tiers, and chest filters remain usable. Enable the relevant existing categories on the destination containers; no relinking is required by this patch.

## Requested mappings

| Item | Category |
|---|---|
| Whole pumpkins, all growth stages | Plants & Seeds |
| Pumpkin slices | Perishable Food |
| All seven vanilla flours | Preserved Food |

“Preserved Food” is a routing category. Flour retains its normal spoilage, nutrition, and item attributes.

## Additional corrections

- Perishable Food: whole raw fish, raw cheese, spices, dairy intermediates, pressed fruit mash, pies, and edible perishable liquid portions.
- Food containers: filled buckets, food bowls, and crocks are classified using their contents. Milk is perishable; honey is preserved. Empty buckets and water are not classified as food.
- Preserved Food: salt and nonperishable edible liquid portions.
- Plants & Seeds: previously missed fruit-tree cuttings, leaves, bushes, ferns, reeds, vines, roots, hay, flax fibers, and plant debris.
- Metals: chains, lamellae, scales, nails and strips, solder bars, and iron blooms.
- Wood & Lumber: aged firewood, oak bark, and bamboo stakes.
- Animal Products: antlers, tusks, and bonemeal.
- Stone & Earth: flint, compost, bricks, lime, and mortar.
- Clothing & Armor: wearable behaviors, bags, quivers, and gliders.
- Tools: previously missed nets, locks, sewing kits, fishing accessories, ammunition, and tool heads.

## Installed-mod support

- Alchemy: flower cuttings -> Plants & Seeds; mortars/pestles and stirring spoons -> Tools.
- Butchering: carcasses, ivory, sinew, strong bones, and blood bait -> Animal Products; butchery bags -> Clothing & Armor; hooks, needles, and tool heads -> Tools.
- Primitive Survival: raw whole fish, curing caviar, and raw trussed meat -> Perishable Food; smoked trussed meat -> Preserved Food; hooks and lures -> Tools. Live crabs, snakes, and other captured creatures retain their original classification.
- Substrate: spore prints and wet spore paper -> Plants & Seeds.
- Rustbound Magic: armor and inventory bags -> Clothing & Armor.
- Other mods' items using the standard wearable behavior or standard edible liquid metadata also benefit from the generic rules.

Unlisted items retain Temporal Storage's original predicates. Objects that have no reasonable match among the ten existing categories still need unfiltered storage. This is not a universal category system for every decorative object, machine, book, spell, or live creature.

## What the patch changes

It wraps the ten existing category predicates at startup. Specific corrected classifications take precedence; everything else falls through to the original predicates. It adds no inventory-transfer code, performs no world/save conversion, and does not run a background sorting task. Container inspection uses cloned stacks.

Removing the companion patch and restarting restores the original classifier. It does not undo item movements performed by Temporal Storage while the patch was active.

This patch addresses classification. It does not fix unrelated XSkills/XLib crashes or guarantee every custom inventory implementation works with Temporal Storage.

## Source and validation

The separate source archive includes the C# patch, build script, and a test-only audit mod. Do not install the source archive or audit mod in your normal game.

The build script uses the local game API, the installed Temporal Storage DLL, the installed .NET SDK C# compiler, and the PowerShell .NET runtime reference assemblies. No NuGet packages are downloaded. Its default paths match this computer; adjust them when building elsewhere.

The audit runs a separate localhost server with its own data folder. It enumerates loaded collectible definitions and creative sample stacks, checks category acceptance, and exits before world generation. See the accompanying audit report for the final counts and test results. Player interaction and chest-to-chest transfers in an existing save were not exercised.

## Source repository

[TemporalStorageFilterFix](https://github.com/tannerjriddle97/TemporalStorageFilterFix) contains the addon source, authoring files and available test sources. No open-source license has been selected for this source. Game/mod dependencies, local profiles, saves, recovery provenance and generated packages are excluded. Historical audit/probe scripts require a separate deliberate runtime-testing decision; publication does not run them. Interactive acceptance limits remain as documented.
