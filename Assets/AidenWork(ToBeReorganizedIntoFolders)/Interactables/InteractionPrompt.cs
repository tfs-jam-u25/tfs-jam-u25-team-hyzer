using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InteractionPrompt : MonoBehaviour
{
    public static InteractionPrompt Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Camera mainCamera;

    private Transform target;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        root.SetActive(false);
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public void Show(List<InteractionOption> options, Transform target)
    {
        if (options == null || options.Count == 0)
        {
            Hide();
            return;
        }

        this.target = target;

        promptText.text = "";
        foreach (var opt in options)
            promptText.text += $"[{opt.key}] {opt.promptText}\n";

        promptText.text = promptText.text.TrimEnd('\n');
        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
        target = null;
    }

    void LateUpdate()
    {
        if (target == null || !root.activeSelf)
            return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        transform.position = screenPos;
    }
}
