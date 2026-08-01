# 🔔 Notification System - Complete Guide

## Overview

The notification system automatically sends push notifications to users when:
- ✅ Account is **banned**
- ✅ Account is **unbanned**
- ✅ Audio/song is **approved**
- ✅ Audio/song is **deleted/removed**

Notifications are stored in PlayFab and displayed in the Unity app.

---

## 📁 Files Created

### CloudScript (Server-Side):
- **`cloudscript.js`** - Updated with notification functions

### Unity Scripts (Client-Side):
1. **`NotificationManager.cs`** - Core manager, fetches and handles notifications
2. **`NotificationPanel.cs`** - UI panel to display notification list
3. **`NotificationItem.cs`** - Individual notification item component

---

## 🎯 How It Works

### Server Flow:

```
Admin Dashboard Action (Ban/Unban/Approve/Delete)
       ↓
CloudScript Handler Executes
       ↓
Notification Sent to User's PlayFab Internal Data
       ↓
Stored in user's "Notifications" key
```

### Client Flow:

```
User Logs In
       ↓
NotificationManager.StartNotificationChecking()
       ↓
Periodic checks every 30 seconds
       ↓
Fetch notifications from PlayFab
       ↓
Show badge with unread count
       ↓
Display popup for important notifications (ban/unban/audio)
       ↓
User clicks notification icon → NotificationPanel opens
       ↓
User can read, mark as read, or delete
```

---

## 🚀 Setup Instructions

### 1. Deploy CloudScript

1. Open **PlayFab Dashboard**
2. Go to **Automation → Cloud Script**
3. Copy entire `cloudscript.js` content
4. Paste into editor
5. Click **Save and Publish**

✅ The notification system is now active on the server!

### 2. Unity Setup

#### A. Add NotificationManager to Scene

1. Create empty GameObject: "NotificationManager"
2. Add `NotificationManager` component
3. Configure in Inspector:
   - **Check Interval**: 30 (check every 30 seconds)
   - **Enable Debug Logs**: true (for testing)
   - **Notification Badge**: Assign UI badge icon (optional)
   - **Badge Count Text**: Assign TextMeshProUGUI for count (optional)

#### B. Create Notification UI

**Option 1: Simple Badge Icon**
```
Canvas
├── NotificationButton
│   ├── Icon (Image)
│   └── Badge (Red dot)
│       └── CountText (TextMeshProUGUI) "3"
```

**Option 2: Full Notification Panel**
```
Canvas
├── NotificationPanel (GameObject)
│   ├── Background (Image)
│   ├── Header
│   │   ├── Title (Text): "Notifications"
│   │   ├── CloseButton (Button)
│   │   └── MarkAllReadButton (Button)
│   ├── ScrollView
│   │   └── Content (Transform) ← notificationListContainer
│   └── EmptyText (Text): "No notifications"
```

#### C. Create Notification Item Prefab

1. Create GameObject: "NotificationItem"
2. Add components:
   - `Image` (background)
   - `Button` (click to mark as read)
   - `NotificationItem` script

3. Structure:
```
NotificationItem (Prefab)
├── BackgroundImage (Image)
├── TypeIcon (Image) ← Color changes based on type
├── TitleText (TextMeshProUGUI)
├── MessageText (TextMeshProUGUI)
├── TimeText (TextMeshProUGUI) "2h ago"
├── UnreadDot (Image - red dot)
└── DeleteButton (Button)
```

4. Save as Prefab

#### D. Connect NotificationPanel

1. Add `NotificationPanel` component to your panel
2. In Inspector:
   - **Notification Panel**: Assign panel GameObject
   - **Notification List Container**: Assign ScrollView/Content
   - **Notification Item Prefab**: Assign your prefab
   - **Close Button**: Assign button
   - **Mark All Read Button**: Assign button

### 3. Integrate with Login

Update your login script to start notifications:

```csharp
void OnLoginSuccess()
{
    // Your existing login code...
    
    // Start notification checking
    if (NotificationManager.Instance != null)
    {
        NotificationManager.Instance.StartNotificationChecking();
    }
}
```

