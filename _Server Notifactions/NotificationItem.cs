using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual notification item component
/// Attach to notification item prefab
/// </summary>
/// 
/// 
namespace Kangi.ServerNotifications
{
public class NotificationItem : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image typeIcon;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI timeText;
    public GameObject unreadDot;
    public Button deleteButton;

    private string notificationId;
    private bool isRead;

    /// <summary>
    /// Setup notification item
    /// </summary>
    public void Setup(Notification notification, System.Action<string> onDelete)
    {
        notificationId = notification.id;
        isRead = notification.read;

        // Set text
        if (titleText != null) titleText.text = notification.title;
        if (messageText != null) messageText.text = notification.message;
        if (timeText != null) timeText.text = FormatTime(notification.createdAt);

        // Show/hide unread indicator
        if (unreadDot != null) unreadDot.SetActive(!isRead);

        // Set background alpha based on read status
        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = isRead ? 0.05f : 0.1f;
            backgroundImage.color = color;
        }

        // Set type icon color
        if (typeIcon != null)
        {
            switch (notification.type)
            {
                case "ban":
                    typeIcon.color = new Color(1f, 0.2f, 0.2f); // Red
                    break;
                case "unban":
                    typeIcon.color = new Color(0.2f, 1f, 0.2f); // Green
                    break;
                case "audio_approved":
                    typeIcon.color = new Color(0.2f, 0.8f, 1f); // Cyan
                    break;
                case "audio_deleted":
                    typeIcon.color = new Color(1f, 0.8f, 0.2f); // Yellow
                    break;
                case "success":
                    typeIcon.color = Color.green;
                    break;
                case "warning":
                    typeIcon.color = Color.yellow;
                    break;
                case "error":
                    typeIcon.color = Color.red;
                    break;
                default:
                    typeIcon.color = Color.white;
                    break;
            }
        }

        // Setup delete button
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete?.Invoke(notificationId));
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
    /// Mark this notification as read visually
    /// </summary>
    public void MarkAsRead()
    {
        isRead = true;
        
        if (unreadDot != null) unreadDot.SetActive(false);
        
        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = 0.05f;
            backgroundImage.color = color;
        }
    }

    void OnDestroy()
    {
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
        }
    }
}
}