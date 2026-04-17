using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EzvizPlayer.Models
{
    public class PageConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("devices")]
        public List<DeviceConfig> Devices { get; set; } = new();

        [JsonPropertyName("rtspDevices")]
        public List<DeviceConfig> RtspDevices { get; set; } = new();
    }

    public class Config
    {
        [JsonPropertyName("windowTitle")]
        public string WindowTitle { get; set; } = "配送系统视频监控";

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("accessTokens")]
        public Dictionary<string, string> AccessTokens { get; set; } = new();

        [JsonPropertyName("window_width")]
        public int WindowWidth { get; set; } = 1280;

        [JsonPropertyName("window_height")]
        public int WindowHeight { get; set; } = 720;

        [JsonPropertyName("startFullscreen")]
        public bool StartFullscreen { get; set; } = false;

        [JsonPropertyName("screenRatio")]
        public double ScreenRatio { get; set; } = 0.85;

        [JsonPropertyName("devices")]
        public List<DeviceConfig> Devices { get; set; } = new();

        [JsonPropertyName("rtspDevices")]
        public List<DeviceConfig> RtspDevices { get; set; } = new();

        [JsonPropertyName("pages")]
        public List<PageConfig> Pages { get; set; } = new();
    }
}
