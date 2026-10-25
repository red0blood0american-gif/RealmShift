using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RealmShift.EditorTools
{
    /// <summary>
    /// Menu items and the batchmode entry point that rebuild both blockout scenes from the layout parameters.
    /// </summary>
    public static class RealmShiftBuild
    {
        const string RendererPath = "Assets/Settings/URP_Renderer.asset";
        const string PipelinePath = "Assets/Settings/URP_Pipeline.asset";

        public static int PlayerLayer => LayerMask.NameToLayer("Player");

        [MenuItem("RealmShift/Build Hub")]
        public static void BuildHubMenu()
        {
            EnsureProject();
            HubSceneBuilder.Build();
            RefreshBuildSettings();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("RealmShift/Build Frostfell Fair")]
        public static void BuildFrostfellMenu()
        {
            EnsureProject();
            FrostfellSceneBuilder.Build();
            RefreshBuildSettings();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("RealmShift/Build All")]
        public static void BuildAllMenu()
        {
            BuildAll();
        }

        public static void BuildAll()
        {
            EnsureProject();
            EditorApplication.LockReloadAssemblies();
            try
            {
                BlockoutFactory.ResetGeneratedMeshes();
                BlockoutFactory.EnsureFolder("Assets/Scenes");
                HubSceneBuilder.Build();
                FrostfellSceneBuilder.Build();
                RefreshBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log("RealmShift: Build All finished.");
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        /// <summary>Batchmode entry: -executeMethod RealmShift.EditorTools.RealmShiftBuild.BuildAllBatch</summary>
        public static void BuildAllBatch()
        {
            try
            {
                BuildAll();
            }
            catch (Exception e)
            {
                Debug.LogError("RealmShift build failed: " + e);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        public static void EnsureProject()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "RealmShift";
            PlayerSettings.productName = "RealmShift";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EnsureLayer("Player", 8);
            EnsurePipeline();
            EnsureInput();
        }

        public static void RefreshBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(RealmShiftScenes.HubPath, true),
                new EditorBuildSettingsScene(RealmShiftScenes.FrostfellPath, true)
            };
        }

        static void EnsureLayer(string name, int index)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
                throw new InvalidOperationException("TagManager.asset is missing.");
            var so = new SerializedObject(assets[0]);
            var layers = so.FindProperty("layers");
            var slot = layers.GetArrayElementAtIndex(index);
            if (slot.stringValue != name)
            {
                if (!string.IsNullOrEmpty(slot.stringValue) && slot.stringValue != name)
                    Debug.LogWarning("Replacing layer " + index + " '" + slot.stringValue + "' with " + name);
                slot.stringValue = name;
                so.ApplyModifiedProperties();
            }
        }

        static void EnsurePipeline()
        {
            BlockoutFactory.EnsureFolder("Assets/Settings");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var create = typeof(UniversalRenderPipelineAsset).GetMethod(
                    "CreateRendererAsset",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string), typeof(RendererType), typeof(bool), typeof(string) },
                    null);
                if (create == null)
                    throw new MissingMethodException("UniversalRenderPipelineAsset.CreateRendererAsset");
                var renderer = (ScriptableRendererData)create.Invoke(null, new object[] { RendererPath, RendererType.UniversalRenderer, false, "Renderer" });
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            pipeline.shadowDistance = 90f;
            pipeline.msaaSampleCount = (int)MsaaQuality.Disabled;
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;

            int levels = QualitySettings.names.Length;
            int previous = QualitySettings.GetQualityLevel();
            for (int i = 0; i < levels; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(previous, false);

            var qualityAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qualityAsset != null && qualityAsset.Length > 0)
            {
                var so = new SerializedObject(qualityAsset[0]);
                var list = so.FindProperty("m_QualitySettings");
                if (list != null)
                {
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        var custom = list.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline");
                        if (custom != null)
                            custom.objectReferenceValue = pipeline;
                    }
                    so.ApplyModifiedProperties();
                }
            }

            // The global-settings type is internal. Asset import usually creates it; call Ensure when we can see it.
            var settingsType = typeof(UniversalRenderPipelineAsset).Assembly.GetType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings");
            var ensure = settingsType?.GetMethod(
                "Ensure",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(bool) },
                null);
            if (ensure != null)
                ensure.Invoke(null, new object[] { true });
            else
                Debug.LogWarning("RealmShift: URP global settings Ensure() was not found.");
        }

        static void EnsureInput()
        {
            var project = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (project != null && project.Length > 0)
            {
                var so = new SerializedObject(project[0]);
                var handler = so.FindProperty("activeInputHandler");
                if (handler != null && handler.intValue != 2)
                {
                    handler.intValue = 2;
                    so.ApplyModifiedProperties();
                }
            }

            var input = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            if (input == null || input.Length == 0)
                return;
            var axesObject = new SerializedObject(input[0]);
            var axes = axesObject.FindProperty("m_Axes");
            EnsureAxis(axes, "RightStickX", 2, 3, false);
            EnsureAxis(axes, "RightStickY", 2, 4, false);
            axesObject.ApplyModifiedProperties();
        }

        static void EnsureAxis(SerializedProperty axes, string name, int type, int axis, bool invert)
        {
            for (int i = 0; i < axes.arraySize; i++)
            {
                if (axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == name)
                    return;
            }

            axes.arraySize++;
            var entry = axes.GetArrayElementAtIndex(axes.arraySize - 1);
            entry.FindPropertyRelative("m_Name").stringValue = name;
            entry.FindPropertyRelative("descriptiveName").stringValue = "";
            entry.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
            entry.FindPropertyRelative("negativeButton").stringValue = "";
            entry.FindPropertyRelative("positiveButton").stringValue = "";
            entry.FindPropertyRelative("altNegativeButton").stringValue = "";
            entry.FindPropertyRelative("altPositiveButton").stringValue = "";
            entry.FindPropertyRelative("gravity").floatValue = 0f;
            entry.FindPropertyRelative("dead").floatValue = 0.19f;
            entry.FindPropertyRelative("sensitivity").floatValue = 1f;
            entry.FindPropertyRelative("snap").boolValue = false;
            entry.FindPropertyRelative("invert").boolValue = invert;
            entry.FindPropertyRelative("type").intValue = type;
            entry.FindPropertyRelative("axis").intValue = axis;
            entry.FindPropertyRelative("joyNum").intValue = 0;
        }
    }
}
