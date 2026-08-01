using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// Quick tester script for notification system
/// Attach to a GameObject with UI buttons for testing
/// </summary>
public class NotificationTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    /// <summary>
    /// Test 1: Send test notification via CloudScript
    /// </summary>
    public void TestSendNotification()
    {
        Log("Testing: Send Notification");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "sendTest"
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, 
            result => OnTestSuccess(result, "Send Test"), 
            OnTestError);
    }

    /// <summary>
    /// Test 2: Get all notifications
    /// </summary>
    public void TestGetNotifications()
    {
        Log("Testing: Get All Notifications");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "getAll"
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => OnTestSuccess(result, "Get All"),
            OnTestError);
    }

    /// <summary>
    /// Test 3: Check storage
    /// </summary>
    public void TestCheckStorage()
    {
        Log("Testing: Check Storage");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "checkStorage"
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => OnTestSuccess(result, "Check Storage"),
            OnTestError);
    }

    /// <summary>
    /// Test 4: Test ban notification
    /// </summary>
    public void TestBanNotification()
    {
        Log("Testing: Ban Notification");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "testBan"
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => OnTestSuccess(result, "Ban Test"),
            OnTestError);
    }

    /// <summary>
    /// Test 5: Test unban notification
    /// </summary>
    public void TestUnbanNotification()
    {
        Log("Testing: Unban Notification");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "testUnban"
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => OnTestSuccess(result, "Unban Test"),
            OnTestError);
    }

    /// <summary>
    /// Test 6: Test audio approve notification
    /// </summary>
    public void TestAudioApproveNotification()
    {
        Log("Testing: Audio Approve Notification");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "testAudioApprove"
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => OnTestSuccess(result, "Audio Approve Test"),
            OnTestError);
    }

    /// <summary>
    /// Test 7: Test audio delete notification
    /// </summary>
    public void TestAudioDeleteNotification()
    {
        Log("Testing: Audio Delete Notification");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "testAudioDelete"
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => OnTestSuccess(result, "Audio Delete Test"),
            OnTestError);
    }

    /// <summary>
    /// Test 8: Send multiple notifications
    /// </summary>
    public void TestSendMultiple()
    {
        Log("Testing: Send Multiple Notifications");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "sendMultiple",
                count = 5
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => OnTestSuccess(result, "Multiple Test"),
            OnTestError);
    }

    /// <summary>
    /// Test 9: Clear all notifications
    /// </summary>
    public void TestClearAll()
    {
        Log("Testing: Clear All Notifications");

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "testNotificationSystem",
            FunctionParameter = new
            {
                testType = "clearAll"
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => OnTestSuccess(result, "Clear All"),
            OnTestError);
    }

    /// <summary>
    /// Test 10: Fetch using NotificationManager
    /// </summary>
    public void TestNotificationManager()
    {
        Log("Testing: NotificationManager Fetch");

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.CheckForNewNotifications();
            Log("✓ NotificationManager fetch triggered. Check console for results.");
        }
        else
        {
            LogError("✗ NotificationManager.Instance is NULL! Add NotificationManager to scene.");
        }
    }

    /// <summary>
    /// Run ALL tests sequentially
    /// </summary>
    public void RunAllTests()
    {
        Log("═══════════════════════════════════════");
        Log("RUNNING ALL NOTIFICATION TESTS");
        Log("═══════════════════════════════════════");

        StartCoroutine(RunTestsSequentially());
    }

    private System.Collections.IEnumerator RunTestsSequentially()
    {
        // Clear first
        TestClearAll();
        yield return new WaitForSeconds(1f);

        // Send test notification
        TestSendNotification();
        yield return new WaitForSeconds(1f);

        // Send ban notification
        TestBanNotification();
        yield return new WaitForSeconds(1f);

        // Send unban notification
        TestUnbanNotification();
        yield return new WaitForSeconds(1f);

        // Send audio approve
        TestAudioApproveNotification();
        yield return new WaitForSeconds(1f);

        // Send audio delete
        TestAudioDeleteNotification();
        yield return new WaitForSeconds(1f);

        // Get all notifications
        TestGetNotifications();
        yield return new WaitForSeconds(1f);

        // Check storage
        TestCheckStorage();
        yield return new WaitForSeconds(1f);

        // Test NotificationManager
        TestNotificationManager();

        Log("═══════════════════════════════════════");
        Log("ALL TESTS COMPLETED!");
        Log("═══════════════════════════════════════");
    }

    private void OnTestSuccess(ExecuteCloudScriptResult result, string testName)
    {
        if (result.FunctionResult != null)
        {
            string jsonResult = result.FunctionResult.ToString();
            Log($"✓ [{testName}] SUCCESS:\n{jsonResult}");
            
            // Show notification popup if available
            if (NotifcationPanel.Instance != null)
            {
                NotifcationPanel.Instance.ShowNotification($"Test '{testName}' passed!");
            }
        }
        else
        {
            LogError($"✗ [{testName}] Result is null");
        }

        // Log CloudScript logs if available
        if (result.Logs != null && result.Logs.Count > 0)
        {
            Log($"CloudScript Logs for [{testName}]:");
            foreach (var logEntry in result.Logs)
            {
                Log($"  {logEntry.Level}: {logEntry.Message}");
            }
        }
    }

    private void OnTestError(PlayFabError error)
    {
        LogError($"✗ TEST FAILED: {error.ErrorMessage}");
        LogError($"Error Details: {error.ErrorDetails}");

        if (NotifcationPanel.Instance != null)
        {
            NotifcationPanel.Instance.ShowNotification($"Test failed: {error.ErrorMessage}");
        }
    }

    private void Log(string message)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"<color=cyan>[NotificationTester]</color> {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"<color=red>[NotificationTester]</color> {message}");
    }

    // Keyboard shortcuts for testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Log("Hotkey: T - Send Test Notification");
            TestSendNotification();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            Log("Hotkey: G - Get All Notifications");
            TestGetNotifications();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Log("Hotkey: C - Clear All");
            TestClearAll();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            Log("Hotkey: M - Test NotificationManager");
            TestNotificationManager();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Log("Hotkey: 0 - Run All Tests");
            RunAllTests();
        }
    }

    void OnGUI()
    {
        // Debug UI overlay
        if (!enableDebugLogs) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.Label("=== Notification Tester ===");
        GUILayout.Label("Hotkeys:");
        GUILayout.Label("  T - Send Test");
        GUILayout.Label("  G - Get All");
        GUILayout.Label("  C - Clear All");
        GUILayout.Label("  M - Test Manager");
        GUILayout.Label("  0 - Run All Tests");
        GUILayout.EndArea();
    }
}
