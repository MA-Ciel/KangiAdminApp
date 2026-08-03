using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// Notification Manager - Handles fetching and displaying notifications from PlayFab UserData
/// Auto-checks for new notifications on login and periodically
/// UPDATED: Uses GetUserData instead of CloudScript for faster access
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float checkInterval = 30f; // Check every 30 seconds
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("UI References")]
    [SerializeField] private GameObject notificationBadge; // Red dot indicator
    [SerializeField] private TMPro.TextMeshProUGUI badgeCountText;

    public event Action<int> OnUnreadCountChanged;
    public event Action<List<Notification>> OnNotificationsReceived;

    private List<Notification> notifications = new List<Notification>();
    private int unreadCount = 0;
    private Coroutine checkCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start checking for notifications after login
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            StartNotificationChecking();
        }
    }

    /// <summary>
    /// Start periodic notification checking
    /// </summary>
    public void StartNotificationChecking()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
        }
        checkCoroutine = StartCoroutine(PeriodicCheck());
        
        // Check immediately
        CheckForNewNotifications();
    }

    /// <summary>
    /// Stop periodic checking
    /// </summary>
    public void StopNotificationChecking()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }
    }

    /// <summary>
    /// Periodic check for new notifications
    /// </summary>
    private IEnumerator PeriodicCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            CheckForNewNotifications();
        }
    }

    /// <summary>
    /// Check for new notifications from PlayFab UserData - MUCH FASTER!
    /// </summary>
    public void CheckForNewNotifications()
    {
        Log("Checking for new notifications from UserData...");

        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            Keys = new List<string> { "Notifications" }
        }, OnGetUserDataSuccess, OnGetUserDataError);
    }

    private void OnGetUserDataSuccess(GetUserDataResult result)
    {
        try
        {
            if (result.Data != null && result.Data.ContainsKey("Notifications"))
            {
                string jsonString = result.Data["Notifications"].Value;
                
                if (!string.IsNullOrEmpty(jsonString))
                {
                    // Parse notifications array
                    notifications = ParseNotificationsFromJson(jsonString);
                    
                    // Count unread
                    unreadCount = 0;
                    foreach (var notif in notifications)
                    {
                        if (!notif.read) unreadCount++;
                    }

                    Log($"✓ Received {notifications.Count} notifications, {unreadCount} unread");

                    UpdateBadge();
                    OnUnreadCountChanged?.Invoke(unreadCount);
                    OnNotificationsReceived?.Invoke(notifications);

                    // Show popup for important unread notifications
                    ShowImportantNotifications();
                }
                else
                {
                    Log("No notifications found");
                    notifications.Clear();
                    unreadCount = 0;
                    UpdateBadge();
                }
            }
            else
            {
                Log("No notifications in UserData");
                notifications.Clear();
                unreadCount = 0;
                UpdateBadge();
            }
        }
        catch (System.Exception e)
        {
            Log($"Failed to parse notifications: {e.Message}", true);
        }
    }

    private void OnGetUserDataError(PlayFabError error)
    {
        Log($"Failed to get notifications: {error.ErrorMessage}", true);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public void MarkAsRead(string notificationId)
    {
        // Find and mark as read locally
        var notif = notifications.Find(n => n.id == notificationId);
        if (notif != null)
        {
            notif.read = true;
            unreadCount--;
            if (unreadCount < 0) unreadCount = 0;
            UpdateBadge();
            
            // Update in PlayFab
            SaveNotificationsToPlayFab();
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    public void MarkAllAsRead()
    {
        foreach (var notif in notifications)
        {
            notif.read = true;
        }
        unreadCount = 0;
        UpdateBadge();
        OnUnreadCountChanged?.Invoke(0);
        
        // Update in PlayFab
        SaveNotificationsToPlayFab();
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    public void DeleteNotification(string notificationId)
    {
        var notif = notifications.Find(n => n.id == notificationId);
        if (notif != null)
        {
            if (!notif.read) unreadCount--;
            notifications.Remove(notif);
            UpdateBadge();
            
            // Update in PlayFab
            SaveNotificationsToPlayFab();
        }
    }

    /// <summary>
    /// Save notifications back to PlayFab UserData
    /// </summary>
    private void SaveNotificationsToPlayFab()
    {
        string jsonString = SerializeNotifications(notifications);
        
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Notifications", jsonString }
            }
        }, (result) =>
        {
            Log("Notifications updated in PlayFab");
            CheckForNewNotifications(); // Refresh
        }, (error) =>
        {
            Log($"Failed to update notifications: {error.ErrorMessage}", true);
        });
    }

    /// <summary>
    /// Update badge UI
    /// </summary>
    private void UpdateBadge()
    {
        if (notificationBadge != null)
        {
            notificationBadge.SetActive(unreadCount > 0);
        }

        if (badgeCountText != null)
        {
            badgeCountText.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
        }
    }

    /// <summary>
    /// Show popup for important unread notifications (ban/unban/audio)
    /// </summary>
    private void ShowImportantNotifications()
    {
        foreach (var notif in notifications)
        {
            if (notif.read) continue;

            // Show popup for ban/unban/audio notifications
            if (notif.type == "ban" || notif.type == "unban" || 
                notif.type == "audio_approved" || notif.type == "audio_deleted")
            {
                ShowNotificationPopup(notif);
                break; // Show only first unread important notification
            }
        }
    }

    /// <summary>
    /// Show a notification popup
    /// </summary>
    private void ShowNotificationPopup(Notification notif)
    {
        // Use your existing notification system
        if (NotifcationPanel.Instance != null)
        {
            NotifcationPanel.Instance.ShowNotification($"{notif.title}: {notif.message}");
        }
        else
        {
            Debug.Log($"[Notification] {notif.title}: {notif.message}");
        }
    }

    /// <summary>
    /// Get all current notifications
    /// </summary>
    public List<Notification> GetNotifications()
    {
        return new List<Notification>(notifications);
    }

    /// <summary>
    /// Get unread count
    /// </summary>
    public int GetUnreadCount()
    {
        return unreadCount;
    }

    // JSON Parsing Methods
    private List<Notification> ParseNotificationsFromJson(string json)
    {
        List<Notification> result = new List<Notification>();
        
        if (string.IsNullOrEmpty(json) || json == "[]") return result;
        
        // Find notifications array
        int arrayStart = json.IndexOf("[");
        if (arrayStart == -1) return result;
        
        int arrayEnd = json.LastIndexOf("]");
        if (arrayEnd == -1) return result;

        string arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1).Trim();
        if (string.IsNullOrEmpty(arrayContent)) return result;

        // Split by notification objects
        int depth = 0;
        int objStart = -1;
        
        for (int i = 0; i < arrayContent.Length; i++)
        {
            if (arrayContent[i] == '{')
            {
                if (depth == 0) objStart = i;
                depth++;
            }
            else if (arrayContent[i] == '}')
            {
                depth--;
                if (depth == 0 && objStart != -1)
                {
                    string objJson = arrayContent.Substring(objStart, i - objStart + 1);
                    Notification notif = ParseSingleNotification(objJson);
                    if (notif != null) result.Add(notif);
                    objStart = -1;
                }
            }
        }

        return result;
    }

    private Notification ParseSingleNotification(string json)
    {
        try
        {
            Notification notif = new Notification();
            
            notif.id = ExtractStringValue(json, "id");
            notif.title = ExtractStringValue(json, "title");
            notif.message = ExtractStringValue(json, "message");
            notif.type = ExtractStringValue(json, "type");
            notif.createdAt = ExtractStringValue(json, "createdAt");
            notif.read = ExtractBoolValue(json, "read");
            
            // Parse data object if exists
            notif.data = new NotificationData();
            int dataStart = json.IndexOf("\"data\":");
            if (dataStart != -1)
            {
                int objStart = json.IndexOf("{", dataStart);
                int objEnd = json.IndexOf("}", objStart);
                if (objStart != -1 && objEnd != -1)
                {
                    string dataJson = json.Substring(objStart, objEnd - objStart + 1);
                    notif.data.reason = ExtractStringValue(dataJson, "reason");
                    notif.data.songId = ExtractStringValue(dataJson, "songId");
                    notif.data.songTitle = ExtractStringValue(dataJson, "songTitle");
                }
            }

            return notif;
        }
        catch
        {
            return null;
        }
    }

    private string SerializeNotifications(List<Notification> notifs)
    {
        if (notifs == null || notifs.Count == 0) return "[]";
        
        string result = "[";
        for (int i = 0; i < notifs.Count; i++)
        {
            if (i > 0) result += ",";
            result += SerializeSingleNotification(notifs[i]);
        }
        result += "]";
        return result;
    }

    private string SerializeSingleNotification(Notification notif)
    {
        string dataJson = "{";
        dataJson += $"\"reason\":\"{EscapeJson(notif.data.reason)}\",";
        dataJson += $"\"songId\":\"{EscapeJson(notif.data.songId)}\",";
        dataJson += $"\"songTitle\":\"{EscapeJson(notif.data.songTitle)}\"";
        dataJson += "}";
        
        string json = "{";
        json += $"\"id\":\"{EscapeJson(notif.id)}\",";
        json += $"\"title\":\"{EscapeJson(notif.title)}\",";
        json += $"\"message\":\"{EscapeJson(notif.message)}\",";
        json += $"\"type\":\"{EscapeJson(notif.type)}\",";
        json += $"\"data\":{dataJson},";
        json += $"\"read\":{(notif.read ? "true" : "false")},";
        json += $"\"createdAt\":\"{EscapeJson(notif.createdAt)}\"";
        json += "}";
        return json;
    }

    private string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private string ExtractStringValue(string json, string key)
    {
        string searchKey = "\"" + key + "\"";
        int keyIndex = json.IndexOf(searchKey);
        if (keyIndex == -1) return "";

        int colonIndex = json.IndexOf(":", keyIndex);
        if (colonIndex == -1) return "";

        int valueStart = json.IndexOf("\"", colonIndex);
        if (valueStart == -1) return "";
        valueStart++;

        int valueEnd = json.IndexOf("\"", valueStart);
        if (valueEnd == -1) return "";

        return json.Substring(valueStart, valueEnd - valueStart);
    }

    private bool ExtractBoolValue(string json, string key)
    {
        string searchKey = "\"" + key + "\"";
        int keyIndex = json.IndexOf(searchKey);
        if (keyIndex == -1) return false;

        int colonIndex = json.IndexOf(":", keyIndex);
        if (colonIndex == -1) return false;

        int trueIndex = json.IndexOf("true", colonIndex, 10);
        return trueIndex != -1 && trueIndex < colonIndex + 10;
    }

    private void Log(string message, bool isError = false)
    {
        if (!enableDebugLogs) return;

        if (isError)
            Debug.LogError($"<color=red>[NotificationManager]</color> {message}");
        else
            Debug.Log($"<color=yellow>[NotificationManager]</color> {message}");
    }

    void OnDestroy()
    {
        StopNotificationChecking();
    }
}

[Serializable]
public class Notification
{
    public string id;
    public string title;
    public string message;
    public string type; // info, success, warning, error, ban, unban, audio_approved, audio_deleted
    public NotificationData data;
    public bool read;
    public string createdAt;
}

[Serializable]
public class NotificationData
{
    public string reason;
    public string songId;
    public string songTitle;
}
