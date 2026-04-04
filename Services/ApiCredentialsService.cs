namespace Gamelist_Manager.Services
{
    public static class ApiCredentialsService
    {
        public static string GetEmuMoviesBearerToken()    => Secrets.EmuMoviesBearerToken;
        public static string GetScreenScraperDevId()      => Secrets.ScreenScraperDevId;
        public static string GetScreenScraperDevPassword() => Secrets.ScreenScraperDevPassword;
    }
}
