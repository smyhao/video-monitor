using System.Text.Json.Serialization;

namespace EzvizPlayer.Models
{
    public class DeviceConfig
    {
        // 播放模式判定：rtspUrl 非空则走 RTSP/LibVLC，否则走萤石云/WebView2
        [JsonPropertyName("rtspUrl")]
        public string RtspUrl { get; set; } = "";

        // 兼容旧配置（type + url）
        [JsonPropertyName("type")]
        public string Type { get; set; } = "ezviz";

        [JsonPropertyName("tokenKey")]
        public string TokenKey { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("deviceSerial")]
        public string DeviceSerial { get; set; } = "";

        [JsonPropertyName("channelNo")]
        public string ChannelNo { get; set; } = "1";

        [JsonPropertyName("deviceVerifyCode")]
        public string DeviceVerifyCode { get; set; } = "";

        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = "";

        [JsonPropertyName("streamType")]
        public string StreamType { get; set; } = "";

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "live";

        [JsonPropertyName("themeId")]
        public string ThemeId { get; set; } = "pcLive";

        [JsonPropertyName("begin")]
        public string Begin { get; set; } = "";

        [JsonPropertyName("end")]
        public string End { get; set; } = "";
    }
}
