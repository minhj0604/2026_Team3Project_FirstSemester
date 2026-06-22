using UnityEngine;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class StageMapController : MonoBehaviour
    {
        private const string SelectedChapterKey = "Team3.SelectedChapter";
        private const string ClearedStageKeyPrefix = "Team3.ClearedStage.";

        [SerializeField] private Text chapterText;
        [SerializeField] private Text progressText;
        [SerializeField] private int chapterIndex = 1;
        [SerializeField] private int currentStage = 1;

        private void Start()
        {
            chapterIndex = PlayerPrefs.GetInt(SelectedChapterKey, chapterIndex);
            currentStage = Mathf.Clamp(PlayerPrefs.GetInt($"{ClearedStageKeyPrefix}{chapterIndex}", 0) + 1, 1, 3);
            Refresh();
            WireStageButton("Stage 1 Node", 1);
            WireStageButton("Stage 2 Node", 2);
            WireStageButton("Boss Stage Node", 3);
        }

        public void Refresh()
        {
            if (chapterText != null)
            {
                chapterText.text = $"{chapterIndex}\uCC55\uD130";
            }

            if (progressText != null)
            {
                progressText.text = BuildProgressText();
            }
        }

        private string BuildProgressText()
        {
            var parts = new string[3];
            var clearedStage = Mathf.Clamp(currentStage - 1, 0, 3);
            for (var i = 1; i <= 3; i++)
            {
                parts[i - 1] = i <= clearedStage ? "[\uC644\uB8CC]" : i == currentStage ? "[\uC9C4\uD589]" : i == 3 ? "[\uBCF4\uC2A4]" : "[\uC7A0\uAE40]";
            }

            return $"{parts[0]}  ->  {parts[1]}  ->  {parts[2]}";
        }

        private void WireStageButton(string objectName, int stage)
        {
            var target = GameObject.Find(objectName);
            var button = target == null ? null : target.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = stage <= currentStage;
            SetButtonLabel(button, BuildStageLabel(stage));
            button.onClick.AddListener(() =>
            {
                if (stage > currentStage)
                {
                    return;
                }

                SceneFlowBootstrap.EnterStage(stage);
            });

            if (button.GetComponent<DirectButtonClicker>() == null)
            {
                button.gameObject.AddComponent<DirectButtonClicker>();
            }
        }

        private string BuildStageLabel(int stage)
        {
            var name = stage == 3 ? "\uBCF4\uC2A4" : $"{stage}\uC2A4\uD14C\uC774\uC9C0";
            var clearedStage = Mathf.Clamp(currentStage - 1, 0, 3);
            var state = stage <= clearedStage ? "\uC644\uB8CC" : stage == currentStage ? "\uC9C4\uD589" : "\uC7A0\uAE40";
            return $"{name}\n{state}";
        }

        private static void SetButtonLabel(Button button, string value)
        {
            var text = button == null ? null : button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
