# ✅ Complete Notification System Test Checklist

## 🎯 Testing Methods Available

You have **3 ways** to test the notification system:

1. **Browser Test** (Easiest) - `test-notifications.html`
2. **PlayFab Dashboard Test** - Manual CloudScript testing
3. **Unity Test** - Using NotificationTester.cs

---

## 🌐 Method 1: Browser Test (Recommended)

### Step 1: Open Test Page
```
1. Open file: test-notifications.html
2. Double-click to open in browser
```

### Step 2: Login
```
Title ID: 182E5E
Email: [Your PlayFab account email]
Password: [Your password]
Click "Login to PlayFab"
```

### Step 3: Run Tests
```
Click each test button one by one:
✓ Test 1: Send Test
✓ Test 2: Check Storage
✓ Test 3: Get All
✓ Test 4: Ban Notification
✓ Test 5: Unban Notification
✓ Test 6: Audio Approve
✓ Test 7: Audio Delete
✓ Test 8: Multiple

OR

Click "Run All Tests" button
```

### Expected Results:
- All badges turn GREEN with "✓ Passed"
- Console shows SUCCESS messages
- No red ERROR messages

---

## 🎮 Method 2: PlayFab Dashboard Test

### Step 1: Deploy CloudScript
```
1. PlayFab Dashboard
2. Automation → Cloud Script
3. DELETE all existing code
4. Copy cloudscript.js
5. Paste
6. Save and Publish (NOT just Save!)
7. Refresh page
```

### Step 2: Run Tests

Go to: **Cloud Script → Revisions → [Latest] → Test**

#### Test 1: Send Test Notification
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "sendTest"
  }
}
```
**Expected:** `"success": true`

#### Test 2: Check Storage
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "checkStorage"
  }
}
```
**Expected:** `"success": true, "rawValue": "[...]"`

#### Test 3: Get All Notifications
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "getAll"
  }
}
```
**Expected:** `"success": true, "notifications": [...]`

#### Test 4: Test Ban
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "testBan"
  }
}
```
**Expected:** `"success": true, "notification": {...}`

#### Test 5: Test Unban
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "testUnban"
  }
}
```
**Expected:** `"success": true`

#### Test 6: Test Audio Approve
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "testAudioApprove"
  }
}
```
**Expected:** `"success": true`

#### Test 7: Test Audio Delete
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "testAudioDelete"
  }
}
```
**Expected:** `"success": true`

#### Test 8: Send Multiple
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "sendMultiple",
    "count": 5
  }
}
```
**Expected:** `"success": true, "message": "Sent 5 notifications"`

#### Test 9: Clear All (Cleanup)
```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "clearAll"
  }
}
```
**Expected:** `"success": true`

---

## 🎯 Method 3: Unity Test

### Step 1: Add Tester Script
```
1. Open Unity project
2. Add NotificationTester.cs to a GameObject
3. Assign to Canvas or create UI buttons
```

### Step 2: Run Tests

**Keyboard Shortcuts:**
- **T** - Send test notification
- **G** - Get all notifications
- **C** - Clear all
- **M** - Test NotificationManager
- **0** - Run all tests

### Step 3: Check Console

**Expected Output:**
```
[NotificationTester] Testing: Send Notification
[NotificationTester] ✓ [Send Test] SUCCESS: {...}
[NotificationManager] Checking for new notifications...
[NotificationManager] Received X notifications, Y unread
```

---

## 📊 Verification Checklist

### CloudScript Verification:
- [ ] CloudScript deployed successfully
- [ ] All test functions return `"success": true`
- [ ] No JavaScript errors in logs
- [ ] `checkStorage` returns notifications array

### PlayFab Storage Verification:
- [ ] Go to Players → [Your User] → Player Data
- [ ] Internal Data section has key "Notifications"
- [ ] Value is valid JSON array
- [ ] Notifications have correct structure

### Unity App Verification:
- [ ] NotificationManager starts on login
- [ ] Console shows "Received X notifications"
- [ ] Badge displays unread count
- [ ] Popup shows for ban/unban/audio
- [ ] Notification panel displays list

### Real Action Verification:
- [ ] Ban user → Notification appears
- [ ] Unban user → Notification appears
- [ ] Approve song → Notification appears (if song has uploaderId)
- [ ] Delete song → Notification appears (if song has uploaderId)

---

## ⚠️ Common Issues & Solutions

### Issue 1: "Function not found"
**Cause:** CloudScript not deployed
**Fix:** 
1. Delete all code in CloudScript editor
2. Copy cloudscript.js
3. Save and Publish
4. Refresh page

### Issue 2: "success": false
**Cause:** Logic error or missing data
**Fix:** Check CloudScript logs for error details

### Issue 3: Storage empty after sending
**Cause:** Using wrong PlayFab data type
**Check:** CloudScript uses `UpdateUserInternalData` not `UpdateUserData`

### Issue 4: Audio notifications not working
**Cause:** Songs missing `uploaderId` field
**Fix:** When submitting songs, add:
```javascript
{
    uploaderId: currentPlayerId,  // ADD THIS
    ownerId: currentPlayerId,     // OR THIS
    ...
}
```

### Issue 5: Unity not receiving
**Cause:** NotificationManager not started
**Fix:**
```csharp
// On login:
NotificationManager.Instance.StartNotificationChecking();
```

### Issue 6: JSON parsing error in Unity
**Cause:** Using old PlayFab.Json.JsonWrapper
**Fix:** Use updated NotificationManager.cs with custom parsing

---

## 🎯 Quick Test Script

Copy-paste into PlayFab Dashboard Cloud Script Test:

```json
{
  "FunctionName": "testNotificationSystem",
  "FunctionParameter": {
    "testType": "sendTest"
  }
}
```

If this returns `"success": true`, your system is working! ✅

---

## 📁 All Test Files Created

| File | Purpose |
|------|---------|
| `test-notifications.html` | Browser-based testing tool |
| `TEST_CLOUDSCRIPT.json` | Test cases reference |
| `COMPLETE_TEST_CHECKLIST.md` | This file |
| `NOTIFICATION_TESTING_GUIDE.md` | Detailed testing guide |
| `NotificationTester.cs` | Unity testing script |

---

## ✅ Success Indicators

**System is working when:**

1. ✅ Browser test: All badges turn GREEN
2. ✅ PlayFab test: All return `"success": true`
3. ✅ Storage check: Notifications visible in Internal Data
4. ✅ Unity test: Console shows "Received X notifications"
5. ✅ Real actions: Ban/unban/audio trigger notifications

---

## 🚀 Final Steps

1. **Deploy** cloudscript.js to PlayFab
2. **Open** test-notifications.html in browser
3. **Login** with your PlayFab account
4. **Click** "Run All Tests" button
5. **Verify** all badges turn GREEN

**If all tests pass = System is 100% working!** 🎉

---

## 📞 Quick Help

**If tests fail:**
1. Check CloudScript is deployed and published
2. Check CloudScript logs for errors
3. Verify Title ID is correct (182E5E)
4. Make sure you're logged in to PlayFab
5. Check browser console for JavaScript errors

**Still not working?**
- Copy test results from browser console
- Copy CloudScript logs from PlayFab Dashboard
- Check all files are latest version
- Verify PlayFab account has permission

---

**Everything is ready for testing! Start with test-notifications.html for easiest testing.** 🚀
