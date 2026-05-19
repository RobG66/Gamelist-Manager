using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Gamelist_Manager.Messages;

public class ApplyProfileSwitchMessage : AsyncRequestMessage<bool>
{
    public string ProfileName { get; }

    public ApplyProfileSwitchMessage(string profileName) => ProfileName = profileName;
}
