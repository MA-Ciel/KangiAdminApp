using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// InboxViewer — Shows this player's sent messages and admin replies.
///
/// ─── Inspector setup ───────────────────────────────────────────
///
///   Tab buttons (optional):
///     tabAllBtn      Button   — show everything
///     tabOpenBtn     Button   — only unanswered messages
///     tabRepliedBtn  Button   — only replied messages
///     refreshBtn     Button   — manual refresh
///
///   List:
///     listContainer       Transform        — ScrollRect content parent
///     messageItemPrefab   GameObject       — spawned per message
///     emptyText           TextMeshProUGUI  — shown when list is empty
///
/// ─── MessageItem prefab required child names ───────────────────
///
///   UserNameText       TextMeshProUGUI   display name + "You"
///   UserMessageText    TextMeshProUGUI   the message you sent
///   TimeText           TextMeshProUGUI   relative time
///   StatusChip         TextMeshProUGUI   "Open" / "Replied"
///   AdminReplyBlock    GameObject        disabled until there is a reply
///     AdminReplyText   TextMeshProUGUI   admin reply text (child of above)
///     AdminTimeText    TextMeshProUGUI   reply timestamp (child of above)
///
/// ─── Data flow ─────────────────────────────────────────────────
///   Refresh() → supportWorkflow.getMyMessages → filters by currentPlayerId
///   Each item shows: username, message body, time, status badge
///   If status == "replied": AdminReplyBlock is shown with admin reply text
/// </summary>
public class InboxViewer : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────

    [Header("Tabs (all optional)")]
    [SerializeField] private Button tabAllBtn;
    [SerializeField] private Button tabOpenBtn;
    [SerializeField] private Button tabRepliedBtn;
    [SerializeField] private Button refreshBtn;

    [Header("List")]
    [SerializeField] private Transform          listContainer;
    [SerializeField] private GameObject         messageItemPrefab;
    [SerializeField] private TextMeshProUGUI    emptyText;

    [Header("Tab Colors")]
    [SerializeField] private Color activeTabColor   = new Color(0.93f, 0.28f, 0.60f);
    [SerializeField] private Color inactiveTabColor = new Color(1f, 1f, 1f, 0.4f);

    // ── Internal ──────────────────────────────────────────────

    private enum Tab { All, Open, Replied }
    private Tab currentTab = Tab.All;

    private List<MsgRecord>  allMessages  = new List<MsgRecord>();
    private List<GameObject> spawnedItems = new List<GameObject>();
    private bool isLoading = false;

    // ── Lifecycle ─────────────────────────────────────────────

    void Start()
    {
        tabAllBtn?.onClick.AddListener(()     => SwitchTab(Tab.All));
        tabOpenBtn?.onClick.AddListener(()    => SwitchTab(Tab.Open));
        tabRepliedBtn?.onClick.AddListener(() => SwitchTab(Tab.Replied));
        refreshBtn?.onClick.AddListener(Refresh);

        SwitchTab(Tab.All);
        Refresh();
    }

    // ── Public API ────────────────────────────────────────────

    /// <summary>Reload messages from PlayFab. Safe to call any time.</summary>
    public void Refresh()
    {
        if (isLoading) return;
        isLoading = true;
        ShowEmpty("Loading...");

        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName            = "supportWorkflow",
                FunctionParameter       = new { action = "getMyMessages" },
                GeneratePlayStreamEvent = false
            },
            OnFetchSuccess,
            OnFetchError
        );
    }

    // ── Fetch callbacks ───────────────────────────────────────

    private void OnFetchSuccess(ExecuteCloudScriptResult result)
    {
        isLoading = false;
        allMessages.Clear();

        // FunctionResult from PlayFab SDK is a JsonObject whose .ToString()
        // produces valid JSON — no extra serializer needed.
        string json = "";
        if (result.FunctionResult != null)
            json = result.FunctionResult.ToString();

        if (!string.IsNullOrEmpty(json))
            allMessages = ParseMessages(json);

        Debug.Log($"[InboxViewer] Loaded {allMessages.Count} messages. JSON length: {json.Length}");
        RenderList();
    }

    private void OnFetchError(PlayFabError error)
    {
        isLoading = false;
        ShowEmpty("Failed to load: " + error.ErrorMessage);
        Debug.LogError("[InboxViewer] " + error.ErrorMessage);
    }

    // ── Tab ───────────────────────────────────────────────────

    private void SwitchTab(Tab tab)
    {
        currentTab = tab;
        SetTabColor(tabAllBtn,     tab == Tab.All);
        SetTabColor(tabOpenBtn,    tab == Tab.Open);
        SetTabColor(tabRepliedBtn, tab == Tab.Replied);
        RenderList();
    }

    private void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null) lbl.color = active ? activeTabColor : inactiveTabColor;
    }

    // ── Render ────────────────────────────────────────────────

    private void RenderList()
    {
        // Destroy old items
        foreach (var go in spawnedItems) { if (go != null) Destroy(go); }
        spawnedItems.Clear();

        // Apply tab filter
        var filtered = new List<MsgRecord>();
        foreach (var m in allMessages)
        {
            bool show = currentTab == Tab.All
                     || (currentTab == Tab.Open    && m.status == "open")
                     || (currentTab == Tab.Replied && m.status == "replied");
            if (show) filtered.Add(m);
        }

        if (filtered.Count == 0)
        {
            ShowEmpty(allMessages.Count == 0
                ? "No messages yet.\nUse the input above to contact admin."
                : "No messages in this tab.");
            return;
        }

        HideEmpty();

        foreach (var msg in filtered)
        {
            if (messageItemPrefab == null || listContainer == null) break;
            GameObject item = Instantiate(messageItemPrefab, listContainer);
            spawnedItems.Add(item);
            PopulateItem(item, msg);
        }
    }

    private void PopulateItem(GameObject item, MsgRecord msg)
    {
        // ── Username ("You" + display name)
        var userNameTxt = item.transform.Find("UserNameText")?.GetComponent<TextMeshProUGUI>();
        if (userNameTxt != null)
            userNameTxt.text = string.IsNullOrEmpty(msg.displayName)
                ? "You"
                : $"You ({msg.displayName})";

        // ── Message body the user sent
        var userMsgTxt = item.transform.Find("UserMessageText")?.GetComponent<TextMeshProUGUI>();
        if (userMsgTxt != null) userMsgTxt.text = msg.body;

        // ── Timestamp
        var timeTxt = item.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
        if (timeTxt != null) timeTxt.text = FormatTime(msg.createdAt);

        // ── Status chip
        var statusTxt = item.transform.Find("StatusChip")?.GetComponent<TextMeshProUGUI>();
        if (statusTxt != null)
        {
            bool replied = msg.status == "replied";
            statusTxt.text  = replied ? "Replied" : "Open";
            statusTxt.color = replied
                ? new Color(0.30f, 1.00f, 0.50f)  // green
                : new Color(1.00f, 0.80f, 0.20f);  // amber
        }

        // ── Admin reply block (only show if there is a reply)
        bool hasReply = !string.IsNullOrEmpty(msg.adminReply);
        var replyBlock = item.transform.Find("AdminReplyBlock")?.gameObject;
        if (replyBlock != null) replyBlock.SetActive(hasReply);

        if (hasReply)
        {
            // Try both paths: direct child or inside AdminReplyBlock
            var replyTxt  = item.transform.Find("AdminReplyBlock/AdminReplyText")?.GetComponent<TextMeshProUGUI>()
                         ?? item.transform.Find("AdminReplyText")?.GetComponent<TextMeshProUGUI>();
            var replyTime = item.transform.Find("AdminReplyBlock/AdminTimeText")?.GetComponent<TextMeshProUGUI>()
                         ?? item.transform.Find("AdminTimeText")?.GetComponent<TextMeshProUGUI>();

            if (replyTxt  != null) replyTxt.text  = msg.adminReply;
            if (replyTime != null) replyTime.text  = FormatTime(msg.repliedAt);
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    private void ShowEmpty(string text)
    {
        if (emptyText == null) return;
        emptyText.gameObject.SetActive(true);
        emptyText.text = text;
    }

    private void HideEmpty()
    {
        if (emptyText != null) emptyText.gameObject.SetActive(false);
    }

    private string FormatTime(string iso)
    {
        try
        {
            DateTime  t    = DateTime.Parse(iso);
            TimeSpan  diff = DateTime.UtcNow - t;
            if (diff.TotalMinutes < 1)  return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours   < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays    <  7) return $"{(int)diff.TotalDays}d ago";
            return t.ToString("MMM dd");
        }
        catch { return ""; }
    }

    // ── JSON parser ───────────────────────────────────────────
    // Parses: { "success":true, "messages":[{...},{...}], "total":N }

    private List<MsgRecord> ParseMessages(string json)
    {
        var result = new List<MsgRecord>();
        if (string.IsNullOrEmpty(json)) return result;

        // Locate the "messages" array
        int keyIdx = json.IndexOf("\"messages\"", StringComparison.Ordinal);
        if (keyIdx == -1) return result;

        int arrStart = json.IndexOf('[', keyIdx);
        if (arrStart == -1) return result;

        // Find the matching closing bracket for this array
        int arrEnd   = -1;
        int depth    = 0;
        for (int i = arrStart; i < json.Length; i++)
        {
            if (json[i] == '[') depth++;
            else if (json[i] == ']')
            {
                depth--;
                if (depth == 0) { arrEnd = i; break; }
            }
        }
        if (arrEnd == -1) return result;

        string content = json.Substring(arrStart + 1, arrEnd - arrStart - 1).Trim();
        if (string.IsNullOrEmpty(content)) return result;

        // Split individual objects
        int objDepth = 0, objStart = -1;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '{')
            {
                if (objDepth == 0) objStart = i;
                objDepth++;
            }
            else if (content[i] == '}')
            {
                objDepth--;
                if (objDepth == 0 && objStart != -1)
                {
                    string obj = content.Substring(objStart, i - objStart + 1);
                    var rec = ParseRecord(obj);
                    if (rec != null) result.Add(rec);
                    objStart = -1;
                }
            }
        }
        return result;
    }

    private MsgRecord ParseRecord(string json)
    {
        try
        {
            return new MsgRecord
            {
                id          = Extract(json, "id"),
                playFabId   = Extract(json, "playFabId"),
                displayName = Extract(json, "displayName"),
                body        = Extract(json, "body"),
                status      = Extract(json, "status"),
                adminReply  = Extract(json, "adminReply"),
                createdAt   = Extract(json, "createdAt"),
                repliedAt   = Extract(json, "repliedAt")
            };
        }
        catch { return null; }
    }

    /// <summary>
    /// Extract a simple string value for a given JSON key.
    /// Handles escaped quotes inside values.
    /// </summary>
    private string Extract(string json, string key)
    {
        string searchKey = "\"" + key + "\"";
        int ki = json.IndexOf(searchKey, StringComparison.Ordinal);
        if (ki == -1) return "";

        int ci = json.IndexOf(':', ki + searchKey.Length);
        if (ci == -1) return "";

        // Skip whitespace after colon
        int vs = ci + 1;
        while (vs < json.Length && json[vs] == ' ') vs++;

        if (vs >= json.Length) return "";

        // Null value
        if (json[vs] != '"') return "";

        vs++; // skip opening quote

        // Read until unescaped closing quote
        var sb = new System.Text.StringBuilder();
        while (vs < json.Length)
        {
            char c = json[vs];
            if (c == '\\' && vs + 1 < json.Length)
            {
                char next = json[vs + 1];
                switch (next)
                {
                    case '"':  sb.Append('"');  vs += 2; continue;
                    case '\\': sb.Append('\\'); vs += 2; continue;
                    case 'n':  sb.Append('\n'); vs += 2; continue;
                    case 'r':  sb.Append('\r'); vs += 2; continue;
                    default:   sb.Append(next); vs += 2; continue;
                }
            }
            if (c == '"') break;
            sb.Append(c);
            vs++;
        }
        return sb.ToString();
    }

    void OnDestroy()
    {
        tabAllBtn?.onClick.RemoveAllListeners();
        tabOpenBtn?.onClick.RemoveAllListeners();
        tabRepliedBtn?.onClick.RemoveAllListeners();
        refreshBtn?.onClick.RemoveAllListeners();
    }

    // ── Data model ────────────────────────────────────────────

    private class MsgRecord
    {
        public string id;
        public string playFabId;
        public string displayName;
        public string body;
        public string status;       // "open" | "replied"
        public string adminReply;
        public string createdAt;
        public string repliedAt;
    }
}
