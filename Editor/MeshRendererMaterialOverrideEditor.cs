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
            var sharedMaterials = meshRenderer.sharedMaterials;

            ExpandOverrides = EditorGUILayout.BeginFoldoutHeaderGroup(ExpandOverrides, "Overrides");
            if (ExpandOverrides)
            {
                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < sharedMaterials.Length; ++i)
                {
                    if (sharedMaterials[i] == null)
                        continue;

                    bool isOverriden = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sharedMaterials[i]));

                    EditorGUI.BeginChangeCheck();
                    bool overrideMaterial = GUILayout.Toggle(isOverriden, isOverriden ? sharedMaterials[i].parent.name : sharedMaterials[i].name, EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(meshRenderer, isOverriden ? "Revert Renderer Material" : "Override Renderer Material");
                        if (overrideMaterial)
                        {
                            var variant = new Material(sharedMaterials[i]);
                            variant.name = $"{meshRenderer.gameObject.name}.{sharedMaterials[i].name}";
                            variant.parent = sharedMaterials[i];
                            sharedMaterials[i] = variant;
                        }
                        else
                        {
                            if (sharedMaterials[i].parent == null)
                                Debug.LogWarning("Local Material has no parent.");
                            else
                                sharedMaterials[i] = sharedMaterials[i].parent;
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck())
                    meshRenderer.sharedMaterials = sharedMaterials;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            defaultEditor.OnInspectorGUI();
        }
    }
}
