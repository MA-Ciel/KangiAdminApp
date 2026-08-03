using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// ContactAdmin — one input field + send button.
/// The user types a message and it gets stored in the admin's
/// PlayFab Title Internal Data under "AdminInbox" so the
/// web dashboard can display it.
///
/// Setup in Inspector:
///   messageInput  — TMP_InputField  (the text box)
///   sendButton    — Button          (the Send button)
///   statusText    — TextMeshProUGUI (optional feedback label)
/// </summary>
public class ContactAdmin : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button         sendButton;
    [SerializeField] private TextMeshProUGUI statusText;   // optional

    void Start()
    {
        sendButton?.onClick.AddListener(SendMessage);
        if (statusText != null) statusText.gameObject.SetActive(false);
    }

    // ── Send ────────────────────────────────────────────────────────────────

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

        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            FunctionName = "supportWorkflow",
            FunctionParameter = new { action = "sendMessage", body = text },
            GeneratePlayStreamEvent = true
        },
        result =>
        {
            sendButton.interactable = true;
            if (messageInput != null) messageInput.text = "";
            ShowStatus("Message sent to admin!", true);
        },
        error =>
        {
            sendButton.interactable = true;
            ShowStatus("Failed: " + error.ErrorMessage, false);
        });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void ShowStatus(string msg, bool good)
    {
        if (statusText == null) return;
        statusText.gameObject.SetActive(true);
        statusText.text  = msg;
        statusText.color = good
            ? new Color(0.3f, 1f, 0.5f)    // green
            : new Color(1f, 0.35f, 0.35f);  // red
    }

    void OnDestroy()
    {
        sendButton?.onClick.RemoveAllListeners();
    }
}
