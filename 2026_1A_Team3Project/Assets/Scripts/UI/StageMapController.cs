using UnityEngine;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class StageMapController : MonoBehaviour
    {
        [SerializeField] private Text chapterText;
        [SerializeField] private Text progressText;
        [SerializeField] private int chapterIndex = 1;
        [SerializeField] private int currentStage = 1;

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (chapterText != null)
            {
                chapterText.text = $"Chapter {chapterIndex}";
            }

            if (progressText != null)
            {
                progressText.text = BuildProgressText();
            }
        }

        private string BuildProgressText()
        {
            var parts = new string[3];
            for (var i = 1; i <= 3; i++)
            {
                parts[i - 1] = i < currentStage ? "[Clear]" : i == currentStage ? "[Now]" : i == 3 ? "[Boss]" : "[Enemy]";
            }

            return $"{parts[0]}  ->  {parts[1]}  ->  {parts[2]}";
        }
    }
}
