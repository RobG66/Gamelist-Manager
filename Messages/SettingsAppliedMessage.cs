using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Gamelist_Manager.Messages;

public class SettingsAppliedMessage : ValueChangedMessage<bool>
{
    public SettingsAppliedMessage() : base(true) { }
}