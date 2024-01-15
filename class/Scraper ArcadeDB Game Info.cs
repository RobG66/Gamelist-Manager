using System.Collections.Generic;

namespace GamelistManager
{
    public class ScraperArcadeDBGameInfo
    {
        public int index { get; set; }
        public string url { get; set; }
        public string game_name { get; set; }
        public string title { get; set; }
        public string cloneof { get; set; }
        public string manufacturer { get; set; }
        public string url_image_ingame { get; set; }
        public string url_image_title { get; set; }
        public string url_image_marquee { get; set; }
        public string url_image_cabinet { get; set; }
        public string url_image_flyer { get; set; }
        public string url_icon { get; set; }
        public string genre { get; set; }
        public int players { get; set; }
        public string year { get; set; }
        public string status { get; set; }
        public string history { get; set; }
        public string history_copyright_short { get; set; }
        public string history_copyright_long { get; set; }
        public string youtube_video_id { get; set; }
        public string url_video_shortplay { get; set; }
        public string url_video_shortplay_hd { get; set; }
        public int emulator_id { get; set; }
        public string emulator_name { get; set; }
        public string languages { get; set; }
        public int rate { get; set; }
        public string short_title { get; set; }
        public string nplayers { get; set; }
        public string input_controls { get; set; }
        public int input_buttons { get; set; }
        public string buttons_colors { get; set; }
        public string serie { get; set; }
        public string screen_orientation { get; set; }
        public string screen_resolution { get; set; }
    }

    public class GameListResponse
    {
        public int release { get; set; }
        public List<ScraperArcadeDBGameInfo> result { get; set; }
    }

}
