using System.IO;
using Team3Project.Dialogue;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Team3Project.UI
{
    [RequireComponent(typeof(Button))]
    public class SceneButton : MonoBehaviour
    {
        private const string SelectedChapterKey = "Team3.SelectedChapter";
        private const string ClearedStageKeyPrefix = "Team3.ClearedStage.";
        private static readonly Vector2Int[] ResolutionOptions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080)
        };

        private const string ResolutionIndexKey = "Team3.Settings.ResolutionIndex";
        private static int currentHelpPage;

        public enum ButtonAction
        {
            LoadScene,
            QuitGame,
            None
        }

        [SerializeField] private ButtonAction action = ButtonAction.None;
        [SerializeField] private string sceneName;
        [SerializeField] private string scenePath;

        private Button button;
        private int lastExecuteFrame = -1;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Execute);
        }

        private void Start()
        {
            RefreshNamedButtonState();
        }

        private void Execute()
        {
            if (lastExecuteFrame == Time.frameCount)
            {
                return;
            }

            lastExecuteFrame = Time.frameCount;
            if (TryExecuteNamedButton())
            {
                return;
            }

            switch (action)
            {
                case ButtonAction.LoadScene:
                    LoadTargetScene();
                    break;
                case ButtonAction.QuitGame:
                    Application.Quit();
                    break;
            }
        }

        private bool TryExecuteNamedButton()
        {
            var activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == "ChapterSelectScene")
            {
                if (name == "Previous Chapter Arrow")
                {
                    SceneFlowBootstrap.ChangeChapter(-1);
                    return true;
                }

                if (name == "Next Chapter Arrow")
                {
                    SceneFlowBootstrap.ChangeChapter(1);
                    return true;
                }

                if (name == "Enter Chapter Button")
                {
                    SceneFlowBootstrap.EnterSelectedChapter();
                    return true;
                }
            }

            if (activeScene == "StageMapScene" && TryGetStageFromName(name, out var stage))
            {
                var chapter = PlayerPrefs.GetInt(SelectedChapterKey, 1);
                var unlockedStage = GetCurrentStageForChapter(chapter);
                if (stage > unlockedStage)
                {
                    return true;
                }

                SceneFlowBootstrap.EnterStage(stage);
                return true;
            }

            if (TryExecutePopupButton(activeScene))
            {
                return true;
            }

            return false;
        }

        private bool TryExecutePopupButton(string activeScene)
        {
            if (activeScene == "TitleScene")
            {
                if (name == "Settings Button")
                {
                    SetPanelActive("Settings Panel", true);
                    RefreshSettingsLabels();
                    return true;
                }

                if (name == "Settings Close Button")
                {
                    SetPanelActive("Settings Panel", false);
                    return true;
                }

                if (name == "Resolution Previous Button")
                {
                    ChangeResolution(-1);
                    return true;
                }

                if (name == "Resolution Next Button")
                {
                    ChangeResolution(1);
                    return true;
                }

                if (name == "Fullscreen Toggle Button")
                {
                    Screen.fullScreen = !Screen.fullScreen;
                    RefreshSettingsLabels();
                    return true;
                }
            }

            if (activeScene == "BattleScene")
            {
                if (name == "Help Button")
                {
                    SetPanelActive("Battle Help Panel", true);
                    ShowHelpPage(0);
                    return true;
                }

                if (name == "Help Close Button")
                {
                    SetPanelActive("Battle Help Panel", false);
                    return true;
                }

                if (name == "Help Previous Page Button")
                {
                    ShowHelpPage(currentHelpPage - 1);
                    return true;
                }

                if (name == "Help Next Page Button")
                {
                    ShowHelpPage(currentHelpPage + 1);
                    return true;
                }
            }

            return false;
        }

        private void RefreshNamedButtonState()
        {
            RefreshSettingsLabels();
            if (button == null || SceneManager.GetActiveScene().name != "StageMapScene" || !TryGetStageFromName(name, out var stage))
            {
                return;
            }

            var chapter = PlayerPrefs.GetInt(SelectedChapterKey, 1);
            var clearedStage = PlayerPrefs.GetInt($"{ClearedStageKeyPrefix}{chapter}", 0);
            var currentStage = GetCurrentStageForChapter(chapter);
            button.interactable = stage <= currentStage;

            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                var stageName = stage == 3 ? "\uBCF4\uC2A4" : $"{stage}\uC2A4\uD14C\uC774\uC9C0";
                var state = stage <= clearedStage ? "\uC644\uB8CC" : stage == currentStage ? "\uC9C4\uD589" : "\uC7A0\uAE40";
                text.text = $"{stageName}\n{state}";
            }
        }

        private static bool TryGetStageFromName(string objectName, out int stage)
        {
            stage = 0;
            if (objectName == "Stage 1 Node")
            {
                stage = 1;
                return true;
            }

            if (objectName == "Stage 2 Node")
            {
                stage = 2;
                return true;
            }

            if (objectName == "Boss Stage Node")
            {
                stage = 3;
                return true;
            }

            return false;
        }

        private static int GetCurrentStageForChapter(int chapter)
        {
            var clearedStage = PlayerPrefs.GetInt($"{ClearedStageKeyPrefix}{chapter}", 0);
            return Mathf.Clamp(clearedStage + 1, 1, 3);
        }

        private static void ChangeResolution(int direction)
        {
            var index = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionIndexKey, 2) + direction, 0, ResolutionOptions.Length - 1);
            PlayerPrefs.SetInt(ResolutionIndexKey, index);
            PlayerPrefs.Save();

            var resolution = ResolutionOptions[index];
            Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
            RefreshSettingsLabels();
        }

        private static void RefreshSettingsLabels()
        {
            var index = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionIndexKey, 2), 0, ResolutionOptions.Length - 1);
            var resolution = ResolutionOptions[index];
            SetNamedText("Resolution Value Text", $"{resolution.x} x {resolution.y}");
            SetNamedText("Fullscreen Value Text", Screen.fullScreen ? "\uC804\uCCB4\uD654\uBA74 ON" : "\uC804\uCCB4\uD654\uBA74 OFF");
        }

        private static void ShowHelpPage(int page)
        {
            currentHelpPage = Mathf.Clamp(page, 0, 1);
            SetPanelActive("Help Body Text", currentHelpPage == 0);
            SetPanelActive("Help Resource Page", currentHelpPage == 1);
            SetPanelActive("Help Previous Page Button", currentHelpPage > 0);
            SetPanelActive("Help Next Page Button", currentHelpPage < 1);
            SetNamedText("Help Page Indicator Text", $"{currentHelpPage + 1} / 2");
        }

        private static void SetPanelActive(string objectName, bool active)
        {
            var panel = GameObject.Find(objectName);
            if (panel == null)
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var found = FindInactiveChild(root.transform, objectName);
                    if (found != null)
                    {
                        panel = found.gameObject;
                        break;
                    }
                }
            }

            panel?.SetActive(active);
        }

        private static Transform FindInactiveChild(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindInactiveChild(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void SetNamedText(string objectName, string value)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var found = FindInactiveChild(root.transform, objectName);
                    if (found != null)
                    {
                        target = found.gameObject;
                        break;
                    }
                }
            }

            var text = target == null ? null : target.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private void LoadTargetScene()
        {
#if UNITY_EDITOR
            var editorScenePath = !string.IsNullOrWhiteSpace(scenePath)
                ? scenePath
                : $"Assets/Scenes/{sceneName}.unity";

            if (!string.IsNullOrWhiteSpace(sceneName) && Application.isEditor)
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                var sceneAssetPath = Path.IsPathRooted(editorScenePath) && !string.IsNullOrEmpty(projectRoot)
                    ? Path.GetRelativePath(projectRoot, editorScenePath)
                    : editorScenePath;
                sceneAssetPath = sceneAssetPath.Replace('\\', '/');

                var fullPath = Path.IsPathRooted(editorScenePath)
                    ? editorScenePath
                    : Path.GetFullPath(Path.Combine(projectRoot ?? string.Empty, sceneAssetPath));
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"Scene file does not exist: {fullPath}. Check that the scene exists in Assets/Scenes.");
                    return;
                }

                EditorSceneManager.LoadSceneInPlayMode(sceneAssetPath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif

            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
