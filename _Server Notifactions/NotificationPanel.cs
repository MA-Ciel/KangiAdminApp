using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Panel to display all notifications
/// Shows ban/unban, audio approved/deleted notifications
/// </summary>
namespace Kangi.ServerNotifications
{
public class NotificationPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Transform notificationListContainer;
    [SerializeField] private GameObject notificationItemPrefab;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button markAllReadButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI emptyText;

    [Header("Colors")]
    [SerializeField] private Color unreadColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private Color readColor = new Color(1f, 1f, 1f, 0.05f);

    private List<GameObject> notificationItems = new List<GameObject>();

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
        if (openButton != null)
        {
            openButton.onClick.AddListener(Show);
        }
        if (markAllReadButton != null)
        {
            markAllReadButton.onClick.AddListener(OnMarkAllReadClicked);
        }

        // Subscribe to notification events
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.OnNotificationsReceived += OnNotificationsReceived;
        }

        Hide();
    }

    /// <summary>
    /// Show notification panel
    /// </summary>
    public void Show()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
        }
            Debug.Log("<color=blue>[Server Request]</color> Fetching latest notifications...");
        // Fetch latest notifications
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.CheckForNewNotifications();
        }
    }

    /// <summary>
    /// Hide notification panel
    /// </summary>
    public void Hide()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Called when notifications are received
    /// </summary>
    private void OnNotificationsReceived(List<Notification> notifications)
    {
        ClearList();

        if (notifications == null || notifications.Count == 0)
        {
            ShowEmptyState();
            return;
        }

        HideEmptyState();

        foreach (var notif in notifications)
        {
            CreateNotificationItem(notif);
        }
    }

    /// <summary>
    /// Create a notification item in the list
    /// </summary>
    private void CreateNotificationItem(Notification notif)
    {
        if (notificationItemPrefab == null || notificationListContainer == null) return;

        GameObject item = Instantiate(notificationItemPrefab, notificationListContainer);
        notificationItems.Add(item);

        // Set background color based on read status
        Image bgImage = item.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = notif.read ? readColor : unreadColor;
        }

        // Find UI elements in the prefab
        TextMeshProUGUI titleText = item.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI messageText = item.transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timeText = item.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
        GameObject unreadDot = item.transform.Find("UnreadDot")?.gameObject;
        Button deleteButton = item.transform.Find("DeleteButton")?.GetComponent<Button>();

        // Set notification type icon/color
        Image typeIcon = item.transform.Find("TypeIcon")?.GetComponent<Image>();
        if (typeIcon != null)
        {
            switch (notif.type)
            {
                case "ban":
                    typeIcon.color = Color.red;
                    break;
                case "unban":
                    typeIcon.color = Color.green;
                    break;
                case "audio_approved":
                    typeIcon.color = Color.cyan;
                    break;
                case "audio_deleted":
                    typeIcon.color = Color.yellow;
                    break;
                default:
                    typeIcon.color = Color.white;
                    break;
            }
        }

        // Set text content
        if (titleText != null) titleText.text = notif.title;
        if (messageText != null) messageText.text = notif.message;
        if (timeText != null) timeText.text = FormatTime(notif.createdAt);
        if (unreadDot != null) unreadDot.SetActive(!notif.read);

        // Delete button
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() => OnDeleteClicked(notif.id));
        }

        // Click to mark as read
        Button itemButton = item.GetComponent<Button>();
        if (itemButton != null && !notif.read)
        {
            itemButton.onClick.AddListener(() => OnNotificationClicked(notif.id));
        }
    }

    /// <summary>
    /// Format timestamp for display
    /// </summary>
    private string FormatTime(string isoTime)
    {
        try
        {
            System.DateTime time = System.DateTime.Parse(isoTime);
            System.TimeSpan diff = System.DateTime.UtcNow - time;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            
            return time.ToString("MMM dd");
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Clear all notification items
    /// </summary>
    private void ClearList()
    {
        foreach (var item in notificationItems)
        {
            Destroy(item);
        }
        notificationItems.Clear();
    }

    /// <summary>
    /// Show empty state
    /// </summary>
    private void ShowEmptyState()
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(true);
            emptyText.text = "No notifications";
        }
    }

    /// <summary>
    /// Hide empty state
    /// </summary>
    private void HideEmptyState()
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Called when notification is clicked
    /// </summary>
    private void OnNotificationClicked(string notifId)
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.MarkAsRead(notifId);
        }
    }

    /// <summary>
    /// Called when delete button is clicked
    /// </summary>
    private void OnDeleteClicked(string notifId)
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.DeleteNotification(notifId);
        }
    }

    /// <summary>
    /// Called when mark all read button is clicked
    /// </summary>
    private void OnMarkAllReadClicked()
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.MarkAllAsRead();
        }
    }

    void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        if (markAllReadButton != null) markAllReadButton.onClick.RemoveAllListeners();

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.OnNotificationsReceived -= OnNotificationsReceived;
        }
    }
}
}