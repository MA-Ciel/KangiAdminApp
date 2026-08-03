# Kangi — Admin Inbox System (Unity)

Two scripts in this folder handle the full user → admin messaging flow.

---

## ContactAdmin.cs — Send a message

**Attach to:** any GameObject in the scene (e.g. a "ContactPanel" Canvas).

| Inspector field | Type | Required |
|---|---|---|
| `messageInput` | TMP_InputField | ✅ |
| `sendButton` | Button | ✅ |
| `statusText` | TextMeshProUGUI | optional |

**How it works:**
1. User types in `messageInput` and clicks `sendButton`.
2. Calls `supportWorkflow → sendMessage` CloudScript.
3. Message is stored in PlayFab Title Internal Data (`AdminInbox`).
4. Admin sees it in the web dashboard **Messages** tab.
5. Admin replies from the dashboard → user receives a **Notification** in-game via `NotificationManager`.

---

## InboxViewer.cs — View sent messages + admin replies

**Attach to:** a separate "InboxPanel" Canvas or as a second tab inside the same panel.

| Inspector field | Type | Required |
|---|---|---|
| `tabAllBtn` | Button | optional |
| `tabOpenBtn` | Button | optional |
| `tabRepliedBtn` | Button | optional |
| `refreshBtn` | Button | optional |
| `listContainer` | Transform | ✅ |
| `messageItemPrefab` | GameObject | ✅ |
| `emptyText` | TextMeshProUGUI | optional |

### MessageItem Prefab — required child names
```
MessageItem (prefab root)
├── BodyText        TextMeshProUGUI   ← the user's original message
├── StatusChip      TextMeshProUGUI   ← "Open" / "Replied"
├── TimeText        TextMeshProUGUI   ← relative time ("2h ago")
├── ReplyContainer  GameObject        ← hide when no reply
│   └── ReplyText   TextMeshProUGUI   ← admin reply text + timestamp
```

**How it works:**
1. On `Start()` calls `Refresh()` automatically.
2. `Refresh()` calls `supportWorkflow → getMessages` CloudScript.
3. Renders one prefab per message, filtered by active tab.
4. Call `Refresh()` again any time (e.g. when panel is opened).

---

## Scene hierarchy example

```
Canvas
├── ContactPanel
│   ├── ContactAdmin.cs (component)
│   ├── MessageInput    (TMP_InputField)
│   ├── SendButton      (Button)
│   └── StatusText      (TextMeshProUGUI)
│
└── InboxPanel
    ├── InboxViewer.cs (component)
    ├── Tabs
    │   ├── TabAllBtn     (Button)
    │   ├── TabOpenBtn    (Button)
    │   └── TabRepliedBtn (Button)
    ├── RefreshBtn        (Button)
    ├── EmptyText         (TextMeshProUGUI)
    └── ListContainer     (Transform — ScrollRect content)
```

---

## Flow diagram

```
User (Unity)                  PlayFab CloudScript          Admin (Web Dashboard)
─────────────                 ───────────────────          ─────────────────────
ContactAdmin.SendMessage()
  → supportWorkflow           → stores in AdminInbox  →    Messages tab shows it
                                                           Admin types reply
  ← Notification arrives  ←  → sendNotification()    ←   "Reply" button clicked
InboxViewer.Refresh()
  → supportWorkflow           → reads AdminInbox
  ← shows reply in list
```
