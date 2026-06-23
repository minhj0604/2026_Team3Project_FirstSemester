using System.Collections.Generic;
using UnityEngine;

namespace Team3Project.Dialogue
{
    [System.Serializable]
    public class DialogueLineData
    {
        [TextArea(3, 10)]
        public string text;

        [Header("Line Character Override")]
        public string characterName;
        public Sprite characterImage;

        [Header("Line Background Override")]
        public bool changeBackground;
        public Sprite backgroundImage;
        public Color backgroundColor = Color.black;
    }

    [CreateAssetMenu(fileName = "DialogueDataSO", menuName = "Team3/Dialogue Data")]
    public class DialogueDataSO : ScriptableObject
    {
        [Header("Character Info")]
        public string characterName = "\uCE90\uB9AD\uD130";
        public Sprite characterImage;

        [Header("Default Background")]
        public bool useDefaultBackground = true;
        public Sprite defaultBackgroundImage;
        public Color defaultBackgroundColor = Color.black;

        [Header("Dialogue Entries")]
        public List<DialogueLineData> entries = new();

        [Header("Legacy Dialogue Lines")]
        [TextArea(3, 10)]
        public List<string> dialogueLines = new();

        public bool UsesEntries => entries != null && entries.Count > 0;

        public int LineCount => entries != null && entries.Count > 0
            ? entries.Count
            : dialogueLines == null ? 0 : dialogueLines.Count;

        public DialogueLineData GetLine(int index)
        {
            if (entries != null && entries.Count > 0)
            {
                return index >= 0 && index < entries.Count ? entries[index] : null;
            }

            if (dialogueLines == null || index < 0 || index >= dialogueLines.Count)
            {
                return null;
            }

            return new DialogueLineData
            {
                text = dialogueLines[index],
                characterName = characterName,
                characterImage = characterImage,
                changeBackground = index == 0 && useDefaultBackground,
                backgroundImage = defaultBackgroundImage,
                backgroundColor = defaultBackgroundColor
            };
        }
    }
}
