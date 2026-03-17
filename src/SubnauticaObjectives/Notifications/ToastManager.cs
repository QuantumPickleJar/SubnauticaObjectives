using BepInEx.Logging;

namespace SubnauticaObjectives.Notifications;

// Wraps ErrorMessage to display objective-change toast notifications.
// ErrorMessage is a vanilla Subnautica singleton — no Nautilus dependency required.
public static class ToastManager
{
    private static ManualLogSource? _log;

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
    }

    // Shows a short toast message on screen (uses the vanilla ErrorMessage system).
    public static void Show(string message)
    {
        _log?.LogInfo($"[Toast] {message}");
        ErrorMessage.AddMessage(message);
    }

    // Formats and shows an objective-change notification.
    public static void ShowObjectiveChanged(string objectiveTitle, int hintDepth, string hintText)
    {
        string display = hintDepth <= 1
            ? $"Objective: {objectiveTitle}"
            : $"Objective: {hintText}";

        Show(display);
    }

    // Shows a "chapter entered" notification for major milestones.
    public static void ShowChapterEntered(string chapterTitle)
    {
        Show($"— {chapterTitle} —");
    }
}
