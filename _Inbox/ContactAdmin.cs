using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// ContactAdmin
/// ─────────────────────────────────────────────────────────────
/// Wire in Inspector:
///   messageInput   TMP_InputField   — the text box
///   sendButton     Button           — Send button
///   statusText     TextMeshProUGUI  — optional feedback label
///   inboxViewer    InboxViewer      — optional: auto-refreshes list after send
/// </summary>
public class ContactAdmin : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField  messageInput;
    [SerializeField] private Button          sendButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Optional — refresh inbox after send")]
    [SerializeField] private InboxViewer inboxViewer;

    void Start()
    {
        sendButton?.onClick.AddListener(SendMessage);
        HideStatus();
    }

    // ── Call from button onClick or code ──────────────────────

    public void SendMessage()
    {
        string text = messageInput != null ? messageInput.text.Trim() : "";

        if (string.IsNullOrEmpty(text))
        {
            ShowStatus("Please type a message first.", false);
            return;
        }

        sendButton.interactable = false;
        ShowStatus("Sending...", true);

        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "supportWorkflow",
                FunctionParameter = new { action = "sendMessage", body = text },
                GeneratePlayStreamEvent = true
            },
            result =>
            {
                sendButton.interactable = true;
                messageInput.text = "";
                ShowStatus("Sent! Admin will reply soon.", true);

                // Refresh the inbox list so the new message appears immediately
                inboxViewer?.Refresh();
            },
            error =>
            {
                sendButton.interactable = true;
                ShowStatus("Failed: " + error.ErrorMessage, false);
                Debug.LogError("[ContactAdmin] " + error.ErrorMessage);
            }
        );
    }

    // ── Status helpers ────────────────────────────────────────

    private void ShowStatus(string msg, bool good)
    {
        if (statusText == null) return;
        statusText.gameObject.SetActive(true);
        statusText.text  = msg;
        statusText.color = good
            ? new Color(0.30f, 1.00f, 0.50f)
            : new Color(1.00f, 0.35f, 0.35f);
    }

    private void HideStatus()
    {
        if (statusText != null) statusText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        sendButton?.onClick.RemoveAllListeners();
    }
}
