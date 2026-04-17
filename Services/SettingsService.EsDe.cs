using Gamelist_Manager.Classes.Helpers;
using System.Collections.Generic;

namespace Gamelist_Manager.Services
{
    public partial class SettingsService
    {
        internal Dictionary<string, string> BuildEsDeDefaults()
        {
            return new Dictionary<string, string>
            {
                [SettingKeys.ProfileType] = SettingKeys.ProfileTypeStandard,
                [SettingKeys.EsDeRoot] = string.Empty
            };
        }
    }
}
