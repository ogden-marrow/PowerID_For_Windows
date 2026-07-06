using Windows.ApplicationModel;

namespace PowerID.Services;

/// <summary>
/// Wraps the packaged-app StartupTask API (declared in Package.appxmanifest as
/// "PowerIDStartupTask") - the Windows equivalent of SMLoginItemSetEnabled on macOS. The task's
/// actual registration state is the source of truth, not a locally-persisted preference: the user
/// can also toggle this from Task Manager's Startup tab, which this reads back correctly.
/// </summary>
public static class StartupTaskService
{
    private const string TaskId = "PowerIDStartupTask";

    public static async Task<bool> IsEnabledAsync()
    {
        var task = await StartupTask.GetAsync(TaskId);
        return task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
    }

    /// <summary>
    /// Attempts to enable/disable the startup task per <paramref name="enabled"/>. Returns the
    /// state actually in effect afterward, which can differ from what was requested if the user
    /// declined the OS consent prompt or an admin policy disabled it.
    /// </summary>
    public static async Task<bool> SetEnabledAsync(bool enabled)
    {
        var task = await StartupTask.GetAsync(TaskId);

        if (!enabled)
        {
            task.Disable();
            return false;
        }

        var state = task.State switch
        {
            StartupTaskState.Disabled => await task.RequestEnableAsync(),
            _ => task.State,
        };

        return state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
    }
}
