using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SupportMessageSender — User-facing UI for contacting the admin.
///
/// Hierarchy (assign in Inspector):
/// ┌─ SupportPanel (root GameObject, toggled by openButton)
/// │   ├─ TabBar
/// │   │   ├─ TabComposeBtn       (Button)
/// │   │   └─ TabInboxBtn         (Button)
/// │   ├─ ComposePanel
/// │   │   ├─ SubjectInput        (TMP_InputField)
/// │   │   ├─ BodyInput           (TMP_InputField, multiline)
/// │   │   ├─ CharCountText       (TextMeshProUGUI)  "0 / 1000"
/// │   │   ├─ SendBtn             (Button)
/// │   │   └─ StatusText          (TextMeshProUGUI)
/// │   └─ InboxPanel
/// │       ├─ EmptyText           (TextMeshProUGUI)
/// │       └─ MessageListContainer (Transform — parent for item prefabs)
/// └─ OpenBtn / CloseBtn          (Buttons outside the panel)
///
/// Prefab: MessageItemPrefab needs children named:
///   SubjectText, StatusText, TimeText, BodyText, ReplyText, ReplyContainer
/// </summary>
namespace Kangi.ServerNotifications
{
public class SupportMessageSender : MonoBehaviour
{
    // ── Panel root ────────────────────────────────────────────────────────────
    [Header("Panel")]
    [SerializeField] private GameObject supportPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    // ── Tabs ─────────────────────────────────────────────────────────────────
    [Header("Tabs")]
    [SerializeField] private Button tabComposeBtn;
    [SerializeField] private Button tabInboxBtn;
    [SerializeField] private GameObject composePanel;
    [SerializeField] private GameObject inboxPanel;
    [SerializeField] private Color tabActiveColor   = new Color(0.93f, 0.28f, 0.60f);
    [SerializeField] private Color tabInactiveColor = new Color(1f, 1f, 1f, 0.35f);

    // ── Compose ───────────────────────────────────────────────────────────────
    [Header("Compose")]
    [SerializeField] private TMP_InputField subjectInput;
    [SerializeField] private TMP_InputField bodyInput;
    [SerializeField] private TextMeshProUGUI charCountText;
    [SerializeField] private Button sendBtn;
    [SerializeField] private TextMeshProUGUI statusText;

    // ── Inbox ─────────────────────────────────────────────────────────────────
    [Header("Inbox")]
    [SerializeField] private Transform messageListContainer;
    [SerializeField] private GameObject messageItemPrefab;
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private Button refreshInboxBtn;

    // ── Badge (unread reply count) ────────────────────────────────────────────
    [Header("Badge")]
    [SerializeField] private GameObject replyBadge;
    [SerializeField] private TextMeshProUGUI replyBadgeText;

    private List<GameObject> spawnedItems = new List<GameObject>();
    private bool isSending = false;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        openButton?.onClick.AddListener(Open);
        closeButton?.onClick.AddListener(Close);
        tabComposeBtn?.onClick.AddListener(() => SwitchTab(true));
        tabInboxBtn?.onClick.AddListener(() => SwitchTab(false));
        sendBtn?.onClick.AddListener(OnSendClicked);
        refreshInboxBtn?.onClick.AddListener(RefreshInbox);

        if (bodyInput != null)
            bodyInput.onValueChanged.AddListener(OnBodyChanged);

        // Subscribe to inbox updates
        if (SupportInboxManager.Instance != null)
            SupportInboxManager.Instance.OnMessagesUpdated += OnMessagesUpdated;

        SwitchTab(true); // Start on Compose
        UpdateBadge();
        Close();
    }

    // ─── Open / Close ─────────────────────────────────────────────────────────

    public void Open()
    {
        supportPanel?.SetActive(true);
        SupportInboxManager.Instance?.FetchMyMessages();
    }

    public void Close()
    {
        supportPanel?.SetActive(false);
    }

    // ─── Tab switching ────────────────────────────────────────────────────────

    private void SwitchTab(bool showCompose)
    {
        composePanel?.SetActive(showCompose);
        inboxPanel?.SetActive(!showCompose);

        SetTabColor(tabComposeBtn,  showCompose);
        SetTabColor(tabInboxBtn,   !showCompose);

        if (!showCompose)
        {
            // Render current cached list immediately; refresh from server
            RenderMessages(SupportInboxManager.Instance?.GetCachedMessages()
                           ?? new List<SupportMessage>());
            SupportInboxManager.Instance?.FetchMyMessages();
        }
    }

    private void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.color = active ? tabActiveColor : tabInactiveColor;

