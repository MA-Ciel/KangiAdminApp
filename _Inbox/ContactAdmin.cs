using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// ContactAdmin
/// ─────────────────────────────────────────────────────────────
/// Attach to a Canvas GameObject. Wire up 3 fields in Inspector:
///
///   messageInput  — TMP_InputField   (the text box)
///   sendButton    — Button           (Send button)
///   statusText    — TextMeshProUGUI  (feedback label, can be null)
///
/// What it does:
///   • User types a message → hits Send
///   • Message is stored server-side via PlayFab CloudScript
///   • Admin sees it in the web dashboard Messages tab
///   • Admin can reply; the reply arrives as a Notification in-game
/// </summary>
public class ContactAdmin : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField  messageInput;
    [SerializeField] private Button          sendButton;
    [SerializeField] private TextMeshProUGUI statusText;

    // ── Lifecycle ─────────────────────────────────────────────

    void Start()
    {
        sendButton?.onClick.AddListener(SendMessage);
        HideStatus();
    }

    // ── Public: call from Button onClick or code ───────────────

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
                ShowStatus("Message sent! Admin will reply via notifications.", true);
            },
            error =>
            {
                sendButton.interactable = true;
                ShowStatus("Failed: " + error.ErrorMessage, false);
            }
        );
    }

    // ── Helpers ───────────────────────────────────────────────

    private void ShowStatus(string msg, bool good)
    {
        if (statusText == null) return;
        statusText.gameObject.SetActive(true);
        statusText.text  = msg;
        statusText.color = good
            ? new Color(0.30f, 1.00f, 0.50f)   // green
            : new Color(1.00f, 0.35f, 0.35f);   // red
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
