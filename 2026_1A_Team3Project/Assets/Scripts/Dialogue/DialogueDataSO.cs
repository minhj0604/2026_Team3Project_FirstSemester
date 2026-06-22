using System.Collections.Generic;
using UnityEngine;

namespace Team3Project.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueDataSO", menuName = "Team3/Dialogue Data")]
    public class DialogueDataSO : ScriptableObject
    {
        [Header("Character Info")]
        public string characterName = "캐릭터";
        public Sprite characterImage;

        [Header("Dialogue Lines")]
        [TextArea(3, 10)]
        public List<string> dialogueLines = new();
    }
}
