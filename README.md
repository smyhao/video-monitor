# EzvizPlayer - 配送系统视频监控

基于 WPF 的多路视频监控播放器，支持萤石云 H5 播放和 RTSP 流直连两种模式。

## 功能特性

- **双引擎播放** — 萤石云设备通过 WebView2 播放，RTSP 设备通过 LibVLC 播放
- **自适应网格布局** — 根据设备数量自动计算 1x1 ~ 4x4 网格
- **多页监控切换** — 支持将不同设备按地区/场景分组，标题栏下拉切换页面
- **多账号支持** — 不同设备可绑定不同萤石云账号 token
- **全屏模式** — 支持 F11 / 双击标题栏 / 按钮切换
- **直播与回放** — 支持 `live` 实时流和 `rec` 录像回放
- **深色主题 UI** — 无边框窗口，自定义标题栏

## 技术栈

- .NET 8 WPF
- WebView2（萤石云 H5 播放器）
- LibVLCSharp + VideoLAN.LibVLC（RTSP 流播放）

## 快速开始

### 环境要求

- Windows 10 1803+
- .NET 8 SDK
- WebView2 Runtime（Windows 11 已内置）

### 构建

```bash
cd EzvizPlayer
dotnet build -c Release
```

输出目录为 `EzvizPlayer/bin/Release/net8.0-windows/`。

### 运行

1. 将 `config.json` 复制到构建输出目录
2. 运行 `EzvizPlayer.exe`

或直接使用 `build/` 目录下已编译好的产物。

## 配置说明

配置文件 `config.json` 放置在运行目录下。推荐使用 `pages` 多页结构：

```json
{
  "windowTitle": "配送系统视频监控",
  "accessToken": "at.xxx",
  "accessTokens": {
    "account1": "at.xxx",
    "account2": "at.xxx"
  },
  "startFullscreen": true,
  "screenRatio": 0.95,
  "pages": [
    {
      "name": "一楼仓库",
      "devices": [
        {
          "tokenKey": "account1",
          "name": "设备名称",
          "deviceSerial": "BGXXXXXXX",
          "channelNo": "1",
          "mode": "live",
          "themeId": "pcLive"
        }
      ],
      "rtspDevices": [
        {
          "name": "RTSP摄像头",
          "rtspUrl": "rtsp://host/stream"
        }
      ]
    }
  ]
}
```

### 向后兼容

如果 `pages` 不存在或为空，但保留了旧版的顶层 `devices` / `rtspDevices`，程序会自动将其包装为单页模式，原有行为不变。

| 字段 | 说明 |
|------|------|
| `accessToken` | 全局萤石云 accessToken |
| `accessTokens` | 多账号 token 映射，设备通过 `tokenKey` 引用 |
| `startFullscreen` | 是否启动即全屏 |
| `screenRatio` | 窗口占屏幕比例（0.3~1.0） |
| `pages` | 页面列表，每个页面包含 `name`、`devices`、`rtspDevices` |
| `devices`（旧）| 萤石云设备列表（向后兼容） |
| `rtspDevices`（旧）| RTSP 设备列表（向后兼容） |

## 项目结构

```
EzvizPlayer/
├── Models/
│   ├── Config.cs           # 全局配置模型（含 PageConfig）
│   └── DeviceConfig.cs     # 设备配置模型
├── Services/
│   ├── GridCalculator.cs   # 网格布局计算
│   └── UrlBuilder.cs       # 萤石云 URL 构建
├── App.xaml                # 应用入口 XAML
├── App.xaml.cs             # 应用启动，初始化 LibVLCSharp
├── MainWindow.xaml         # 主窗口 UI（无边框深色主题 + 页面切换器）
├── MainWindow.xaml.cs      # 核心逻辑
├── EzvizPlayer.csproj      # 项目配置与依赖
└── icon.ico                # 应用图标
```

## 文件详细说明

### MainWindow.xaml.cs

核心业务逻辑，负责：

| 功能 | 说明 |
|------|------|
| 配置加载 | 从 `config.json` 读取设备和窗口配置，支持新旧两种配置格式 |
| 多页切换 | 标题栏 ComboBox 切换页面，切换时释放上一页 VLC 资源 |
| 网格布局 | 调用 `GridCalculator` 根据当前页设备数量动态生成行列 |
| 视频面板创建 | 为每路设备创建带标题栏的视频面板 |
| 萤石云播放 | 通过 WebView2 嵌入 `open.ys7.com` H5 播放器 |
| RTSP 播放 | 通过 LibVLCSharp VideoView 直接播放 RTSP 流 |
| 全屏切换 | F11 / 双击标题栏 / 按钮，记录并恢复窗口位置 |
| 刷新视频 | 遍历当前页面所有面板，重新加载 WebView2 或重启 VLC 播放 |
| 加载/错误状态 | 显示加载动画和错误提示 |

### Models/Config.cs

全局配置模型：

| 字段 | 类型 | 说明 |
|------|------|------|
| `WindowTitle` | string | 窗口标题 |
| `AccessToken` | string | 全局萤石云 accessToken |
| `AccessTokens` | Dictionary | 多账号 token 映射 |
| `WindowWidth/Height` | int | 窗口初始尺寸 |
| `StartFullscreen` | bool | 启动即全屏 |
| `ScreenRatio` | double | 窗口占屏幕比例 |
| `Pages` | List | 页面列表（推荐） |
| `Devices` | List | 萤石云设备列表（向后兼容） |
| `RtspDevices` | List | RTSP 设备列表（向后兼容） |

### Models/PageConfig.cs（Config.cs 内嵌）

页面配置模型：

| 字段 | 说明 |
|------|------|
| `Name` | 页面名称，显示在标题栏下拉框中 |
| `Devices` | 该页面下的萤石云设备列表 |
| `RtspDevices` | 该页面下的 RTSP 设备列表 |

### Models/DeviceConfig.cs

设备配置模型，通过 `RtspUrl` 是否为空判断播放模式：

- **萤石云模式**：需要 `tokenKey`、`deviceSerial`、`channelNo` 等
- **RTSP 模式**：只需 `name` 和 `rtspUrl`

| 字段 | 说明 |
|------|------|
| `RtspUrl` | RTSP 地址，非空则走 VLC |
| `TokenKey` | 引用 `AccessTokens` 中的 key |
| `Name` | 面板标题显示的设备名称 |
| `DeviceSerial` | 萤石云设备序列号 |
| `ChannelNo` | 通道号，默认"1" |
| `Resolution` | 分辨率后缀，如 `.hd` |
| `StreamType` | 码流类型后缀 |
| `Mode` | `live`（直播）或 `rec`（回放） |
| `ThemeId` | H5 播放器主题 ID |
| `Begin/End` | 回放时间范围（`yyyyMMddhhmmss`） |

### Services/UrlBuilder.cs

构建萤石云 EZOPEN 协议播放 URL：

1. 根据 `tokenKey` 查找 token，回退到全局 `accessToken`
2. 拼接 `ezopen://[验证码@]open.ys7.com/{序列号}/{通道}{分辨率}{码流}.{模式}`
3. 回放模式追加 `?begin=...&end=...`
4. 最终生成 `https://open.ys7.com/console/jssdk/pc.html?accessToken=...&url=...&themeId=...`

### Services/GridCalculator.cs

根据设备总数计算网格布局：

| 设备数 | 行 x 列 |
|--------|---------|
| 1 | 1 x 1 |
| 2 | 1 x 2 |
| 3-4 | 2 x 2 |
| 5-6 | 2 x 3 |
| 7-9 | 3 x 3 |
| 10+ | 4 x 4 |

## License

[MIT](LICENSE)
