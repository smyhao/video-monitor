using System;
using EzvizPlayer.Models;

namespace EzvizPlayer.Services
{
    public static class UrlBuilder
    {
        public static string BuildUrl(Config config, DeviceConfig dev)
        {
            string accessToken = "";
            if (!string.IsNullOrWhiteSpace(dev.TokenKey)
                && config.AccessTokens != null
                && config.AccessTokens.TryGetValue(dev.TokenKey, out var tokenFromMap)
                && !string.IsNullOrWhiteSpace(tokenFromMap))
            {
                accessToken = tokenFromMap;
            }
            else if (!string.IsNullOrWhiteSpace(config.AccessToken))
            {
                accessToken = config.AccessToken;
            }

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException($"设备 [{dev.Name}] 未找到有效的 accessToken，请检查 tokenKey 或根级 accessToken 配置");
            if (string.IsNullOrWhiteSpace(dev.DeviceSerial))
                throw new ArgumentException("配置文件中缺少 deviceSerial");

            string verifyPart = string.IsNullOrWhiteSpace(dev.DeviceVerifyCode)
                ? ""
                : $"{dev.DeviceVerifyCode}@";

            string ezopenPath = $"{dev.ChannelNo}{dev.Resolution}{dev.StreamType}.{dev.Mode}";
            string ezopenUrl = $"ezopen://{verifyPart}open.ys7.com/{dev.DeviceSerial}/{ezopenPath}";

            if (dev.Mode == "rec")
            {
                if (string.IsNullOrWhiteSpace(dev.Begin) || string.IsNullOrWhiteSpace(dev.End))
                    throw new ArgumentException($"回放模式需要在配置中设置 begin 和 end（格式：yyyyMMddhhmmss）: {dev.DeviceSerial}");
                ezopenUrl += $"?begin={dev.Begin}&end={dev.End}";
            }

            string baseUrl = "https://open.ys7.com/console/jssdk/pc.html";
            string encodedAccessToken = Uri.EscapeDataString(accessToken);
            string encodedEzopenUrl = Uri.EscapeDataString(ezopenUrl);
            string encodedThemeId = Uri.EscapeDataString(dev.ThemeId ?? "");
            return $"{baseUrl}?accessToken={encodedAccessToken}&url={encodedEzopenUrl}&themeId={encodedThemeId}";
        }
    }
}
