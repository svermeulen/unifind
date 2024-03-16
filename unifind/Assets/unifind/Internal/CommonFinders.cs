using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unifind.Internal;
namespace Unifind
{
    public static class CommonFinders
    {
        [FuzzyFinderAction(GroupId = "UnifindExample")]
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

            var choice = await FuzzyFinder.UserSelect("Open Scene", entries);

            if (choice != null)
            {
                var scenePath = choice.Value;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
        }

        [FuzzyFinderAction(Name = "Files - All", GroupId = "UnifindExample")]
        public static void SelectFileFromAll()
        {
            SelectFile(null);
        }

        [FuzzyFinderAction(Name = "Files - MonoBehaviour", GroupId = "UnifindExample")]
        public static void SelectFileMonoBehaviour()
        {
            SelectFile("t:MonoScript");
        }

        [FuzzyFinderAction(Name = "Files - Texture", GroupId = "UnifindExample")]
        public static void SelectFileTexture()
        {
            SelectFile("t:Texture");
        }

        [FuzzyFinderAction(Name = "Files - Shader", GroupId = "UnifindExample")]
        public static void SelectFileShader()
        {
            SelectFile("t:Shader");
        }

        [FuzzyFinderAction(Name = "Files - Animation", GroupId = "UnifindExample")]
        public static void SelectFileAnimation()
        {
            SelectFile("t:Animation");
        }

        [FuzzyFinderAction(Name = "Files - Animator", GroupId = "UnifindExample")]
        public static void SelectFileAnimator()
        {
            SelectFile("t:Animator");
        }

        [FuzzyFinderAction(Name = "Files - Prefab", GroupId = "UnifindExample")]
        public static void SelectFilePrefab()
        {
            SelectFile("t:Prefab");
        }

        [FuzzyFinderAction(Name = "Files - AudioClip", GroupId = "UnifindExample")]
        public static void SelectFileAudioClip()
        {
            SelectFile("t:AudioClip");
        }

        [FuzzyFinderAction(Name = "Files - ScriptableObject", GroupId = "UnifindExample")]
        public static void SelectFileScriptableObject()
        {
            SelectFile("t:ScriptableObject");
        }

        public static async void SelectFile(string? filter)
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

                entries.Add(
                    new FuzzyFinderEntry<string>(
                        name: Path.GetFileName(path),
                        value: path,
                        summary: path
                    )
                );
            }

            var title = string.Format("Files ({0})", filter);
            var chosenPath = (await FuzzyFinder.UserSelect(title, entries))?.Value;

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

        [FuzzyFinderAction(Name = "Select Scene Object - MonoBehaviour", GroupId = "UnifindExample")]
        public static void SelectSceneObjectMonoBehaviour()
        {
            SelectSceneObject<MonoBehaviour>(SceneObjectDisplayMode.TypeName);
        }

        [FuzzyFinderAction(Name = "Select Scene Object - Transform", GroupId = "UnifindExample")]
        public static void SelectSceneObjectTransform()
        {
            SelectSceneObject<Transform>(SceneObjectDisplayMode.GameObjectName);
        }

        [FuzzyFinderAction(Name = "Select Scene Object - Camera", GroupId = "UnifindExample")]
        public static void SelectSceneObjectCamera()
        {
            SelectSceneObject<Camera>(SceneObjectDisplayMode.GameObjectName);
        }

        public enum SceneObjectDisplayMode
        {
            GameObjectName,
            TypeName,
        }

        public static async void SelectSceneObject<T>(SceneObjectDisplayMode displayMode)
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
            var chosenObj = (await FuzzyFinder.UserSelect(title, entries))?.Value;

            if (chosenObj != null)
            {
                Selection.activeGameObject = chosenObj.gameObject;
            }
        }

        [FuzzyFinderAction(Name = "Change Platform", GroupId = "UnifindExample")]
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

            var chosenTarget = (await FuzzyFinder.UserSelect("Change Platform", entries))?.Value;

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

        public static GameObject CreateGameObject(GameObjectTypes choice)
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

        [FuzzyFinderAction(Name = "Create Game Object", GroupId = "UnifindExample")]
        public static async void ChooseCreateGameObject()
        {
            var choice = await FuzzyFinder.UserSelect(
                "Create GameObject",
                Enum.GetValues(typeof(GameObjectTypes)).Cast<GameObjectTypes?>()
            );

            if (choice != null)
            {
                var obj = CreateGameObject(choice.Value);
                Undo.RegisterCreatedObjectUndo(obj, "Created Game Object");
                Selection.activeGameObject = obj;
            }
        }

        public static async Task<string?> TryChooseAssetWithLabel(string title, string assetLabel)
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
                await FuzzyFinder.UserSelect(title, entries)
            )?.Value;
        }

        [FuzzyFinderAction(Name = "Change Editor Layout", GroupId = "UnifindExample")]
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

        public static async Task<UnityEngine.Object?> TryCreateFile(CreateFileTypes createType)
        {
            switch (createType)
            {
                case CreateFileTypes.MonoBehaviour:
                {
                    var chosenPath = await TryChooseAssetWithLabel("Choose C# Template", "l:MonoBehaviourTemplate");

                    if (chosenPath == null)
                    {
                        return null;
                    }

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

                    if (chosenPath == null)
                    {
                        return null;
                    }

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

        [FuzzyFinderAction(Name = "Create File", GroupId = "UnifindExample")]
        public static async void ChooseCreateFile()
        {
            var choice = await FuzzyFinder.UserSelect(
                "Create File",
                Enum.GetValues(typeof(CreateFileTypes)).Cast<CreateFileTypes?>()
            );

            if (choice == null)
            {
                return;
            }

            var obj = await TryCreateFile(choice.Value);

            if (obj == null)
            {
                return;
            }

            Selection.activeObject = obj;
        }

        public enum CreateFileTypes
        {
            MonoBehaviour,
            Shader,
            Material,
            Texture,
            Prefab,
        }

        public enum GameObjectTypes
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
