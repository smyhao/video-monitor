# 多页监控切换功能 — Prompt

## 需求描述

在现有 EzvizPlayer 萤石云视频监控播放器基础上，增加多页（多地区）监控画面切换功能。

- 左上角标题栏添加页面选择器，点击下拉可切换不同地区的监控画面
- 支持键盘快捷键：←/→ 预选页面，Enter 确认切换，Esc 取消
- 切换页面时释放上一页的 VLC 播放资源
- 不需要记住上次查看的页面

## 配置结构

**重要：基于当前已有的 `build/config.json` 进行修改，不要从头重写示例。**

当前配置文件包含 4 台萤石云设备（起飞柜1/2、降落柜1/2）和 1 路 RTSP 示例设备，需要在此基础上将原有的 `devices` 和 `rtspDevices` 重组为 `pages` 结构。

新增 `PageConfig` 模型，将现有设备按地区分组到 `pages` 数组中：

- 保持原有的 `accessToken`、`accessTokens`、`screenRatio` 等顶层字段不变
- 移除顶层的 `devices` 和 `rtspDevices`
- 新增 `pages` 数组，每个元素包含 `name`、`devices`、`rtspDevices`
- 将原有设备分配到对应的页面分组中

### 向后兼容

- 若 `pages` 存在且非空 → 多页模式
- 若 `pages` 不存在或为空，但旧 `devices` / `rtspDevices` 存在 → 自动包装为单页，行为不变

## UI 设计

标题栏左侧添加 ComboBox 页面选择器：

```
[▼ 一楼仓库]              配送系统视频监控 · 3 路视频        [刷新] [—] [□] [×]
┌─────────┬─────────┐
│ 起飞柜1  │ 降落柜1  │
├─────────┼─────────┼
│ 门口摄像头│         │
└─────────┴─────────┘
```

- ComboBox 样式：无边框，深色透明背景，白色文字，与现有 UI 风格一致
- 只有 1 页时隐藏 ComboBox
- 副标题显示当前页信息：`X 路视频 · 页面名称`

## 键盘交互

| 按键 | 行为 |
|------|------|
| ← | 进入导航模式，预选上一页（循环） |
| → | 进入导航模式，预选下一页（循环） |
| Enter | 确认切换到预选页 |
| Esc | 取消导航，回到当前页 |
| F11 | 全屏切换（保持不变） |

导航模式的视觉反馈：ComboBox 弹出下拉并高亮预选项。

## 代码改动

### 改动文件

| 文件 | 改动内容 |
|------|----------|
| `Models/Config.cs` | 新增 `PageConfig` 类（name, devices, rtspDevices），`Config` 新增 `Pages` 属性 |
| `MainWindow.xaml` | 标题栏左侧图标旁加 `ComboBox`，添加扁平深色样式资源 |
| `MainWindow.xaml.cs` | 多页加载逻辑、`SwitchToPage()` 方法、VLC 资源释放、键盘导航处理 |

### 不改动

`DeviceConfig.cs`、`GridCalculator.cs`、`UrlBuilder.cs`、`Services/` 下其他文件

### 关键实现细节

**SwitchToPage(int index) 方法：**
1. 遍历 `_mediaPlayers`，Stop + Dispose 每个播放器，清空列表
2. 清空 `VideoGrid` 子元素和行列定义
3. 从 `_pages[index]` 获取设备列表
4. 调用 `GridCalculator.Calculate()` 计算网格
5. 遍历设备调用已有的 `CreateVideoPanel()` 创建面板
6. 更新副标题，同步 ComboBox 选中项

**MainWindow_Loaded 改动：**
- 加载配置后判断 `pages` 是否存在
- 有 pages → 多页模式，填充 ComboBox
- 无 pages → 将旧 `devices` + `rtspDevices` 包装为单页
- 调用 `SwitchToPage(0)` 渲染

## 现有项目结构参考

```
EzvizPlayer/
├── Models/
│   ├── Config.cs           ← 新增 PageConfig 类，Config 加 Pages
│   └── DeviceConfig.cs     ← 不改
├── Services/
│   ├── GridCalculator.cs   ← 不改
│   └── UrlBuilder.cs       ← 不改
├── App.xaml / App.xaml.cs   ← 不改
├── MainWindow.xaml          ← 加 ComboBox + 样式
├── MainWindow.xaml.cs       ← 核心改动
└── EzvizPlayer.csproj       ← 不改
```
