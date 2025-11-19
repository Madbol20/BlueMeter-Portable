using BlueMeter.WPF.Config;

namespace BlueMeter.WPF.Services;

/// <summary>
/// Design-time implementation of IGlobalHotkeyService for Visual Studio designer preview.
/// All methods are no-ops since hotkey registration is not needed at design time.
/// </summary>
internal sealed class DesignTimeGlobalHotkeyService : IGlobalHotkeyService
{
    public void Start()
    {
        // Design-time no-op
    }

    public void Stop()
    {
        // Design-time no-op
    }

    public void UpdateFromConfig(AppConfig config)
    {
        // Design-time no-op
    }
}
