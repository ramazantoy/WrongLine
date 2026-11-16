using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WrongLine.Road.Tests
{
    /// <summary>Builds road assets in memory (recipe, prefab-like view, catalog, config) and destroys them all on dispose.</summary>
    internal sealed class RoadTestKit : IDisposable
    {
        private readonly List<Object> _created = new List<Object>();

        public T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        public RoadPieceView MakePrefab(string name, TrackRecipe recipe, bool isStart)
        {
            var asset = Track(ScriptableObject.CreateInstance<RoadPieceRecipe>());
            asset.name = name;
            var recipeObject = new SerializedObject(asset);
            recipeObject.FindProperty("_length").floatValue = recipe.Length;
            recipeObject.FindProperty("_yawDegrees").floatValue = recipe.YawDegrees;
            recipeObject.FindProperty("_yawProfile").enumValueIndex = (int)recipe.YawProfile;
            recipeObject.FindProperty("_pitchDegrees").floatValue = recipe.PitchDegrees;
            recipeObject.FindProperty("_pitchProfile").enumValueIndex = (int)recipe.PitchProfile;
            recipeObject.FindProperty("_bankDegrees").floatValue = recipe.BankDegrees;
            recipeObject.FindProperty("_isStart").boolValue = isStart;
            recipeObject.ApplyModifiedPropertiesWithoutUndo();

            var root = Track(new GameObject(name));
            var view = root.AddComponent<RoadPieceView>();
            var viewObject = new SerializedObject(view);
            viewObject.FindProperty("_recipe").objectReferenceValue = asset;
            viewObject.FindProperty("_bakedHash").intValue = recipe.StableHash();
            viewObject.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        public RoadCatalog MakeCatalog(RoadPieceView start, params RoadPieceView[] pieces)
        {
            var catalog = Track(ScriptableObject.CreateInstance<RoadCatalog>());
            var serialized = new SerializedObject(catalog);
            serialized.FindProperty("_start").objectReferenceValue = start;
            SerializedProperty list = serialized.FindProperty("_pieces");
            list.arraySize = pieces.Length;
            for (int i = 0; i < pieces.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        public RoadConfig MakeConfig(Action<SerializedObject> change = null)
        {
            var config = Track(ScriptableObject.CreateInstance<RoadConfig>());
            if (change != null)
            {
                var serialized = new SerializedObject(config);
                change(serialized);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return config;
        }

        public void Dispose()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }
    }
}
