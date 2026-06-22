using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Team3Project.GameSystems;

namespace Team3Project.UI
{
    public static class SceneFlowBootstrap
    {
        private const string SelectedChapterKey = "Team3.SelectedChapter";
        private const string SelectedStageKey = "Team3.SelectedStage";
        private const string ClearedStageKeyPrefix = "Team3.ClearedStage.";
        private const string UnlockedChapterKey = "Team3.UnlockedChapter";

        private static int selectedChapter = 1;
        private static GameObject titleReturnWarning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded += (_, _) => RunSetupAfterFrame();
            RunSetupAfterFrame();
        }

        private static void RunSetupAfterFrame()
        {
            SceneFlowBootstrapDriver.Instance.RunSetup();
        }

        internal static void Setup()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "ChapterSelectScene")
            {
                SetupChapterSelectScene();
            }
            else if (sceneName == "StageMapScene")
            {
                SetupStageMapScene();
            }
            else if (sceneName == "GameOverScene")
            {
                SetupGameOverScene();
            }
        }

        private static void SetupChapterSelectScene()
        {
            WireButton("Enter Chapter Button", EnterSelectedChapter);

            WireButton("Previous Chapter Arrow", () => ChangeChapter(-1));
            WireButton("Next Chapter Arrow", () => ChangeChapter(1));
            WireButton("Back Button", () => SceneManager.LoadScene("TitleScene"));
            EnsureChapterArrowFallback();
            RefreshChapterSelect();
        }

        public static void ChangeChapter(int direction)
        {
            selectedChapter = Mathf.Clamp(selectedChapter + direction, 1, 3);
            RefreshChapterSelect();
        }

        public static void EnterSelectedChapter()
        {
            if (selectedChapter > GetUnlockedChapter())
            {
                return;
            }

            BattleController.ResetChapterRun(selectedChapter);
            SceneManager.LoadScene("StageMapScene");
        }

        public static void RefreshChapterSelect()
        {
            SetText("Title Text", $"{selectedChapter}장");
            RefreshChapterBossImage(selectedChapter);
            var enter = FindButton("Enter Chapter Button");
            if (enter != null)
            {
                enter.interactable = selectedChapter <= GetUnlockedChapter();
            }

            var lockOne = GameObject.Find("Chapter Lock Hidden 1");
            if (lockOne != null)
            {
                lockOne.SetActive(selectedChapter > GetUnlockedChapter());
            }

            var lockTwo = GameObject.Find("Chapter Lock Hidden 2");
            if (lockTwo != null)
            {
                lockTwo.SetActive(selectedChapter > GetUnlockedChapter());
            }
        }

        private static void RefreshChapterBossImage(int chapter)
        {
            var target = GameObject.Find("Chapter Boss Image");
            if (target == null || !target.TryGetComponent<Image>(out var image))
            {
                return;
            }

            image.color = chapter <= GetUnlockedChapter() ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
        }

        private static void EnsureChapterArrowFallback()
        {
            if (Object.FindObjectOfType<ChapterArrowDirectClick>() != null)
            {
                return;
            }

            var inputObject = new GameObject("Chapter Arrow Direct Input");
            Object.DontDestroyOnLoad(inputObject);
            inputObject.AddComponent<ChapterArrowDirectClick>();
        }

        private static void SetupStageMapScene()
        {
            SetText("Title Text", $"{selectedChapter}장 스테이지");
            WireStageButton("Stage 1 Node", 1);
            WireStageButton("Stage 2 Node", 2);
            WireStageButton("Boss Stage Node", 3);
            WireButton("Back Button", () => SceneManager.LoadScene("ChapterSelectScene"));
        }

        private static void SetupGameOverScene()
        {
            EnsureTitleReturnWarning();
            WireButton("Title Button", ShowTitleReturnWarning);
        }

        private static void ShowTitleReturnWarning()
        {
            var dialog = EnsureTitleReturnWarning();
            dialog.SetActive(true);
            dialog.transform.SetAsLastSibling();
        }

        private static GameObject EnsureTitleReturnWarning()
        {
            if (titleReturnWarning != null)
            {
                return titleReturnWarning;
            }

            var existing = GameObject.Find("Title Return Warning");
            if (existing != null)
            {
                titleReturnWarning = existing;
                return existing;
            }

            var canvas = Object.FindObjectOfType<Canvas>();
            var parent = canvas == null ? null : canvas.transform;
            var overlay = new GameObject("Title Return Warning", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(parent, false);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.sprite = LoadUiSprite("경고창 화면.png");
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = true;
            overlayImage.preserveAspect = false;

            CreateDialogButton(overlay.transform, "Yes Button", "예 버튼.png", new Vector2(-340f, -280f), () => SceneManager.LoadScene("TitleScene"));
            CreateDialogButton(overlay.transform, "No Button", "아니요 버튼.png", new Vector2(340f, -280f), () => overlay.SetActive(false));

            overlay.SetActive(false);
            titleReturnWarning = overlay;
            return titleReturnWarning;
        }

        private static Button CreateDialogButton(Transform parent, string objectName, string spriteFileName, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(650f, 150f);
            rect.anchoredPosition = position;

            var image = buttonObject.GetComponent<Image>();
            image.sprite = LoadUiSprite(spriteFileName);
            image.color = Color.white;
            image.preserveAspect = true;

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            buttonObject.AddComponent<DirectButtonClicker>();
            return button;
        }

        private static Sprite LoadUiSprite(string fileName)
        {
            return LoadAssetSprite("GameOver Menu", fileName);
        }

        private static Sprite LoadAssetSprite(params string[] relativePathParts)
        {
            var pathParts = new string[relativePathParts.Length + 1];
            pathParts[0] = "Resource";
            for (var i = 0; i < relativePathParts.Length; i++)
            {
                pathParts[i + 1] = relativePathParts[i];
            }

            var path = Path.Combine(Application.dataPath, Path.Combine(pathParts));
            if (!File.Exists(path))
            {
                Debug.LogWarning($"UI sprite not found: {path}");
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                Debug.LogWarning($"Failed to load UI sprite: {path}");
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void WireStageButton(string objectName, int stage)
        {
            var button = FindButton(objectName);
            if (button == null)
            {
                return;
            }

            var unlockedStage = GetCurrentStageForChapter(selectedChapter);
            var clearedStage = PlayerPrefs.GetInt($"{ClearedStageKeyPrefix}{selectedChapter}", 0);
            button.interactable = stage <= unlockedStage;
            SetButtonLabel(button, GetStageLabel(stage, clearedStage, unlockedStage));
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (stage > GetCurrentStageForChapter(selectedChapter))
                {
                    return;
                }

                PlayerPrefs.SetInt(SelectedChapterKey, selectedChapter);
                PlayerPrefs.SetInt(SelectedStageKey, stage);
                PlayerPrefs.Save();
                SceneManager.LoadScene("BattleScene");
            });

            if (button.GetComponent<DirectButtonClicker>() == null)
            {
                button.gameObject.AddComponent<DirectButtonClicker>();
            }
        }

        private static string GetStageLabel(int stage, int clearedStage, int unlockedStage)
        {
            var name = stage == 3 ? "보스" : $"{stage}스테이지";
            var state = stage <= clearedStage ? "완료" : stage == unlockedStage ? "진행" : "잠김";
            return $"{name}\n{state}";
        }

        private static int GetCurrentStageForChapter(int chapter)
        {
            var clearedStage = PlayerPrefs.GetInt($"{ClearedStageKeyPrefix}{chapter}", 0);
            return Mathf.Clamp(clearedStage + 1, 1, 3);
        }

        private static int GetUnlockedChapter()
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(UnlockedChapterKey, 1), 1, 3);
        }

        private static void WireButton(string objectName, UnityEngine.Events.UnityAction action)
        {
            var button = FindButton(objectName);
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            if (button.GetComponent<DirectButtonClicker>() == null)
            {
                button.gameObject.AddComponent<DirectButtonClicker>();
            }
        }

        private static Button FindButton(string objectName)
        {
            var target = GameObject.Find(objectName);
            return target == null ? null : target.GetComponent<Button>();
        }

        private static void SetButtonLabel(Button button, string value)
        {
            var text = button == null ? null : button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetText(string objectName, string value)
        {
            var target = GameObject.Find(objectName);
            if (target != null && target.TryGetComponent<Text>(out var text))
            {
                text.text = value;
            }
        }
    }

    public class SceneFlowBootstrapDriver : MonoBehaviour
    {
        private static SceneFlowBootstrapDriver instance;
        private Coroutine setupRoutine;

        public static SceneFlowBootstrapDriver Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                var driverObject = new GameObject("Scene Flow Bootstrap Driver");
                Object.DontDestroyOnLoad(driverObject);
                instance = driverObject.AddComponent<SceneFlowBootstrapDriver>();
                return instance;
            }
        }

        public void RunSetup()
        {
            if (setupRoutine != null)
            {
                StopCoroutine(setupRoutine);
            }

            setupRoutine = StartCoroutine(RunSetupRoutine());
        }

        private IEnumerator RunSetupRoutine()
        {
            yield return null;
            SceneFlowBootstrap.Setup();
            yield return null;
            SceneFlowBootstrap.Setup();
            setupRoutine = null;
        }
    }
}
