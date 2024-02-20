using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unifind.Internal
{
    public static class MiscFinders
    {
        [FuzzyFinderMethod]
        public static async void OpenScene()
        {
            var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
            var scenePaths = new List<string>();
            var entries = new List<FuzzyFinderEntry<string>>();

            foreach (string guid in sceneGUIDs)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guid);

                if (!filePath.StartsWith("Assets/"))
                {
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(filePath);

                entries.Add(new FuzzyFinderEntry<string>(name: fileName, value: filePath));
            }

            var choice = await FuzzyFinderWindow.Select("Open Scene", entries);

            if (choice != null)
            {
                var scenePath = choice.Value;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
        }

        [FuzzyFinderMethod(Name = "Files - All")]
        public static void SelectFileFromAll()
        {
            SelectFile(null);
        }

        [FuzzyFinderMethod(Name = "Files - MonoBehaviour")]
        public static void SelectFileMonoBehaviour()
        {
            SelectFile("t:MonoScript");
        }

        [FuzzyFinderMethod(Name = "Files - Texture")]
        public static void SelectFileTexture()
        {
            SelectFile("t:Texture");
        }

        [FuzzyFinderMethod(Name = "Files - Shader")]
        public static void SelectFileShader()
        {
            SelectFile("t:Shader");
        }

        [FuzzyFinderMethod(Name = "Files - Animation")]
        public static void SelectFileAnimation()
        {
            SelectFile("t:Animation");
        }

        [FuzzyFinderMethod(Name = "Files - Animator")]
        public static void SelectFileAnimator()
        {
            SelectFile("t:Animator");
        }

        [FuzzyFinderMethod(Name = "Files - Prefab")]
        public static void SelectFilePrefab()
        {
            SelectFile("t:Prefab");
        }

        [FuzzyFinderMethod(Name = "Files - AudioClip")]
        public static void SelectFileAudioClip()
        {
            SelectFile("t:AudioClip");
        }

        [FuzzyFinderMethod(Name = "Files - ScriptableObject")]
        public static void SelectFileScriptableObject()
        {
            SelectFile("t:ScriptableObject");
        }

        static async void SelectFile(string? filter)
        {
            string[] guids = AssetDatabase.FindAssets(filter);

            var entries = new List<FuzzyFinderEntry<string>>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.StartsWith("Assets/"))
                {
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(path);

                entries.Add(
                    new FuzzyFinderEntry<string>(
                        name: fileName,
                        value: path,
                        summary: path
                    )
                );
            }

            var title = string.Format("Files ({0})", filter);
            var chosenPath = (await FuzzyFinderWindow.Select(title, entries))?.Value;

            if (chosenPath == null)
            {
                // cancelled
            }
            else
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(chosenPath);

                if (obj == null)
                {
                    Log.Error("Expected to find file at path: {0}", chosenPath);
                }
                else
                {
                    Selection.activeObject = obj;
                }
            }
        }

        [FuzzyFinderMethod(Name = "Select Scene Object - MonoBehaviour")]
        public static void SelectSceneObjectMonoBehaviour()
        {
            SelectSceneObject<MonoBehaviour>(SceneObjectDisplayMode.TypeName);
        }

        [FuzzyFinderMethod(Name = "Select Scene Object - Transform")]
        public static void SelectSceneObjectTransform()
        {
            SelectSceneObject<Transform>(SceneObjectDisplayMode.GameObjectName);
        }

        [FuzzyFinderMethod(Name = "Select Scene Object - Camera")]
        public static void SelectSceneObjectCamera()
        {
            SelectSceneObject<Camera>(SceneObjectDisplayMode.GameObjectName);
        }

        enum SceneObjectDisplayMode
        {
            GameObjectName,
            TypeName,
        }

        static async void SelectSceneObject<T>(SceneObjectDisplayMode displayMode)
            where T : Component
        {
            var entries = new List<FuzzyFinderEntry<T>>();

#if UNITY_2023_1_OR_NEWER
            foreach (var obj in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
#else
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<T>())
#endif
            {
                string name;

                switch (displayMode)
                {
                    case SceneObjectDisplayMode.GameObjectName:
                    {
                        name = obj.gameObject.name;
                        break;
                    }
                    case SceneObjectDisplayMode.TypeName:
                    {
                        name = obj.GetType().Name;
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }

                entries.Add(new(name: name, value: obj));
            }

            var title = string.Format("Scene Heirarchy - {0}", typeof(T).Name);
            var chosenObj = (await FuzzyFinderWindow.Select(title, entries))?.Value;

            if (chosenObj != null)
            {
                Selection.activeGameObject = chosenObj.gameObject;
            }
        }

        [FuzzyFinderMethod]
        public static async void ChangePlatform()
        {
            var entries = new List<FuzzyFinderEntry<BuildTarget>>();

            foreach (var buildTarget in (BuildTarget[])System.Enum.GetValues(typeof(BuildTarget)))
            {
                // Create a local copy of the loop variable to avoid capturing issue in lambda
                BuildTarget localBuildTarget = buildTarget;
                BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(
                    localBuildTarget
                );

                if (!BuildPipeline.IsBuildTargetSupported(buildTargetGroup, localBuildTarget))
                {
                    continue;
                }

                entries.Add(
                    new( name: localBuildTarget.ToString(), value: localBuildTarget )
                );
            }

            var chosenTarget = (await FuzzyFinderWindow.Select("Change Platform", entries))?.Value;

            if (chosenTarget == null)
            {
                // cancelled
                return;
            }

            var chosenTargetGroup = BuildPipeline.GetBuildTargetGroup(chosenTarget.Value);

            // Use the appropriate method based on Unity version
            bool switchSuccess = EditorUserBuildSettings.SwitchActiveBuildTarget(
                chosenTargetGroup,
                chosenTarget.Value
            );

            if (switchSuccess)
            {
                Log.Debug("Successfully switched to platform {0}", chosenTarget.Value);
            }
            else
            {
                Log.Error(
                    "Failed to switch to {0}. Please check if the target platform is installed.",
                    chosenTarget.Value
                );
            }
        }

        static GameObject CreateGameObject(GameObjectTypes choice)
        {
            switch (choice)
            {
                case GameObjectTypes.Empty:
                {
                    return new GameObject("New GameObject");
                }
                case GameObjectTypes.Cube:
                {
                    return GameObject.CreatePrimitive(PrimitiveType.Cube);
                }
                case GameObjectTypes.Sphere:
                {
                    return GameObject.CreatePrimitive(PrimitiveType.Sphere);
                }
                case GameObjectTypes.Capsule:
                {
                    return GameObject.CreatePrimitive(PrimitiveType.Capsule);
                }
                case GameObjectTypes.Cylinder:
                {
                    return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                }
                case GameObjectTypes.Plane:
                {
                    return GameObject.CreatePrimitive(PrimitiveType.Plane);
                }
                case GameObjectTypes.Camera:
                {
                    return new GameObject("Camera").AddComponent<Camera>().gameObject;
                }
                case GameObjectTypes.Light:
                {
                    var light = new GameObject("Light").AddComponent<Light>();
                    light.type = LightType.Directional;
                    return light.gameObject;
                }
                default:
                {
                    throw new System.NotImplementedException();
                }
            }
        }

        [FuzzyFinderMethod(Name = "Create Game Object")]
        public static async void ChooseCreateGameObject()
        {
            GameObjectTypes? choice = await FuzzyFinderWindow.Select(
                "Create GameObject",
                Enum.GetValues(typeof(GameObjectTypes)).Cast<GameObjectTypes>()
            );

            if (choice != null)
            {
                var obj = CreateGameObject(choice.Value);
                Selection.activeGameObject = obj;
            }
        }

        static async Task<string?> TryChooseAssetWithLabel(string title, string assetLabel)
        {
            var entries = new List<FuzzyFinderEntry<string>>();

            string[] guids = AssetDatabase.FindAssets(assetLabel);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileNameWithoutExtension(path);

                entries.Add(
                    new( name: fileName, value: path, summary: path)
                );
            }

            return (
                await FuzzyFinderWindow.Select(title, entries)
            )?.Value;
        }

        [FuzzyFinderMethod]
        public static async void ChangeEditorLayout()
        {
            // Note that we assume here that user manually sets asset labels with "EditorLayout"
            var chosenPath = await TryChooseAssetWithLabel("Change Editor Layout", "l:EditorLayout");

            if (chosenPath != null)
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(chosenPath);
                if (obj == null)
                {
                    Log.Error("Expected to find file at path: {0}", chosenPath);
                }
                else
                {
                    EditorUtility.LoadWindowLayout(chosenPath);
                }
            }
        }

        static async Task<UnityEngine.Object?> TryCreateFile(CreateFileTypes createType)
        {
            switch (createType)
            {
                case CreateFileTypes.MonoBehaviour:
                {
                    var chosenPath = await TryChooseAssetWithLabel("Choose C# Template", "l:MonoBehaviourTemplate");
                    var newScriptPath = "Assets/NewBehaviourScript.cs";

                    ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                        chosenPath,
                        newScriptPath
                    );

                    return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newScriptPath);
                }
                case CreateFileTypes.Shader:
                {
                    var chosenPath = await TryChooseAssetWithLabel("Choose Shader Template", "l:ShaderTemplate");
                    var newScriptPath = "Assets/NewShader.shader";

                    ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                        chosenPath,
                        newScriptPath
                    );

                    return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newScriptPath);
                }
                case CreateFileTypes.Material:
                {
                    var material = new Material(Shader.Find("Standard"));
                    AssetDatabase.CreateAsset(material, "Assets/NewMaterial.mat");
                    return material;
                }
                case CreateFileTypes.Texture:
                {
                    var texture = new Texture2D(1, 1);
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    AssetDatabase.CreateAsset(texture, "Assets/NewTexture.png");
                    return texture;
                }
                case CreateFileTypes.Prefab:
                {
                    var go = new GameObject("NewPrefab");
                    PrefabUtility.SaveAsPrefabAsset(go, "Assets/NewPrefab.prefab");
                    return go;
                }
                default:
                {
                    throw new System.NotImplementedException();
                }
            }
        }

        [FuzzyFinderMethod(Name = "Create File")]
        public static async void ChooseCreateFile()
        {
            var choice = await FuzzyFinderWindow.Select(
                "Create File",
                Enum.GetValues(typeof(CreateFileTypes)).Cast<CreateFileTypes>()
            );

            var obj = await TryCreateFile(choice);

            if (obj != null)
            {
                Selection.activeObject = obj;
            }
        }

        enum CreateFileTypes
        {
            MonoBehaviour,
            Shader,
            Material,
            Texture,
            Prefab,
        }

        enum GameObjectTypes
        {
            Empty,
            Cube,
            Sphere,
            Capsule,
            Cylinder,
            Plane,
            Camera,
            Light,
        }
    }
}
