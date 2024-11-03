using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// TODO : add a special drag and drop to assign local variants
// DONE : override/revert in children, siblings and parents
// TODO : prefabs workflow

namespace Unity.LocalMaterialVariants.Editor
{
    using Editor = UnityEditor.Editor;

    [CustomEditor(typeof(MeshRenderer))]
    public class MeshRendererMaterialOverrideEditor : Editor
    {
        private static bool ExpandOverrides
        {
            get => EditorPrefs.GetBool("MeshRendererMaterialOverrideEditor.ExpandOverrides", true);
            set => EditorPrefs.SetBool("MeshRendererMaterialOverrideEditor.ExpandOverrides", value);
        }

        private Editor defaultEditor;
        private MeshRenderer meshRenderer;

        private void OnEnable()
        {
            defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.MeshRendererEditor, UnityEditor"));
            meshRenderer = (MeshRenderer)target;
        }

        public override void OnInspectorGUI()
        {
            ExpandOverrides = EditorGUILayout.BeginFoldoutHeaderGroup(ExpandOverrides, "Overrides");

            if (ExpandOverrides)
                MaterialOverridesGUI();

            EditorGUILayout.EndFoldoutHeaderGroup();

            defaultEditor.OnInspectorGUI();
        }

        private void MaterialOverridesGUI()
        {
            for (int i = 0; i < meshRenderer.sharedMaterials.Length; ++i)
            {
                if (meshRenderer.sharedMaterials[i] == null)
                    continue;

                MaterialGUI(i);
            }
        }

        private void ToggleMaterialOverride(int index, bool overrideMaterial)
        {
            var references = GetMaterialReferences(meshRenderer.transform.root, meshRenderer.sharedMaterials[index]);
            var overrideAll = references.Count > 1 && EditorUtility.DisplayDialog("Apply", string.Format("The same material is used in other renderers of the hierarchy.\nDo you want to {0} them too.", overrideMaterial ? "override" : "revert"), "all", "selection only");

            var newMaterial = overrideMaterial ? meshRenderer.sharedMaterials[index].CreateVariant(meshRenderer.transform.root.name) :
                meshRenderer.sharedMaterials[index].parent?? meshRenderer.sharedMaterials[index];

            if (overrideAll)
            {
                Undo.RecordObjects(references.Keys.ToArray(), overrideMaterial? "Override Materials" : "Revert Materials");
                foreach (var kvp in references)
                    kvp.Key.ReplaceMaterials(kvp.Value.ToArray(), newMaterial);
            }
            else
            {
                Undo.RecordObject(meshRenderer, overrideMaterial ? "Override Material" : "Revert Material");
                meshRenderer.ReplaceMaterial(index, newMaterial);
            }
        }

        private void MaterialGUI(int index)
        {
            EditorGUILayout.BeginHorizontal();
            if (MaterialToggle(meshRenderer.sharedMaterials[index], out bool overrideMaterial))
                ToggleMaterialOverride(index, overrideMaterial);

            if (EditorGUILayout.DropdownButton(new GUIContent(""), FocusType.Keyboard, GUILayout.Width(20f)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Shader"), false, DoSomething, index);
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DoSomething(object index)
        {
            Debug.Log(meshRenderer.sharedMaterials[(int)index].shader.name);
        }

        private bool MaterialToggle(Material material, out bool overrideMaterial)
        {
            bool isOverriden = material.IsLocal();
            EditorGUI.BeginChangeCheck();
            overrideMaterial = GUILayout.Toggle(isOverriden, isOverriden ? material.parent.name : material.name, EditorStyles.miniButton);
            return EditorGUI.EndChangeCheck();
        }

        private Dictionary<MeshRenderer, List<int>> GetMaterialReferences(Transform root, Material material)
        {
            Dictionary<MeshRenderer, List<int>> dict = new();

            var allRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in allRenderers)
            {
                if (Array.IndexOf(renderer.sharedMaterials, material) == -1)
                    continue;
                
                dict.Add(renderer, new List<int>());
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == material)
                        dict[renderer].Add(i);
                }
            }

            return dict;
        }
    }

    internal static class Extensions
    {
        internal static Material CreateVariant(this Material material, string prefix = default)
        {
            var variant = new Material(material);
            variant.name = $"{prefix}.{material.name}";
            variant.parent = material;
            material = variant;
            return variant;
        }

        internal static bool IsLocal(this Material material)
        {
            return string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material));
        }
        
        internal static void ReplaceMaterial(this Renderer renderer, int index, Material material)
        {
            var sharedMaterials = renderer.sharedMaterials;
            sharedMaterials[index] = material;
            renderer.sharedMaterials = sharedMaterials;
        }

        internal static void ReplaceMaterials(this Renderer renderer, int[] indices, Material material)
        {
            var sharedMaterials = renderer.sharedMaterials;
            for (int i = 0; i < indices.Length; i++)
                sharedMaterials[indices[i]] = material;
            renderer.sharedMaterials = sharedMaterials;
        }
    }
}
