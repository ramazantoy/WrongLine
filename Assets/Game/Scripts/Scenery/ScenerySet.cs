using System;
using System.Collections.Generic;

namespace WrongLine.Scenery
{
    /// <summary>
    /// The catalog flattened for a run: every prop gets an id (shared ones first, then each biome's in order), the same index
    /// in <see cref="Rules"/>, <see cref="Prefabs"/> and the pool.
    /// </summary>
    public sealed class ScenerySet
    {
        private ScenerySet(List<PropRule> rules, List<SceneryPropView> prefabs, List<BiomeDef> biomes)
        {
            Rules = rules;
            Prefabs = prefabs;
            Biomes = biomes;
        }

        public IReadOnlyList<PropRule> Rules { get; }

        public IReadOnlyList<SceneryPropView> Prefabs { get; }

        public IReadOnlyList<BiomeDef> Biomes { get; }

        /// <exception cref="ArgumentException">The catalog has no biome, or a prop without a prefab.</exception>
        public static ScenerySet From(SceneryCatalog catalog)
        {
            if (catalog.Biomes.Count == 0)
            {
                throw new ArgumentException("The scenery catalog has no biome.", nameof(catalog));
            }

            var rules = new List<PropRule>();
            var prefabs = new List<SceneryPropView>();
            Add(catalog.Shared, PropRule.Shared, rules, prefabs);
            for (int biome = 0; biome < catalog.Biomes.Count; biome++)
            {
                Add(catalog.Biomes[biome].Props, biome, rules, prefabs);
            }

            return new ScenerySet(rules, prefabs, new List<BiomeDef>(catalog.Biomes));
        }

        private static void Add(IReadOnlyList<PropEntry> entries, int biome, List<PropRule> rules, List<SceneryPropView> prefabs)
        {
            foreach (PropEntry entry in entries)
            {
                if (entry.Prefab == null)
                {
                    throw new ArgumentException($"Scenery prop '{entry.Name}' has no prefab (run WrongLine/Scenery/Rebuild Props).");
                }

                rules.Add(entry.ToRule(rules.Count, biome));
                prefabs.Add(entry.Prefab);
            }
        }
    }
}
