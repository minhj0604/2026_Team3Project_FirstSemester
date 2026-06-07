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

        private void Awake()
        {
            button = GetComponent<Button>();
            rectTransform = GetComponent<RectTransform>();
            button.onClick.AddListener(Execute);
        }

        private void Update()
        {
            if (GetComponent<DirectButtonClicker>() != null)
            {
                return;
            }

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

        private void LoadTargetScene()
        {
#if UNITY_EDITOR
            var editorScenePath = !string.IsNullOrWhiteSpace(scenePath)
                ? scenePath
                : $"Assets/Scenes/{sceneName}.unity";

            if (!string.IsNullOrWhiteSpace(sceneName) && Application.isEditor)
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                var fullPath = Path.IsPathRooted(editorScenePath)
                    ? editorScenePath
                    : Path.GetFullPath(Path.Combine(projectRoot ?? string.Empty, editorScenePath));
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"Scene file does not exist: {fullPath}. Check that the scene exists in Assets/Scenes.");
                    return;
                }

                EditorSceneManager.LoadSceneInPlayMode(fullPath, new LoadSceneParameters(LoadSceneMode.Single));
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
