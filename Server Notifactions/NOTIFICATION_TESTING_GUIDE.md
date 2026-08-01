# 🧪 Notification System - Complete Testing Guide

## ⚠️ If Notifications Are Not Working

Follow these steps to debug and fix the issue.

---

## 🔍 Step 1: Deploy Updated CloudScript

**CRITICAL:** Make sure you deploy the latest cloudscript.js!

1. Open **PlayFab Dashboard**
2. Go to **Automation → Cloud Script**
3. **Delete ALL existing code**
4. Copy the entire `cloudscript.js` file
5. Paste it
6. Click **Save and Publish** (NOT just Save!)
7. Refresh the page to confirm it saved

---

## 🧪 Step 2: Test CloudScript Functions

### Test A: Send Test Notification

**In PlayFab Dashboard:**
1. Go to **Automation → Cloud Script**
2. Click **"Revisions"** tab
3. Find your latest revision → Click **"Test"**
4. Enter:

```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "sendTest"
  }
}
```

5. Click **"Run Function"**

**Expected Result:**
```json
{
  "success": true,
  "message": "Notification sent successfully.",
  "notification": {
    "id": "notif_...",
    "title": "Test Notification",
    "message": "This is a test notification...",
    "type": "info",
    "read": false,
    "createdAt": "2024-11-20T..."
  }
}
```

❌ **If you get an error:**
- CloudScript not deployed properly
- Function name typo
- PlayFab API issue

---

### Test B: Check If Notification Saved

**In PlayFab Dashboard:**

Test again with:
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "checkStorage"
  }
}
```

**Expected Result:**
```json
{
  "success": true,
  "rawValue": "[{\"id\":\"notif_...\",\"title\":\"Test Notification\",...}]",
  "lastUpdated": "2024-11-20T..."
}
```

✅ **If you see this:** Notifications ARE saving to PlayFab!

❌ **If "success": false:** Notifications not saving - check CloudScript logs

---

### Test C: Get All Notifications

Test with:
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "getAll"
  }
}
```

**Expected Result:**
```json
{
  "success": true,
  "notifications": [
    {
      "id": "notif_...",
      "title": "Test Notification",
      "message": "...",
      "type": "info",
      "read": false,
      "createdAt": "..."
    }
  ],
  "unreadCount": 1,
  "total": 1
}
```

✅ **If you see notifications:** CloudScript is working perfectly!

---

### Test D: Test Ban Notification

```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "testBan"
  }
}
```

**Expected:** Ban notification created

---

### Test E: Test Audio Approve Notification

```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "testAudioApprove"
  }
}
```

**Expected:** Audio approved notification created

---

### Test F: Test Audio Delete Notification

```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "testAudioDelete"
  }
}
```

**Expected:** Audio deleted notification created

---

### Test G: Send Multiple Notifications

```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "sendMultiple",
    "count": 5
  }
}
```

**Expected:** 5 notifications created

---

## 🎯 Step 3: Verify in PlayFab Storage

### Method 1: Through Dashboard

1. **Players** tab → Search for your test user
2. Click on player name
3. **Player Data** tab
4. **Internal Data** section
5. Find key: **"Notifications"**
6. Click **View**

**Expected:** JSON array with notifications

---

### Method 2: Through Test Function

Already tested in Test B above.

---

## 🔧 Step 4: Test Real Actions

### Test Ban/Unban Flow:

1. **Admin Dashboard** (your admin panel)
2. Go to **Users** tab
3. Select a test user
4. Click **Ban**

