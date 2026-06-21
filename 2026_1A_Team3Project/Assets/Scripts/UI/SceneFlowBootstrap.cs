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
}
