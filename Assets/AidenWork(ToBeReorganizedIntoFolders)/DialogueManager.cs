using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public TypewriterEffect typewriter;
    public Image portraitImage;
    public Button continueButton;

    public bool CurrentlyActive => currentDialogue != null;

    private DialogueData currentDialogue;
    private int currentIndex;
    private NPCDialogue currentNPC;
    private string currentNPCID;
    private int totalDialoguesForCurrentNPC;

    // Dictionary to track dialogue progress per NPC (backed up to PlayerPrefs)
    private Dictionary<string, int> npcDialogueProgress = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[DialogueManager] Duplicate instance found! Destroying {gameObject.name}. Existing instance: {Instance.gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved progress from PlayerPrefs
        LoadProgressFromPlayerPrefs();

        Debug.Log($"[DialogueManager] Instance created on GameObject: {gameObject.name}. Persisting across scenes.");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[DialogueManager] Scene loaded: {scene.name}. Dictionary still has {npcDialogueProgress.Count} entries.");
        SetupButtonListener();
    }

    void Start()
    {
        SetupButtonListener();
    }

    void SetupButtonListener()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(NextLine);
            Debug.Log("[DialogueManager] Continue button listener set up");
        }
    }

    // NEW: Load progress from PlayerPrefs
    void LoadProgressFromPlayerPrefs()
    {
        string savedData = PlayerPrefs.GetString("DialogueProgress", "");
        if (!string.IsNullOrEmpty(savedData))
        {
            string[] entries = savedData.Split('|');
            foreach (string entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                string[] parts = entry.Split(':');
                if (parts.Length == 2)
                {
                    string npcID = parts[0];
                    if (int.TryParse(parts[1], out int index))
                    {
                        npcDialogueProgress[npcID] = index;
                    }
                }
            }
            Debug.Log($"[DialogueManager] Loaded {npcDialogueProgress.Count} dialogue progress entries from PlayerPrefs");
        }
    }

    // NEW: Save progress to PlayerPrefs
    void SaveProgressToPlayerPrefs()
    {
        List<string> entries = new List<string>();
        foreach (var kvp in npcDialogueProgress)
        {
            entries.Add($"{kvp.Key}:{kvp.Value}");
        }
        string savedData = string.Join("|", entries);
        PlayerPrefs.SetString("DialogueProgress", savedData);
        PlayerPrefs.Save();
        Debug.Log($"[DialogueManager] Saved {npcDialogueProgress.Count} dialogue progress entries to PlayerPrefs");
    }

    public int GetNPCDialogueIndex(string npcID)
    {
        if (npcDialogueProgress.ContainsKey(npcID))
        {
            Debug.Log($"[DialogueManager] Getting progress for {npcID}: {npcDialogueProgress[npcID]}");
            return npcDialogueProgress[npcID];
        }
        Debug.Log($"[DialogueManager] No progress found for {npcID}, returning 0");
        return 0;
    }

    private void SetNPCDialogueIndex(string npcID, int index)
    {
        npcDialogueProgress[npcID] = index;
        SaveProgressToPlayerPrefs(); // NEW: Save immediately
        Debug.Log($"[DialogueManager] ✓✓✓ SAVED: {npcID} dialogue index = {index} (Dictionary: {npcDialogueProgress.Count} entries) ✓✓✓");
    }

    public void StartDialogue(DialogueData dialogue, NPCDialogue npc, string npcID, int totalDialogues)
    {
        if (dialoguePanel == null)
        {
            Debug.LogError("[DialogueManager] dialoguePanel is null! Cannot start dialogue.");
            return;
        }

        currentDialogue = dialogue;
        currentNPC = npc;
        currentNPCID = npcID;
        totalDialoguesForCurrentNPC = totalDialogues;
        currentIndex = 0;
        dialoguePanel.SetActive(true);

        Debug.Log($"[DialogueManager] Starting dialogue for {npcID}, total dialogues: {totalDialogues}");

        if (InteractionPrompt.Instance != null)
        {
            InteractionPrompt.Instance.Hide();
        }

        ShowLine();
    }

    public void NextLine()
    {
        Debug.Log($"[DialogueManager] NextLine called. CurrentDialogue null? {currentDialogue == null}, Index: {currentIndex}");

        if (currentDialogue == null) return;

        if (typewriter != null && typewriter.IsRunning)
        {
            Debug.Log("[DialogueManager] Typewriter still running, skipping");
            typewriter.Skip();
            return;
        }

        if (currentIndex < currentDialogue.lines.Length - 1)
        {
            currentIndex++;
            Debug.Log($"[DialogueManager] Advancing to line {currentIndex}");
            ShowLine();
        }
        else
        {
            Debug.Log("[DialogueManager] Dialogue complete, ending...");
            EndDialogue(shouldAdvanceProgress: true);
        }
    }

    void ShowLine()
    {
        var line = currentDialogue.lines[currentIndex];
        nameText.text = line.speakerName;

        if (typewriter != null)
        {
            typewriter.Play(line.text);
        }
        else
        {
            dialogueText.text = line.text;
        }

        if (portraitImage != null)
        {
            if (line.portrait != null)
            {
                portraitImage.sprite = line.portrait;
                portraitImage.enabled = true;
            }
            else
            {
                portraitImage.enabled = false;
            }
        }
    }

    void EndDialogue(bool shouldAdvanceProgress)
    {
        Debug.Log($"[DialogueManager] EndDialogue called. ShouldAdvance: {shouldAdvanceProgress}, NPCID: {currentNPCID}");

        // Advance dialogue if requested
        if (shouldAdvanceProgress && !string.IsNullOrEmpty(currentNPCID))
        {
            int currentProgress = GetNPCDialogueIndex(currentNPCID);
            if (currentProgress < totalDialoguesForCurrentNPC - 1)
            {
                SetNPCDialogueIndex(currentNPCID, currentProgress + 1);
            }
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // ✅ Re-show interaction prompt if still in range of this NPC
        if (currentNPC != null && InteractionPrompt.Instance != null)
        {
            Interactable interactable = currentNPC.GetComponent<Interactable>();
            if (interactable != null)
            {
                InteractionPrompt.Instance.Show(interactable.GetInteractions(), currentNPC.transform);
            }
        }

        currentDialogue = null;
        currentNPC = null;
        currentNPCID = null;
        totalDialoguesForCurrentNPC = 0;
    }

    public void StopDialogue(NPCDialogue npc)
    {
        if (currentNPC == npc)
        {
            Debug.Log($"[DialogueManager] Dialogue interrupted - ADVANCING progress anyway");
            EndDialogue(shouldAdvanceProgress: true);
        }
    }

    public void ResetNPCDialogue(string npcID)
    {
        if (npcDialogueProgress.ContainsKey(npcID))
        {
            npcDialogueProgress.Remove(npcID);
            SaveProgressToPlayerPrefs();
            Debug.Log($"[DialogueManager] Reset dialogue progress for {npcID}");
        }
    }

    public void ResetAllDialogue()
    {
        npcDialogueProgress.Clear();
        PlayerPrefs.DeleteKey("DialogueProgress");
        PlayerPrefs.Save();
        Debug.Log("[DialogueManager] Reset ALL dialogue progress");
    }

    void OnDestroy()
    {
        Debug.LogError($"[DialogueManager] GameObject '{gameObject.name}' is being DESTROYED! This should NOT happen!");
        Debug.LogError($"Was this the Instance? {this == Instance}");
    }

    public void DebugLogAllProgress()
    {
        Debug.Log("=== NPC Dialogue Progress ===");
        Debug.Log($"DialogueManager Instance: {(Instance != null ? Instance.gameObject.name : "NULL")}");
        Debug.Log($"Dictionary Count: {npcDialogueProgress.Count}");
        foreach (var kvp in npcDialogueProgress)
        {
            Debug.Log($"  {kvp.Key}: Dialogue Index {kvp.Value}");
        }
        if (npcDialogueProgress.Count == 0)
        {
            Debug.Log("  (No dialogue progress tracked yet)");
        }
        Debug.Log("============================");
    }
}