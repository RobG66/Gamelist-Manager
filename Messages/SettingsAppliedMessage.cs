using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Gamelist_Manager.Messages;

public class SettingsAppliedMessage : ValueChangedMessage<bool>
{
    public bool ResetScraperSettings { get; }

    public SettingsAppliedMessage(bool resetScraperSettings = false) : base(true)
    {
        ResetScraperSettings = resetScraperSettings;
    }
}