using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public static class SceneFlowBootstrap
    {
        private static int selectedChapter = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded += (_, _) => Setup();
            Setup();
        }

        private static void Setup()
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
        }

        private static void SetupChapterSelectScene()
        {
            WireButton("Enter Chapter Button", () =>
            {
                if (selectedChapter == 1)
                {
                    SceneManager.LoadScene("StageMapScene");
                }
            });

            WireButton("Back Button", () => SceneManager.LoadScene("TitleScene"));
            EnsureChapterArrowFallback();
            RefreshChapterSelect();
        }

        public static void ChangeChapter(int direction)
        {
            selectedChapter = Mathf.Clamp(selectedChapter + direction, 1, 3);
            RefreshChapterSelect();
        }

        public static void RefreshChapterSelect()
        {
            SetText("Title Text", $"Chapter {selectedChapter}");
            var enter = FindButton("Enter Chapter Button");
            if (enter != null)
            {
                enter.interactable = selectedChapter == 1;
            }

            var lockOne = GameObject.Find("Chapter Lock Hidden 1");
            if (lockOne != null)
            {
                lockOne.SetActive(selectedChapter != 1);
            }

            var lockTwo = GameObject.Find("Chapter Lock Hidden 2");
            if (lockTwo != null)
            {
                lockTwo.SetActive(false);
            }
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
            SetText("Title Text", $"Chapter {selectedChapter} Stage Map");
            WireButton("Stage 2 Node", () => SceneManager.LoadScene("BattleScene"));
            WireButton("Back Button", () => SceneManager.LoadScene("ChapterSelectScene"));
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

        private static void SetText(string objectName, string value)
        {
            var target = GameObject.Find(objectName);
            if (target != null && target.TryGetComponent<Text>(out var text))
            {
                text.text = value;
            }
        }
    }
}
