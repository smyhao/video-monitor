# EzvizPlayerLite - 配送系统视频监控（Lite 版）

基于 WPF 的多路视频监控播放器，**仅支持萤石云 H5 播放**，去掉了 RTSP/LibVLC 依赖，体积更小，部署更轻量。

## 与完整版的区别

| 特性 | EzvizPlayer（完整版） | EzvizPlayerLite（精简版） |
|------|----------------------|--------------------------|
| 萤石云播放 | ✅ WebView2 | ✅ WebView2 |
| RTSP 播放 | ✅ LibVLC | ❌ 已移除 |
| 多页切换 | ✅ | ✅ |
| 全屏/刷新 | ✅ | ✅ |
| 输出体积 | ~280 MB（含 VLC） | ~2 MB |

## 功能特性

- **萤石云播放** — 通过 WebView2 嵌入 H5 播放器
- **多页监控切换** — 支持将不同设备按地区/场景分组，标题栏下拉切换页面
- **多账号支持** — 不同设备可绑定不同萤石云账号 token
- **全屏模式** — 支持 F11 / 双击标题栏 / 按钮切换
- **直播与回放** — 支持 `live` 实时流和 `rec` 录像回放
- **深色主题 UI** — 无边框窗口，自定义标题栏

## 技术栈

- .NET 8 WPF
- WebView2（萤石云 H5 播放器）

## 快速开始

### 环境要求

- Windows 10 1803+
- .NET 8 SDK（仅构建时需要）
- WebView2 Runtime（Windows 11 已内置；部分 Win10 需手动安装）

### 构建

```bash
cd EzvizPlayerLite
dotnet build -c Release
```

### 运行

1. 将 `config.json` 复制到构建输出目录
2. 运行 `EzvizPlayer.exe`

## 配置说明

配置文件 `config.json` 放置在运行目录下，结构与完整版一致：

```json
{
  "windowTitle": "配送系统视频监控",
  "accessToken": "at.xxx",
  "accessTokens": {
    "account1": "at.xxx"
  },
  "startFullscreen": true,
  "screenRatio": 0.95,
  "pages": [
    {
      "name": "一楼仓库",
      "devices": [
        {
          "tokenKey": "account1",
          "name": "起飞柜1",
          "deviceSerial": "BGXXXXXXX",
          "channelNo": "1",
          "mode": "live",
          "themeId": "pcLive"
        }
      ],
      "rtspDevices": []
    }
  ]
}
```

> **注意**：Lite 版本会读取 `rtspDevices` 字段以保持配置兼容，但遇到 RTSP 设备时会显示"不支持"提示，不会进行播放。

## License

[MIT](../LICENSE)
