using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class LogCollector : MonoBehaviour
{
    private static List<string> logs = new List<string>();

    [Header("UI References")]
    [SerializeField] private Button exportButton;
    [SerializeField] private Text confirmationText; // maybe assign a UI Text

    void Awake()
    {
        if (exportButton != null)
            exportButton.onClick.AddListener(OnExportLogsClicked);

        if (confirmationText != null)
            confirmationText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        logs.Add($"[{type}] {logString}\n{stackTrace}");
    }

    private string GetAllLogs()
    {
        return string.Join("\n", logs);
    }

    public void OnExportLogsClicked()
    {
        string allLogs = GetAllLogs();

        // Copy to clipboard
        GUIUtility.systemCopyBuffer = allLogs;

        // Save to file
        string path = Path.Combine(Application.persistentDataPath, "player_logs.txt");
        File.WriteAllText(path, allLogs);

        // Show confirmation
        if (confirmationText != null)
        {
            confirmationText.text = "Logs copied and saved!";
            confirmationText.gameObject.SetActive(true);
            Invoke(nameof(HideConfirmation), 3f); // Hide after 3 seconds
        }

        Debug.Log($"Logs exported to clipboard and saved to: {path}");
    }

    private void HideConfirmation()
    {
        if (confirmationText != null)
            confirmationText.gameObject.SetActive(false);
    }
}
