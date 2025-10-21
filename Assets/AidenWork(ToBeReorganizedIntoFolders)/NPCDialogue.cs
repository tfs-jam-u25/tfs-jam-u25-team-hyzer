using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Data (Progression)")]
    public DialogueData[] dialogues;

    [Header("NPC Identity")]
    [Tooltip("Unique ID for this NPC. Must be unique across ALL scenes!")]
    public string npcID = "NPC_Villager_01";

    void Start()
    {
        if (string.IsNullOrEmpty(npcID))
        {
            Debug.LogError($"NPCDialogue on {gameObject.name}: npcID is empty! Dialogue progress won't persist.", this);
        }

        Debug.Log($"[NPCDialogue] {npcID} initialized with {dialogues.Length} dialogues");
    }

    /// <summary>
    /// Call this via the Interactable component UnityEvent
    /// </summary>
    public void Talk()
    {
        if (DialogueManager.Instance.CurrentlyActive)
        {
            DialogueManager.Instance.NextLine();
            return;
        }

        if (dialogues == null || dialogues.Length == 0) return;

        int currentDialogueIndex = DialogueManager.Instance.GetNPCDialogueIndex(npcID);
        currentDialogueIndex = Mathf.Clamp(currentDialogueIndex, 0, dialogues.Length - 1);

        DialogueData dialogueToPlay = dialogues[currentDialogueIndex];

        if (dialogueToPlay != null)
        {
            Debug.Log($"[NPCDialogue] {npcID} starting dialogue {currentDialogueIndex}");
            DialogueManager.Instance.StartDialogue(dialogueToPlay, this, npcID, dialogues.Length);
        }
    }
}
