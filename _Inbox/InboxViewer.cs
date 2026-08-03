using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// InboxViewer
/// ─────────────────────────────────────────────────────────────
/// Shows the player's sent messages and admin replies in a
/// scrollable list with tabs (All / Open / Replied).
///
/// Inspector setup:
///   Panel root
///   ├─ TabAll        Button
///   ├─ TabOpen       Button
///   ├─ TabReplied    Button
///   ├─ RefreshBtn    Button
///   ├─ EmptyText     TextMeshProUGUI
///   └─ ListContainer Transform  ← parent for MessageItem prefabs
///
/// MessageItem prefab needs these named children:
///   BodyText         TextMeshProUGUI
///   StatusChip       TextMeshProUGUI
///   TimeText         TextMeshProUGUI
///   ReplyContainer   GameObject      (active only when there is a reply)
///   ReplyText        TextMeshProUGUI (inside ReplyContainer)
///
/// Flow:
///   1. InboxViewer.Refresh() fetches AdminInbox from PlayFab UserData
///      (CloudScript supportWorkflow → getMessages reads Title Internal)
///   2. Instantiates one prefab per message
///   3. Admin replies appear as Notifications in NotificationManager
///      AND are reflected next time Refresh() is called
/// </summary>
public class InboxViewer : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────

    [Header("Tabs")]
    [SerializeField] private Button tabAllBtn;
    [SerializeField] private Button tabOpenBtn;
    [SerializeField] private Button tabRepliedBtn;

    [Header("List")]
    [SerializeField] private Transform   listContainer;
    [SerializeField] private GameObject  messageItemPrefab;
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private Button      refreshBtn;

    [Header("Tab Colors")]
    [SerializeField] private Color activeTabColor   = new Color(0.93f, 0.28f, 0.60f);
    [SerializeField] private Color inactiveTabColor = new Color(1f, 1f, 1f, 0.4f);

    // ── Internal state ────────────────────────────────────────

    private enum TabFilter { All, Open, Replied }
    private TabFilter currentTab = TabFilter.All;

    private List<MessageRecord> allMessages = new List<MessageRecord>();
    private List<GameObject>    spawnedItems = new List<GameObject>();

    // ── Lifecycle ─────────────────────────────────────────────

    void Start()
    {
        tabAllBtn?.onClick.AddListener(() => SetTab(TabFilter.All));
        tabOpenBtn?.onClick.AddListener(() => SetTab(TabFilter.Open));
        tabRepliedBtn?.onClick.AddListener(() => SetTab(TabFilter.Replied));
        refreshBtn?.onClick.AddListener(Refresh);

        SetTab(TabFilter.All);
        Refresh();
    }

    // ── Public ────────────────────────────────────────────────

    /// <summary>Call this to reload messages from the server.</summary>
    public void Refresh()
    {
        ShowEmpty("Loading...");

        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "supportWorkflow",
                FunctionParameter = new { action = "getMessages" },
                GeneratePlayStreamEvent = false
            },
            result =>
            {
                allMessages.Clear();

                string json = result.FunctionResult?.ToString() ?? "";
                // Parse the messages array from the JSON string
                allMessages = ParseMessages(json);

                RenderList();
            },
            error =>
            {
                ShowEmpty("Failed to load: " + error.ErrorMessage);
                Debug.LogError("[InboxViewer] " + error.ErrorMessage);
            }
        );
    }

    // ── Tab ───────────────────────────────────────────────────

    private void SetTab(TabFilter tab)
    {
        currentTab = tab;
        SetTabColor(tabAllBtn,     tab == TabFilter.All);
        SetTabColor(tabOpenBtn,    tab == TabFilter.Open);
        SetTabColor(tabRepliedBtn, tab == TabFilter.Replied);
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
        // Clear old items
        foreach (var go in spawnedItems) Destroy(go);
        spawnedItems.Clear();

        // Filter
        var filtered = new List<MessageRecord>();
        foreach (var m in allMessages)
        {
            bool show = currentTab == TabFilter.All
                     || (currentTab == TabFilter.Open    && m.status == "open")
                     || (currentTab == TabFilter.Replied && m.status == "replied");
            if (show) filtered.Add(m);
        }

        if (filtered.Count == 0)
        {
            ShowEmpty(allMessages.Count == 0
                ? "You haven't sent any messages yet."
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

    private void PopulateItem(GameObject item, MessageRecord msg)
    {
        // ── Message body
        var bodyTxt = item.transform.Find("BodyText")?.GetComponent<TextMeshProUGUI>();
        if (bodyTxt != null) bodyTxt.text = msg.body;

        // ── Status chip
        var statusTxt = item.transform.Find("StatusChip")?.GetComponent<TextMeshProUGUI>();
        if (statusTxt != null)
        {
            if (msg.status == "open")
            {
                statusTxt.text  = "Open";
                statusTxt.color = new Color(1f, 0.8f, 0.2f);  // amber
            }
            else
            {
                statusTxt.text  = "Replied";
                statusTxt.color = new Color(0.3f, 1f, 0.5f);  // green
            }
        }

        // ── Timestamp
        var timeTxt = item.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
        if (timeTxt != null) timeTxt.text = FormatTime(msg.createdAt);

        // ── Admin reply section
        var replyContainer = item.transform.Find("ReplyContainer")?.gameObject;
        var replyTxt       = item.transform.Find("ReplyText")?.GetComponent<TextMeshProUGUI>();
        bool hasReply = !string.IsNullOrEmpty(msg.adminReply);

        if (replyContainer != null) replyContainer.SetActive(hasReply);
        if (replyTxt != null && hasReply)
            replyTxt.text = $"Admin ({FormatTime(msg.repliedAt)}):\n{msg.adminReply}";
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
            DateTime t    = DateTime.Parse(iso);
            TimeSpan diff = DateTime.UtcNow - t;
            if (diff.TotalMinutes < 1)  return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours   < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays    <  7) return $"{(int)diff.TotalDays}d ago";
            return t.ToString("MMM dd");
        }
        catch { return ""; }
    }

    // ── Minimal JSON parser (no external deps) ────────────────

    private List<MessageRecord> ParseMessages(string json)
    {
        var result = new List<MessageRecord>();
        if (string.IsNullOrEmpty(json)) return result;

        // Find "messages":[...]
        int arrKey = json.IndexOf("\"messages\"");
        if (arrKey == -1) return result;

        int arrStart = json.IndexOf("[", arrKey);
        int arrEnd   = json.LastIndexOf("]");
        if (arrStart == -1 || arrEnd == -1 || arrEnd <= arrStart) return result;

        string content = json.Substring(arrStart + 1, arrEnd - arrStart - 1).Trim();
        if (string.IsNullOrEmpty(content)) return result;

        int depth = 0, objStart = -1;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '{') { if (depth == 0) objStart = i; depth++; }
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0 && objStart != -1)
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

    private MessageRecord ParseRecord(string json)
    {
        try
        {
            return new MessageRecord
            {
                id         = Extract(json, "id"),
                body       = Extract(json, "body"),
                status     = Extract(json, "status"),
                adminReply = Extract(json, "adminReply"),
                createdAt  = Extract(json, "createdAt"),
                repliedAt  = Extract(json, "repliedAt")
            };
        }
        catch { return null; }
    }

    private string Extract(string json, string key)
    {
        string k = "\"" + key + "\"";
        int ki = json.IndexOf(k);
        if (ki == -1) return "";
        int ci = json.IndexOf(":", ki);
        if (ci == -1) return "";
        int vs = json.IndexOf("\"", ci);
        if (vs == -1) return "";
        vs++;
        int ve = json.IndexOf("\"", vs);
        if (ve == -1) return "";
        return json.Substring(vs, ve - vs);
    }

    void OnDestroy()
    {
        tabAllBtn?.onClick.RemoveAllListeners();
        tabOpenBtn?.onClick.RemoveAllListeners();
        tabRepliedBtn?.onClick.RemoveAllListeners();
        refreshBtn?.onClick.RemoveAllListeners();
    }

    // ── Data model ────────────────────────────────────────────

    private class MessageRecord
    {
        public string id;
        public string body;
        public string status;      // "open" | "replied"
        public string adminReply;
        public string createdAt;
        public string repliedAt;
    }
}
