using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private const string SelectedStageKey = "Team3.SelectedStage";
        private const string ClearedStageKeyPrefix = "Team3.ClearedStage.";

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
        private RectTransform rectTransform;
        private int lastExecuteFrame = -1;

        private void Awake()
        {
            button = GetComponent<Button>();
            rectTransform = GetComponent<RectTransform>();
            button.onClick.AddListener(Execute);
        }

        private void Start()
        {
            RefreshNamedButtonState();
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (button == null || !button.IsActive() || !button.interactable || rectTransform == null)
            {
                return;
            }

            var mousePosition = Mouse.current.position.ReadValue();
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition))
            {
                button.onClick.Invoke();
            }
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

                PlayerPrefs.SetInt(SelectedChapterKey, chapter);
                PlayerPrefs.SetInt(SelectedStageKey, stage);
                PlayerPrefs.Save();
                SceneManager.LoadScene("BattleScene");
                return true;
            }

            return false;
        }

        private void RefreshNamedButtonState()
        {
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
                var stageName = stage == 3 ? "보스" : $"{stage}스테이지";
                var state = stage <= clearedStage ? "완료" : stage == currentStage ? "진행" : "잠김";
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