        // Underline / highlight the active tab via the Button's image
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = active ? 1f : 0.3f;
            img.color = c;
        }
    }

    // ─── Compose handlers ─────────────────────────────────────────────────────

    private void OnBodyChanged(string value)
    {
        if (charCountText != null)
            charCountText.text = $"{value.Length} / 1000";
    }

    private void OnSendClicked()
    {
        if (isSending) return;

        string subject = subjectInput != null ? subjectInput.text.Trim() : "";
        string body    = bodyInput    != null ? bodyInput.text.Trim()    : "";

        if (string.IsNullOrEmpty(subject))
        {
            ShowStatus("Please enter a subject.", false);
            return;
        }
        if (string.IsNullOrEmpty(body))
        {
            ShowStatus("Please write your message.", false);
            return;
        }

        isSending = true;
        SetSendBtnLoading(true);
        ShowStatus("Sending...", true);

        SupportInboxManager.Instance?.SendMessage(subject, body,
            onSuccess: msg =>
            {
                isSending = false;
                SetSendBtnLoading(false);
                ShowStatus("Message sent! We'll reply via notifications.", true);

                // Clear form
                if (subjectInput != null) subjectInput.text = "";
                if (bodyInput    != null) bodyInput.text    = "";
                if (charCountText != null) charCountText.text = "0 / 1000";

                // Auto-switch to inbox after 1 second
                StartCoroutine(DelayedTabSwitch());
            },
            onError: err =>
            {
                isSending = false;
                SetSendBtnLoading(false);
                ShowStatus($"Error: {err}", false);
            });
    }

    private System.Collections.IEnumerator DelayedTabSwitch()
    {
        yield return new WaitForSeconds(1.2f);
        SwitchTab(false);
    }

    private void ShowStatus(string msg, bool good)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = good
            ? new Color(0.3f, 1f, 0.5f)   // green
            : new Color(1f, 0.35f, 0.35f); // red
        statusText.gameObject.SetActive(true);
    }

    private void SetSendBtnLoading(bool loading)
    {
        if (sendBtn == null) return;
        sendBtn.interactable = !loading;
        var label = sendBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = loading ? "Sending..." : "Send Message";
    }

    // ─── Inbox rendering ──────────────────────────────────────────────────────

    private void RefreshInbox()
    {
        SupportInboxManager.Instance?.FetchMyMessages();
    }

    private void OnMessagesUpdated(List<SupportMessage> messages)
    {
        RenderMessages(messages);
        UpdateBadge();
    }

    private void RenderMessages(List<SupportMessage> messages)
    {
        // Clear old items
        foreach (var go in spawnedItems) Destroy(go);
        spawnedItems.Clear();

        if (messages == null || messages.Count == 0)
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(true);
                emptyText.text = "You haven't sent any messages yet.";
            }
            return;
        }

        if (emptyText != null) emptyText.gameObject.SetActive(false);

        foreach (var msg in messages)
        {
            if (messageItemPrefab == null || messageListContainer == null) break;

            GameObject item = Instantiate(messageItemPrefab, messageListContainer);
            spawnedItems.Add(item);
            PopulateItem(item, msg);
        }
    }

    private void PopulateItem(GameObject item, SupportMessage msg)
    {
        // Subject
        var subjectTxt = item.transform.Find("SubjectText")?.GetComponent<TextMeshProUGUI>();
        if (subjectTxt != null) subjectTxt.text = msg.subject;

        // Status badge
        var statusTxt = item.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        if (statusTxt != null)
        {
            switch (msg.status)
            {
                case "open":
                    statusTxt.text  = "Open";
                    statusTxt.color = new Color(1f, 0.8f, 0.2f);  // amber
                    break;
                case "replied":
                case "replied_unread":
                    statusTxt.text  = "Replied";
                    statusTxt.color = new Color(0.3f, 1f, 0.5f);   // green
                    break;
                case "closed":
                    statusTxt.text  = "Closed";
                    statusTxt.color = new Color(0.7f, 0.7f, 0.7f); // grey
                    break;
                default:
                    statusTxt.text  = msg.status;
                    statusTxt.color = Color.white;
                    break;
            }
        }

        // Timestamp
        var timeTxt = item.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
        if (timeTxt != null) timeTxt.text = FormatTime(msg.createdAt);

        // Message body
        var bodyTxt = item.transform.Find("BodyText")?.GetComponent<TextMeshProUGUI>();
        if (bodyTxt != null) bodyTxt.text = msg.body;

        // Admin reply section
        var replyContainer = item.transform.Find("ReplyContainer")?.gameObject;
        var replyTxt       = item.transform.Find("ReplyText")?.GetComponent<TextMeshProUGUI>();
        bool hasReply = !string.IsNullOrEmpty(msg.adminReply);

        if (replyContainer != null) replyContainer.SetActive(hasReply);
        if (replyTxt != null && hasReply)
        {
            replyTxt.text = $"Admin reply ({FormatTime(msg.repliedAt)}):\n{msg.adminReply}";

            // Highlight unread replies
            if (msg.status == "replied_unread")
                replyTxt.color = new Color(1f, 0.9f, 0.3f); // yellow tint
            else
                replyTxt.color = new Color(0.85f, 0.85f, 0.85f);
        }

        // Mark reply as read when the item is clicked
        if (msg.status == "replied_unread")
        {
            var btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                SupportInboxManager.Instance?.MarkReplyAsRead(msg.id);
                if (replyTxt != null)
                    replyTxt.color = new Color(0.85f, 0.85f, 0.85f);
                UpdateBadge();
            });
        }
    }

    // ─── Badge ────────────────────────────────────────────────────────────────

    private void UpdateBadge()
    {
        int count = SupportInboxManager.Instance?.GetUnreadRepliesCount() ?? 0;
        if (replyBadge != null)    replyBadge.SetActive(count > 0);
        if (replyBadgeText != null) replyBadgeText.text = count > 9 ? "9+" : count.ToString();
    }

    // ─── Utility ──────────────────────────────────────────────────────────────

    private string FormatTime(string isoTime)
    {
        try
        {
            System.DateTime t = System.DateTime.Parse(isoTime);
            System.TimeSpan diff = System.DateTime.UtcNow - t;
            if (diff.TotalMinutes < 1)  return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours   < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays    <  7) return $"{(int)diff.TotalDays}d ago";
            return t.ToString("MMM dd");
        }
        catch { return ""; }
    }

    void OnDestroy()
    {
        openButton?.onClick.RemoveAllListeners();
        closeButton?.onClick.RemoveAllListeners();
        tabComposeBtn?.onClick.RemoveAllListeners();
        tabInboxBtn?.onClick.RemoveAllListeners();
        sendBtn?.onClick.RemoveAllListeners();
        refreshInboxBtn?.onClick.RemoveAllListeners();

        if (SupportInboxManager.Instance != null)
            SupportInboxManager.Instance.OnMessagesUpdated -= OnMessagesUpdated;
    }
}
}