**Then check:**
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "getAll",
    "targetPlayFabId": "USER_PLAYFAB_ID_HERE"
  }
}
```

**Expected:** Ban notification appears

### Test Audio Approve Flow:

1. **Admin Dashboard**
2. Go to **Audio Management**
3. Select pending song
4. Click **Approve**

**Important:** Song MUST have `uploaderId` or `ownerId` field!

**Check if song has owner:**
```json
{
  "FunctionName": "videoAppWorkflow",
  "FunctionParameter": {
    "action": "getSongs"
  }
}
```

Look for:
```json
{
  "SongId": "song_123",
  "title": "My Song",
  "uploaderId": "ABC123",  // ← MUST EXIST!
  ...
}
```

❌ **If `uploaderId` missing:** Notification won't be sent (no one to send to!)

**Fix:** When submitting songs, always include `uploaderId`:

```javascript
// In your app, when uploading song:
var songData = {
    SongId: generateId(),
    title: "My Song",
    uploaderId: currentPlayerId,  // ← ADD THIS!
    // ... other fields
};
```

---

## 🔴 Common Issues & Solutions

### Issue 1: "CloudScript returns nothing"

**Cause:** CloudScript not deployed properly

**Fix:**
1. Delete all code in CloudScript editor
2. Copy ENTIRE cloudscript.js
3. Paste
4. **Save and Publish** (not just Save)
5. Refresh page
6. Try test again

---

### Issue 2: "Notification sent but not appearing in storage"

**Cause:** Using wrong data type (User Data instead of User Internal Data)

**Check CloudScript uses:**
```javascript
server.UpdateUserInternalData({  // ← INTERNAL DATA!
    PlayFabId: targetPlayFabId,
    Data: {
        Notifications: JSON.stringify(notifications)
    }
});
```

**NOT:**
```javascript
server.UpdateUserData({  // ← WRONG! This is public data
    ...
});
```

---

### Issue 3: "Audio approve/delete doesn't send notification"

**Cause:** Song missing `uploaderId` or `ownerId`

**Fix:** Update song submission to include owner:

```javascript
// When submitting song:
{
    action: "submit",
    songData: {
        SongId: "song_123",
        title: "My Song",
        uploaderId: currentPlayerId,  // ← ADD THIS
        ownerId: currentPlayerId,     // ← OR THIS
        // ... other fields
    }
}
```

**Test existing songs:**
```json
{
  "FunctionName": "videoAppWorkflow",
  "FunctionParameter": {
    "action": "getSongs"
  }
}
```

Check if songs have `uploaderId` or `ownerId` field.

---

### Issue 4: "Unity app not receiving notifications"

**Cause 1:** NotificationManager not started

**Fix:**
```csharp
// On login success:
NotificationManager.Instance.StartNotificationChecking();
```

**Cause 2:** PlayFab not logged in

**Fix:** Make sure PlayFabClientAPI.IsClientLoggedIn() returns true

**Cause 3:** JSON parsing error

**Fix:** Check Unity console for errors. The updated NotificationManager.cs has custom parsing that should work.

---

### Issue 5: "Badge not showing in Unity"

**Check:**
1. `notificationBadge` GameObject assigned in Inspector?
2. `badgeCountText` TextMeshProUGUI assigned?
3. Is `NotificationManager.Instance` not null?

**Debug:**
```csharp
void Update() {
    // Temporary debug
    if (Input.GetKeyDown(KeyCode.T)) {
        NotificationManager.Instance.CheckForNewNotifications();
    }
}
```

Press T key → Check console for:
```
[NotificationManager] Checking for new notifications...
[NotificationManager] Received X notifications, Y unread
```

---

### Issue 6: "Notification sent to wrong user"

**Check targetPlayFabId:**

In CloudScript, when calling `sendNotification`:
```javascript
// Correct:
sendNotification(
    targetSongOwnerId,  // ← Owner's PlayFabId
    "Song Approved!",
    ...
);

// Wrong:
sendNotification(
    currentPlayerId,  // ← Admin's PlayFabId (wrong!)
    ...
);
```

---

## ✅ Complete Test Checklist

Use this checklist to test everything:

### CloudScript Tests:
- [ ] Test A: Send test notification ✓
- [ ] Test B: Check storage ✓
- [ ] Test C: Get all notifications ✓
- [ ] Test D: Test ban notification ✓
- [ ] Test E: Test audio approve ✓
- [ ] Test F: Test audio delete ✓
- [ ] Test G: Send multiple notifications ✓

### PlayFab Storage Tests:
- [ ] Notifications visible in Player Data → Internal Data ✓
- [ ] JSON format is correct ✓
- [ ] Multiple notifications stored properly ✓

### Real Action Tests:
- [ ] Ban user → Notification appears ✓
- [ ] Unban user → Notification appears ✓
- [ ] Approve song → Notification appears ✓
- [ ] Delete song → Notification appears ✓

### Unity App Tests:
- [ ] NotificationManager starts on login ✓
- [ ] Badge shows unread count ✓
- [ ] Popup appears for ban/unban/audio ✓
- [ ] Notification list displays correctly ✓
- [ ] Mark as read works ✓
- [ ] Delete notification works ✓

---

## 🎯 Quick Debug Commands

### Clear All Notifications (Start Fresh):
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "clearAll"
  }
}
```

### Send 5 Test Notifications:
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "sendMultiple",
    "count": 5
  }
}
```

### Check Raw Storage:
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "checkStorage"
  }
}
```

---

## 📊 Expected Results Summary

### After CloudScript Tests:

**PlayFab Dashboard → Players → [Your User] → Player Data → Internal Data:**
```
Key: Notifications
Value: [{...}, {...}, {...}]
Last Updated: 2024-11-20 15:30:00
```

### After Unity App Tests:

**Unity Console:**
```
[NotificationManager] Checking for new notifications...
[NotificationManager] Received 5 notifications, 3 unread
[NotificationManager] Notification sent to ABC123: Test Notification
```

**Unity UI:**
```
Badge: "3" (unread count)
Popup: "Test Notification: This is a test..."
```

---

## 🆘 Still Not Working?

1. **Copy CloudScript logs:**
   - PlayFab Dashboard → Automation → Cloud Script → Logs
   
2. **Copy Unity console errors:**
   - Unity Console → Filter by "NotificationManager"

3. **Check these files are latest version:**
   - [ ] `cloudscript.js` - Updated with notification system
   - [ ] `NotificationManager.cs` - Custom JSON parsing (no PlayFab.Json.JsonWrapper)
   - [ ] `NotificationPanel.cs` - UI panel script
   - [ ] `NotificationItem.cs` - Item prefab script

4. **Verify CloudScript published:**
   - Automation → Cloud Script → Revisions
   - Latest revision should be marked as "Live"

---

## 🎉 Success Criteria

✅ **Notifications are working when:**

1. Test functions return `"success": true`
2. PlayFab storage shows notifications array
3. Unity console shows "Received X notifications"
4. Badge displays unread count
5. Popup shows for important notifications
6. Notification list displays correctly

**All tests pass = System is working! 🎊**