### 4. Add Notification Button

Add a button to open notifications:

```csharp
public void OnNotificationButtonClicked()
{
    NotificationPanel notifPanel = FindObjectOfType<NotificationPanel>();
    if (notifPanel != null)
    {
        notifPanel.Show();
    }
}
```

---

## 📋 Notification Types

| Type | Color | Trigger | Message |
|------|-------|---------|---------|
| `ban` | 🔴 Red | User banned | "Your account has been temporarily suspended" |
| `unban` | 🟢 Green | User unbanned | "Your account suspension has been lifted. Welcome back!" |
| `audio_approved` | 🔵 Cyan | Song approved | "Your song '[Title]' has been approved and is now live!" |
| `audio_deleted` | 🟡 Yellow | Song deleted | "Your song '[Title]' has been removed from the platform" |
| `info` | ⚪ White | General info | Custom message |
| `success` | 🟢 Green | Success | Custom message |
| `warning` | 🟡 Yellow | Warning | Custom message |
| `error` | 🔴 Red | Error | Custom message |

---

## 🔧 API Reference

### NotificationManager

#### Methods:

```csharp
// Start periodic checking (auto-called on login)
NotificationManager.Instance.StartNotificationChecking();

// Stop checking
NotificationManager.Instance.StopNotificationChecking();

// Manual check for new notifications
NotificationManager.Instance.CheckForNewNotifications();

// Get lightweight unread count only
NotificationManager.Instance.GetUnreadCount();

// Mark specific notification as read
NotificationManager.Instance.MarkAsRead(notificationId);

// Mark all as read
NotificationManager.Instance.MarkAllAsRead();

// Delete a notification
NotificationManager.Instance.DeleteNotification(notificationId);

// Get all notifications
List<Notification> notifications = NotificationManager.Instance.GetNotifications();
```

#### Events:

```csharp
// Subscribe to unread count changes
NotificationManager.Instance.OnUnreadCountChanged += (count) => {
    Debug.Log($"Unread: {count}");
};

// Subscribe to notification updates
NotificationManager.Instance.OnNotificationsReceived += (notifications) => {
    Debug.Log($"Received {notifications.Count} notifications");
};
```

### NotificationPanel

```csharp
// Show notification panel
NotificationPanel.Instance.Show();

// Hide notification panel
NotificationPanel.Instance.Hide();
```

---

## 🧪 Testing

### Test Ban Notification:

1. **Admin Dashboard:**
   - Go to "Users" tab
   - Ban a test user

2. **Unity App (as that user):**
   - Wait up to 30 seconds (or call `CheckForNewNotifications()`)
   - Notification badge appears with count: "1"
   - Popup shows: "Account Suspended: Your account has been temporarily suspended..."
   - Click notification icon to see full list

### Test Unban Notification:

1. **Admin Dashboard:**
   - Go to "Users" tab
   - Unban the user

2. **Unity App:**
   - Notification appears: "Account Restored: Your account suspension has been lifted. Welcome back!"

### Test Audio Approval:

1. **Admin Dashboard:**
   - Go to "Audio Management"
   - Approve a pending song

2. **Unity App (as song owner):**
   - Notification: "Song Approved! Your song '[Title]' has been approved and is now live!"

### Test Audio Deletion:

1. **Admin Dashboard:**
   - Delete a song

2. **Unity App (as song owner):**
   - Notification: "Song Removed: Your song '[Title]' has been removed from the platform"

---

## 💾 Data Structure

### PlayFab Storage:

**Location:** User Internal Data → Key: `"Notifications"`

**Format:**
```json
[
  {
    "id": "notif_1a2b3c4d_xyz123",
    "title": "Account Suspended",
    "message": "Your account has been temporarily suspended. Please contact support for more information.",
    "type": "ban",
    "data": {
      "reason": "Banned via Kangi Admin Dashboard"
    },
    "read": false,
    "createdAt": "2024-11-20T15:30:00Z"
  },
  {
    "id": "notif_5e6f7g8h_abc456",
    "title": "Song Approved!",
    "message": "Your song 'My Track' has been approved and is now live!",
    "type": "audio_approved",
    "data": {
      "songId": "song_12345",
      "songTitle": "My Track"
    },
    "read": true,
    "createdAt": "2024-11-20T14:00:00Z"
  }
]
```

