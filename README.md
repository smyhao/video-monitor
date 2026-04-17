# 配送系统视频监控

本仓库包含两个版本的 WPF 视频监控播放器，均支持萤石云 H5 播放和多页监控切换。

| 版本 | 特点 | 体积 | 适用场景 |
|------|------|------|----------|
| **[EzvizPlayer](src/EzvizPlayer)** | 萤石云 + RTSP 双引擎 | ~282 MB | 需要同时播放萤石云和 RTSP 流 |
| **[EzvizPlayerLite](src/EzvizPlayerLite)** | 仅萤石云，无 LibVLC 依赖 | ~2 MB | 只需要萤石云，追求轻量部署 |

## 目录结构

```
csharp/
├── src/
│   ├── EzvizPlayer/           # 完整版源码
│   └── EzvizPlayerLite/       # Lite 版源码
├── dist/                      # 编译产物（本地生成，被 .gitignore 忽略）
│   ├── EzvizPlayer/
│   └── EzvizPlayerLite/
├── config.example.json        # 配置文件模板
├── LICENSE
└── README.md                  # 本文件
```

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

编译输出默认在 `src/<项目>/bin/Release/net8.0-windows/` 下。

### 运行

1. 复制 `config.example.json` 到运行目录，重命名为 `config.json`
2. 填入你的萤石云 `accessToken` 和设备信息
3. 运行对应的 `EzvizPlayer.exe`

> 本地开发时，`src/EzvizPlayer` 和 `src/EzvizPlayerLite` 的 `csproj` 会自动将根目录的 `config.json`（如果存在）复制到输出目录。

## 配置说明

两个版本共用同一套配置格式。推荐使用 `pages` 多页结构将设备按地区分组：

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
      ],
      "rtspDevices": []
    }
  ]
}
```

- 完整版：`rtspDevices` 中的设备会通过 LibVLC 播放 RTSP 流
- Lite 版：会读取 `rtspDevices`，但遇到 RTSP 设备时只会显示"不支持"提示

更多详细说明请参考各版本源码目录下的 `README.md`：
- [完整版说明](src/EzvizPlayer/README.md)
- [Lite 版说明](src/EzvizPlayerLite/README.md)

## License

[MIT](LICENSE)
