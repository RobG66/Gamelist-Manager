using Avalonia.Platform.Storage;

namespace Gamelist_Manager.Services;

public interface IWindowOwnerProvider 
{
    object? GetMainWindowOwner();
    IStorageProvider? GetStorageProvider();
}
