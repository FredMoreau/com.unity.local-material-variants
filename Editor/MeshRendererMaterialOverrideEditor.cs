using System;
using UnityEngine;
using UnityEditor;

// TODO : add a special drag and drop to assign local variants
// TODO : override/revert in children, siblings and parents
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
            var sharedMaterials = meshRenderer.sharedMaterials;

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < sharedMaterials.Length; ++i)
            {
                if (sharedMaterials[i] == null)
                    continue;

                MaterialGUI(ref sharedMaterials[i]);
            }

            if (EditorGUI.EndChangeCheck())
                meshRenderer.sharedMaterials = sharedMaterials;
        }

        private void MaterialGUI(ref Material material)
        {
            bool isOverriden = IsLocal(material);

            EditorGUI.BeginChangeCheck();
            bool overrideMaterial = GUILayout.Toggle(isOverriden, isOverriden ? material.parent.name : material.name, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(meshRenderer, isOverriden ? "Revert Renderer Material" : "Override Renderer Material");
                if (overrideMaterial)
                {
                    MakeLocalVariant(ref material, meshRenderer.gameObject.name);
                }
                else
                {
                    RevertToParent(ref material);
                }
            }
        }

        private static void RevertToParent(ref Material material)
        {
            if (material.parent == null)
                Debug.LogWarning("Local Material has no parent.");
            else
                material = material.parent;
        }

        private static void MakeLocalVariant(ref Material material, string prefix = default)
        {
            var variant = new Material(material);
            variant.name = $"{prefix}.{material.name}";
            variant.parent = material;
            material = variant;
        }

        private static bool IsLocal(Material material)
        {
            return string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material));
        }
    }
}
