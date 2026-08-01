using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// Notification Manager - Handles fetching and displaying notifications from PlayFab
/// Auto-checks for new notifications on login and periodically
/// </summary>
namespace Kangi.ServerNotifications
{
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
            CheckForNewNotifications();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    /// <summary>
    /// Check for new notifications from PlayFab
    /// </summary>
    public void CheckForNewNotifications()
    {
        Log("Checking for new notifications...");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "notificationWorkflow",
            FunctionParameter = new { action = "getNotifications" }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnGetNotificationsSuccess, OnGetNotificationsFailure);
    }

    /// <summary>
    /// Get only unread count (lightweight check)
    /// </summary>
    public void GetUnreadCount()
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "notificationWorkflow",
            FunctionParameter = new { action = "getUnreadCount" }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnUnreadCountSuccess, OnError);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public void MarkAsRead(string notificationId)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "notificationWorkflow",
            FunctionParameter = new
            {
                action = "markAsRead",
                notificationId = notificationId
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, (result) =>
        {
            Log($"Notification {notificationId} marked as read");
            CheckForNewNotifications(); // Refresh
        }, OnError);
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    public void MarkAllAsRead()
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "notificationWorkflow",
            FunctionParameter = new { action = "markAllAsRead" }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, (result) =>
        {
            Log("All notifications marked as read");
            unreadCount = 0;
            UpdateBadge();
            OnUnreadCountChanged?.Invoke(0);
        }, OnError);
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    public void DeleteNotification(string notificationId)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "notificationWorkflow",
            FunctionParameter = new
            {
                action = "deleteNotification",
                notificationId = notificationId
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, (result) =>
        {
            Log($"Notification {notificationId} deleted");
            CheckForNewNotifications(); // Refresh
        }, OnError);
    }

    private void OnGetNotificationsSuccess(ExecuteCloudScriptResult result)
    {
        if (result.FunctionResult == null) return;

        try
        {
            // Parse JSON manually for compatibility
            string jsonString = result.FunctionResult.ToString();
            
            // Simple JSON parsing for success field
            if (jsonString.Contains("\"success\":true") || jsonString.Contains("\"success\": true"))
            {
                // Extract notifications array and unread count manually
                notifications = ParseNotificationsFromJson(jsonString);
                unreadCount = ParseUnreadCountFromJson(jsonString);

                Log($"Received {notifications.Count} notifications, {unreadCount} unread");

                UpdateBadge();
                OnUnreadCountChanged?.Invoke(unreadCount);
                OnNotificationsReceived?.Invoke(notifications);

                // Show popup for important unread notifications
                ShowImportantNotifications();
            }
        }
        catch (System.Exception e)
        {
            Log($"Failed to parse notifications: {e.Message}", true);
        }
    }

    private List<Notification> ParseNotificationsFromJson(string json)
    {
        List<Notification> result = new List<Notification>();
        
        // Find notifications array
        int notifStart = json.IndexOf("\"notifications\":");
        if (notifStart == -1) return result;
        
        int arrayStart = json.IndexOf("[", notifStart);
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

    private int ParseUnreadCountFromJson(string json)
    {
        string searchKey = "\"unreadCount\"";
        int keyIndex = json.IndexOf(searchKey);
        if (keyIndex == -1) return 0;

        int colonIndex = json.IndexOf(":", keyIndex);
        if (colonIndex == -1) return 0;

        int commaIndex = json.IndexOf(",", colonIndex);
        int braceIndex = json.IndexOf("}", colonIndex);
        
        int endIndex = commaIndex != -1 && commaIndex < braceIndex ? commaIndex : braceIndex;
        if (endIndex == -1) return 0;

        string valueStr = json.Substring(colonIndex + 1, endIndex - colonIndex - 1).Trim();
        
        int value;
        if (int.TryParse(valueStr, out value))
        {
            return value;
        }
        
        return 0;
    }

    private void OnGetNotificationsFailure(PlayFabError error)
    {
        Log($"Failed to get notifications: {error.ErrorMessage}", true);
    }

    private void OnUnreadCountSuccess(ExecuteCloudScriptResult result)
    {
        if (result.FunctionResult == null) return;

        try
        {
            string jsonString = result.FunctionResult.ToString();
            unreadCount = ParseUnreadCountFromJson(jsonString);
            UpdateBadge();
            OnUnreadCountChanged?.Invoke(unreadCount);
        }
        catch (System.Exception e)
        {
            Log($"Failed to parse unread count: {e.Message}", true);
        }
    }

    private void OnError(PlayFabError error)
    {
        Log($"PlayFab Error: {error.ErrorMessage}", true);
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

        // Auto-mark as read after showing
        MarkAsRead(notif.id);
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
    // public int GetUnreadCount()
    // {
    //     return unreadCount;
    // }

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

    // Response classes
    [Serializable]
    public class NotificationResponse
    {
        public bool success;
        public List<Notification> notifications;
        public int unreadCount;
        public int total;
    }

    [Serializable]
    public class UnreadCountResponse
    {
        public bool success;
        public int unreadCount;
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
}