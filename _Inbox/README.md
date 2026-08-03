# Kangi — Admin Inbox System (Unity)

Two scripts. User sends a message → admin replies from web dashboard → reply shows in Unity.

---

## ContactAdmin.cs

Attach to any Canvas GameObject.

| Inspector field | Type | Required |
|---|---|---|
| `messageInput` | TMP_InputField | ✅ |
| `sendButton` | Button | ✅ |
| `statusText` | TextMeshProUGUI | optional |
| `inboxViewer` | InboxViewer | optional — auto-refreshes list after send |

**What happens on Send:**
1. Calls `supportWorkflow → sendMessage` CloudScript
2. Message stored in PlayFab Title Internal Data (`AdminInbox`)
3. `statusText` shows "Sent! Admin will reply soon." in green
4. If `inboxViewer` is assigned, calls `inboxViewer.Refresh()` so the new message appears in the list immediately

---

## InboxViewer.cs

Attach to a separate InboxPanel Canvas (or same Canvas, different tab).

| Inspector field | Type | Required |
|---|---|---|
| `listContainer` | Transform | ✅ — ScrollRect content object |
| `messageItemPrefab` | GameObject | ✅ — spawned per message |
| `emptyText` | TextMeshProUGUI | optional |
| `tabAllBtn` | Button | optional |
| `tabOpenBtn` | Button | optional |
| `tabRepliedBtn` | Button | optional |
| `refreshBtn` | Button | optional |

### MessageItem Prefab — required child names

```
MessageItem  (root)
├── UserNameText       TextMeshProUGUI   "You (DisplayName)"
├── UserMessageText    TextMeshProUGUI   message body the user sent
├── TimeText           TextMeshProUGUI   "2h ago"
├── StatusChip         TextMeshProUGUI   "Open" (amber) or "Replied" (green)
└── AdminReplyBlock    GameObject        hidden until admin replies
    ├── AdminReplyText TextMeshProUGUI   admin reply text
    └── AdminTimeText  TextMeshProUGUI   reply time "5m ago"
```

**What Refresh() does:**
1. Calls `supportWorkflow → getMyMessages` (only this player's messages)
2. Filters by current tab (All / Open / Replied)
3. Spawns one prefab per message
4. `AdminReplyBlock` is activated only when `adminReply` is not empty

---

## Scene hierarchy

```
Canvas
├── ContactPanel
│   ├── ContactAdmin (component)
│   ├── MessageInput      TMP_InputField
│   ├── SendButton        Button
│   ├── StatusText        TextMeshProUGUI
│   └── InboxViewer ref → (drag InboxPanel here)
│
└── InboxPanel
    ├── InboxViewer (component)
    ├── Tabs
    │   ├── TabAll       Button
    │   ├── TabOpen      Button
    │   └── TabReplied   Button
    ├── RefreshBtn       Button
    ├── EmptyText        TextMeshProUGUI
    └── ScrollRect
        └── Content (ListContainer Transform)
```

---

## Full flow

```
User types in Unity (ContactAdmin)
  → supportWorkflow.sendMessage
  → stored in AdminInbox (Title Internal Data)
  → InboxViewer.Refresh() auto-called → message shown in list with status "Open"

Admin opens web dashboard Messages tab
  → sees message with username + body
  → types reply → clicks Reply button
  → supportWorkflow.replyMessage
  → status flips to "replied"
  → sendNotification fires → user gets in-game notification

User opens InboxViewer (or it auto-refreshes)
  → getMyMessages returns their messages
  → AdminReplyBlock shown with admin reply text + time
```

---

## Admin dashboard (web)

The Messages tab in the admin web dashboard shows:
- Player display name + PlayFab ID
- Message body
- Open / Replied status badge
- Timestamp
- Inline reply input (Enter or Reply button to send)
- Previously sent reply shown in green block
- Delete button to remove the message
