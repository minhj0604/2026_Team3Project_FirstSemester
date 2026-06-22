using UnityEngine;

namespace Team3Project.Dialogue
{
    public class DialogueNPC : MonoBehaviour
    {
        [SerializeField] private DialogueDataSO myDialogue;

        private void OnMouseDown()
        {
            if (DialogueManager.Instance.IsDialogueActive() || myDialogue == null)
            {
                return;
            }

            DialogueManager.Instance.StartDialogue(myDialogue);
        }
    }
}
