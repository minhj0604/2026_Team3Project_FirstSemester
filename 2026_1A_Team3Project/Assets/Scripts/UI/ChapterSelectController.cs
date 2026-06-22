using UnityEngine;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class ChapterSelectController : MonoBehaviour
    {
        [SerializeField] private Text chapterTitleText;
        [SerializeField] private Text chapterSummaryText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button enterButton;
        [SerializeField] private Image lockImage;
        [SerializeField] private Image bossImage;
        [SerializeField] private Sprite[] bossSprites;
        [SerializeField] private int unlockedChapter = 1;

        private int currentChapter = 1;

        private void Awake()
        {
            Refresh();
        }

        public void ChangeChapter(int direction)
        {
            currentChapter = Mathf.Clamp(currentChapter + direction, 1, 3);
            Refresh();
        }

        private void Refresh()
        {
            var isUnlocked = currentChapter <= unlockedChapter;

            if (chapterTitleText != null)
            {
                chapterTitleText.text = $"{currentChapter}\uCC55\uD130";
            }

            if (chapterSummaryText != null)
            {
                chapterSummaryText.text = isUnlocked
                    ? $"1\uC2A4\uD14C\uC774\uC9C0 > 2\uC2A4\uD14C\uC774\uC9C0 > \uBCF4\uC2A4\n{currentChapter}\uCC55\uD130 \uC9C4\uC785"
                    : "\uC7A0\uAE40";
            }

            if (enterButton != null)
            {
                enterButton.interactable = isUnlocked;
            }

            if (lockImage != null)
            {
                lockImage.gameObject.SetActive(!isUnlocked);
            }

            if (bossImage != null && bossSprites != null && bossSprites.Length > 0)
            {
                bossImage.sprite = bossSprites[Mathf.Clamp(currentChapter - 1, 0, bossSprites.Length - 1)];
            }
        }
    }
}
