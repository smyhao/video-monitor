# 配送系统视频监控

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

基于 WPF 的多路视频监控播放器，支持萤石云 H5 播放、多页监控切换和深色主题 UI。

## 版本选择

本仓库提供两个版本，满足不同部署需求：

| 版本 | 核心能力 | 输出体积 | 适用场景 |
|------|---------|---------|----------|
| **[EzvizPlayer](src/EzvizPlayer)** | 萤石云 + RTSP 双引擎 | ~282 MB | 需要同时接入萤石云和 RTSP 设备 |
| **[EzvizPlayerLite](src/EzvizPlayerLite)** | 仅萤石云 | ~2 MB | 只接入萤石云，追求轻量部署 |

## 功能特性

- **多页监控切换** — 标题栏下拉切换不同地区/场景，自动释放上一页资源
- **萤石云播放** — WebView2 嵌入 H5 播放器，支持直播与回放
- **RTSP 播放** — 完整版通过 LibVLC 直接播放 RTSP 流（Lite 版已移除）
- **自适应网格** — 根据设备数量自动计算 1×1 ~ 4×4 布局
- **多账号支持** — 不同设备可绑定不同萤石云 `accessToken`
- **全屏模式** — 支持 F11 / 双击标题栏 / 按钮切换
- **深色主题** — 无边框窗口，自定义标题栏

## 快速开始

### 环境要求

- Windows 10 1803+
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- WebView2 Runtime（Windows 11 已内置；部分 Win10 需手动安装）

### 构建

```bash
# 完整版
cd src/EzvizPlayer
dotnet build -c Release

# Lite 版
cd src/EzvizPlayerLite
dotnet build -c Release
```

### 运行

1. 复制根目录的 [`config.example.json`](config.example.json) 到程序运行目录，重命名为 `config.json`
2. 填入萤石云 `accessToken` 和设备信息
3. 运行 `EzvizPlayer.exe`

## 配置示例

推荐使用 `pages` 多页结构将设备按地区分组：

```json
{
  "windowTitle": "配送系统视频监控",
  "accessToken": "at.xxx",
  "accessTokens": {
    "account1": "at.xxx"
  },
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
      ]
    }
  ]
}
```

- 完整版：`rtspDevices` 中的设备会通过 LibVLC 播放 RTSP 流
- Lite 版：兼容读取 `rtspDevices`，但遇到 RTSP 设备时显示"不支持"提示

更多说明请参考各版本目录下的 `README.md`：
- [完整版说明](src/EzvizPlayer/README.md)
- [Lite 版说明](src/EzvizPlayerLite/README.md)

## 项目结构

```
src/
├── EzvizPlayer/           # 完整版源码
└── EzvizPlayerLite/       # Lite 版源码
```

## License

[MIT](LICENSE)