**Limits:**
- Max 50 notifications per user (oldest auto-deleted)
- Stored in User Internal Data (private, server-only access)

---

## ⚙️ Configuration

### Change Check Interval:

```csharp
// In NotificationManager Inspector or code:
checkInterval = 60f; // Check every 60 seconds instead of 30
```

### Disable Auto-Popup:

Comment out in `NotificationManager.cs`:

```csharp
private void OnGetNotificationsSuccess(...)
{
    // ...
    // ShowImportantNotifications(); // ← Comment this line
}
```

### Custom Notification Types:

Add your own types in CloudScript:

```javascript
sendNotification(
    targetPlayFabId,
    "Custom Title",
    "Custom message here",
    "custom_type", // ← Your custom type
    { customData: "value" }
);
```

Then handle in Unity:

```csharp
// In NotificationItem.cs Setup():
case "custom_type":
    typeIcon.color = Color.magenta;
    break;
```

---

## 🐛 Troubleshooting

### "Notifications not appearing"

**Check:**
1. CloudScript deployed and published?
2. `NotificationManager` in scene and active?
3. User logged in to PlayFab?
4. Debug logs enabled? Check console for errors

**Test manually:**
```csharp
NotificationManager.Instance.CheckForNewNotifications();
```

### "Badge not updating"

**Check:**
1. Badge UI references assigned in Inspector?
2. `OnUnreadCountChanged` event firing? (check logs)

### "Popup not showing for ban/unban"

**Check:**
1. `NotifcationPanel.Instance` exists?
2. Notification type is exactly "ban" or "unban"?
3. `ShowImportantNotifications()` not commented out?

### "Old notifications not clearing"

Only last 50 notifications are kept. To manually clear:

```csharp
// Mark all as read
NotificationManager.Instance.MarkAllAsRead();

// Or delete specific ones
NotificationManager.Instance.DeleteNotification(notifId);
```

---

## 🎨 UI Customization

### Badge Styles:

**Red Dot Badge:**
- Create small circular Image (Red)
- Position: top-right of notification icon
- Show/hide based on `unreadCount > 0`

**Count Badge:**
- Add TextMeshProUGUI inside badge
- Font size: 10-12
- Text: "3" or "99+"

### Notification Colors:

Edit in `NotificationItem.cs`:

```csharp
case "ban":
    typeIcon.color = new Color(1f, 0.2f, 0.2f); // Change red shade
    break;
```

### Time Format:

Edit `FormatTime()` in `NotificationItem.cs`:

```csharp
if (diff.TotalDays < 30) return $"{(int)diff.TotalDays}d ago";
return time.ToString("MM/dd/yyyy"); // Show full date
```

---

## 📊 Statistics

Track notification metrics:

```csharp
// Total notifications received
int total = NotificationManager.Instance.GetNotifications().Count;

// Unread count
int unread = NotificationManager.Instance.GetUnreadCount();

// Notifications by type
var notifications = NotificationManager.Instance.GetNotifications();
int bans = notifications.Count(n => n.type == "ban");
int audioApproved = notifications.Count(n => n.type == "audio_approved");
```

---

## 🔐 Security Notes

✅ **Secure:**
- Notifications stored in User **Internal** Data (server-only access)
- Users cannot fake or create their own notifications
- Only CloudScript can write notifications

✅ **Private:**
- Users can only see their own notifications
- Admin dashboard doesn't see user notifications (privacy)

---

## 🎉 Complete!

Your notification system is fully integrated! Users will now receive real-time notifications for:
- Ban/Unban actions
- Audio approvals
- Audio deletions

**Next Steps:**
1. Deploy CloudScript
2. Setup Unity UI
3. Test with ban/unban flow
4. Customize UI to match your app design

---

**Questions?** Check console logs with `enableDebugLogs = true` for detailed debugging.
