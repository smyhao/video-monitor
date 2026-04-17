using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Wpf;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using EzvizPlayer.Models;
using EzvizPlayer.Services;

namespace EzvizPlayer
{
    public partial class MainWindow : Window
    {
        private bool _isFullscreen = false;
        private Rect _restoreBounds;
        private WindowState _restoreState;
        private LibVLC? _libVlc;
        private readonly List<LibVLCSharp.Shared.MediaPlayer> _mediaPlayers = new();
        private Config? _config;
        private readonly List<PageConfig> _pages = new();
        private int _currentPageIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _config = LoadConfig();

            if (_config == null)
            {
                MessageBox.Show("未找到有效的设备配置，请检查 config.json。", "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 向后兼容：旧配置包装为单页
            if (_config.Pages != null && _config.Pages.Count > 0)
            {
                _pages.AddRange(_config.Pages);
            }
            else
            {
                _pages.Add(new PageConfig
                {
                    Name = "默认页面",
                    Devices = _config.Devices ?? new List<DeviceConfig>(),
                    RtspDevices = _config.RtspDevices ?? new List<DeviceConfig>()
                });
            }

            var allDevicesAcrossPages = new List<DeviceConfig>();
            foreach (var page in _pages)
            {
                if (page.Devices != null) allDevicesAcrossPages.AddRange(page.Devices);
                if (page.RtspDevices != null) allDevicesAcrossPages.AddRange(page.RtspDevices);
            }

            if (allDevicesAcrossPages.Count == 0)
            {
                MessageBox.Show("未找到有效的设备配置，请检查 config.json。", "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 初始化 VLC：只要存在 RTSP 设备就初始化 LibVLC
            bool hasRtsp = allDevicesAcrossPages.Exists(d =>
                !string.IsNullOrWhiteSpace(d.RtspUrl) || d.Type == "rtsp");
            if (hasRtsp)
            {
                _libVlc = new LibVLC();
            }

            string titleText = string.IsNullOrWhiteSpace(_config.WindowTitle) ? "配送系统视频监控" : _config.WindowTitle;
            Title = titleText;
            HeaderTitle.Text = titleText;

            if (_pages.Count > 1)
            {
                PageComboBox.ItemsSource = _pages.Select(p => p.Name).ToList();
                PageComboBox.SelectedIndex = 0;
                PageComboBox.Visibility = Visibility.Visible;
            }
            else
            {
                PageComboBox.Visibility = Visibility.Collapsed;
            }

            if (_config.StartFullscreen)
            {
                ToggleFullscreen();
            }
            else if (_config.WindowWidth > 100 && _config.WindowHeight > 100)
            {
                Width = _config.WindowWidth;
                Height = _config.WindowHeight;
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left + (workArea.Width - Width) / 2;
                Top = workArea.Top + (workArea.Height - Height) / 2;
            }
            else
            {
                double ratio = Math.Max(0.3, Math.Min(1.0, _config.ScreenRatio));
                var workArea = SystemParameters.WorkArea;
                Width = workArea.Width * ratio;
                Height = workArea.Height * ratio;
                Left = workArea.Left + (workArea.Width - Width) / 2;
                Top = workArea.Top + (workArea.Height - Height) / 2;
            }

            SwitchToPage(0);
        }

        private void SwitchToPage(int index)
        {
            if (index < 0 || index >= _pages.Count) return;
            _currentPageIndex = index;

            // 1. 释放 VLC 资源
            foreach (var mp in _mediaPlayers)
            {
                mp.Stop();
                mp.Dispose();
            }
            _mediaPlayers.Clear();

            // 2. 释放 WebView2 和 VideoView 控件
            foreach (UIElement element in VideoGrid.Children)
            {
                if (element is Border container && container.Child is Grid layout)
                {
                    foreach (var child in layout.Children)
                    {
                        if (Grid.GetRow((UIElement)child) == 1 && child is Grid contentGrid)
                        {
                            foreach (var inner in contentGrid.Children)
                            {
                                if (inner is WebView2 wv) wv.Dispose();
                                if (inner is VideoView vv) vv.Dispose();
                            }
                        }
                    }
                }
            }

            // 3. 清空 VideoGrid
            VideoGrid.Children.Clear();
            VideoGrid.RowDefinitions.Clear();
            VideoGrid.ColumnDefinitions.Clear();

            var page = _pages[index];
            var devices = new List<DeviceConfig>();
            if (page.Devices != null) devices.AddRange(page.Devices);
            if (page.RtspDevices != null) devices.AddRange(page.RtspDevices);

            // 3. 计算网格
            var (rows, cols) = GridCalculator.Calculate(devices.Count);

            for (int r = 0; r < rows; r++)
                VideoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            for (int c = 0; c < cols; c++)
                VideoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 4. 创建面板
            int i = 0;
            foreach (var dev in devices)
            {
                int r = i / cols;
                int c = i % cols;
                var panel = CreateVideoPanel(dev, _config!);
                Grid.SetRow(panel, r);
                Grid.SetColumn(panel, c);
                VideoGrid.Children.Add(panel);
                i++;
            }

            // 5. 更新副标题和 ComboBox
            string subtitle = $"{devices.Count} 路视频  ·  {page.Name}";
            HeaderSubtitle.Text = subtitle;
            if (PageComboBox.SelectedIndex != index)
            {
                PageComboBox.SelectedIndex = index;
            }
        }

        private void PageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageComboBox.SelectedIndex >= 0 && PageComboBox.SelectedIndex != _currentPageIndex)
            {
                SwitchToPage(PageComboBox.SelectedIndex);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var mp in _mediaPlayers)
            {
                mp.Stop();
                mp.Dispose();
            }
            _mediaPlayers.Clear();
            _libVlc?.Dispose();
        }

        private Border CreateVideoPanel(DeviceConfig dev, Config config)
        {
            var container = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromRgb(13, 31, 58)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 107)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(3),
                ClipToBounds = true
            };

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            bool isRtsp = !string.IsNullOrWhiteSpace(dev.RtspUrl) || dev.Type == "rtsp";
            string rtspUrl = !string.IsNullOrWhiteSpace(dev.RtspUrl) ? dev.RtspUrl : dev.Url;
            string statusText = isRtsp ? "RTSP" : (dev.Mode == "rec" ? "回放" : "直播中");

            // 标题栏
            var titleBar = new Border
            {
                Height = 26,
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(20, 43, 82))
            };

            var titleGrid = new Grid { Margin = new Thickness(8, 0, 8, 0) };
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 状态圆点
            var statusDot = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = isRtsp ? new SolidColorBrush(Color.FromRgb(250, 204, 21)) :
                      (dev.Mode == "rec" ? new SolidColorBrush(Colors.OrangeRed) : new SolidColorBrush(Color.FromRgb(34, 211, 238))),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(statusDot, 0);
            titleGrid.Children.Add(statusDot);

            // 设备名称
            var nameText = new TextBlock
            {
                Text = dev.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 244, 255)),
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameText, 1);
            titleGrid.Children.Add(nameText);

            // 状态标签
            var modeText = new TextBlock
            {
                Text = statusText,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 184, 232)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(modeText, 2);
            titleGrid.Children.Add(modeText);

            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            layout.Children.Add(titleBar);

            // 视频内容区
            var contentGrid = new Grid { Background = new SolidColorBrush(Color.FromRgb(5, 16, 32)) };
            Grid.SetRow(contentGrid, 1);

            try
            {
                if (isRtsp)
                {
                    CreateRtspPlayer(contentGrid, rtspUrl);
                }
                else
                {
                    CreateEzvizPlayer(contentGrid, config, dev);
                }
            }
            catch (Exception ex)
            {
                contentGrid.Children.Add(CreateErrorView(ex.Message));
            }

            layout.Children.Add(contentGrid);
            container.Child = layout;
            return container;
        }

        private void CreateEzvizPlayer(Grid contentGrid, Config config, DeviceConfig dev)
        {
            string url = UrlBuilder.BuildUrl(config, dev);
            var webView = new WebView2
            {
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var loadingGrid = CreateLoadingIndicator();
            contentGrid.Children.Add(loadingGrid);

            webView.NavigationCompleted += (s, e) =>
            {
                loadingGrid.Visibility = Visibility.Collapsed;
                if (!e.IsSuccess)
                {
                    contentGrid.Children.Add(CreateErrorView($"网页加载失败：{e.WebErrorStatus}"));
                }
            };

            contentGrid.Children.Add(webView);
            _ = InitializeWebViewAsync(webView, url, contentGrid, loadingGrid);
        }

        private async Task InitializeWebViewAsync(WebView2 webView, string url, Grid contentGrid, Grid loadingGrid)
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                loadingGrid.Visibility = Visibility.Collapsed;
                contentGrid.Children.Add(CreateErrorView($"WebView2 初始化失败\n{ex.Message}"));
            }
        }

        private void CreateRtspPlayer(Grid contentGrid, string url)
        {
            if (_libVlc == null)
                throw new InvalidOperationException("VLC 未初始化");
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("RTSP 设备缺少 url 配置");

            var videoView = new VideoView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0)
            };

            var loadingGrid = CreateLoadingIndicator();
            contentGrid.Children.Add(loadingGrid);

            var mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc);
            _mediaPlayers.Add(mediaPlayer);
            videoView.MediaPlayer = mediaPlayer;
            videoView.Tag = url;

            videoView.Loaded += (s, e) =>
            {
                using var media = new Media(_libVlc, new Uri(url));
                mediaPlayer.Play(media);
                loadingGrid.Visibility = Visibility.Collapsed;
            };

            contentGrid.Children.Add(videoView);
        }

        private Grid CreateLoadingIndicator()
        {
            var grid = new Grid { Background = new SolidColorBrush(Color.FromRgb(5, 16, 32)) };

            var spinner = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                BorderBrush = new SolidColorBrush(Color.FromRgb(96, 165, 250)),
                BorderThickness = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18),
                Opacity = 0.9
            };
            var clip = new RectangleGeometry(new Rect(0, 0, 28, 14));
            spinner.Clip = clip;
            grid.Children.Add(spinner);

            var text = new TextBlock
            {
                Text = "正在连接...",
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 140)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 36, 0, 0)
            };
            grid.Children.Add(text);

            return grid;
        }

        private Grid CreateErrorView(string message)
        {
            var grid = new Grid { Background = new SolidColorBrush(Color.FromRgb(5, 16, 32)) };

            var icon = new TextBlock
            {
                Text = "⚠",
                Foreground = new SolidColorBrush(Colors.OrangeRed),
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24)
            };
            grid.Children.Add(icon);

            var text = new TextBlock
            {
                Text = $"加载失败\n{message}",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 100, 100)),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(8, 24, 8, 8)
            };
            grid.Children.Add(text);

            return grid;
        }

        private void HeaderBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleFullscreen();
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllVideos();
        }

        private void RefreshAllVideos()
        {
            foreach (UIElement element in VideoGrid.Children)
            {
                if (element is Border container && container.Child is Grid layout)
                {
                    foreach (var child in layout.Children)
                    {
                        if (Grid.GetRow((UIElement)child) == 1 && child is Grid contentGrid)
                        {
                            Grid? loadingGrid = null;
                            WebView2? webView = null;
                            VideoView? videoView = null;

                            foreach (var inner in contentGrid.Children)
                            {
                                if (inner is Grid lg && lg.Background is SolidColorBrush scb && scb.Color == Color.FromRgb(5, 16, 32)) loadingGrid = lg;
                                if (inner is WebView2 wv) webView = wv;
                                if (inner is VideoView vv) videoView = vv;
                            }

                            if (webView != null)
                            {
                                if (loadingGrid != null)
                                    loadingGrid.Visibility = Visibility.Visible;

                                if (webView.CoreWebView2 != null)
                                {
                                    webView.CoreWebView2.Reload();
                                }
                                else if (webView.Source != null)
                                {
                                    webView.Source = webView.Source;
                                }
                            }
                            else if (videoView != null && videoView.MediaPlayer != null)
                            {
                                if (loadingGrid != null)
                                    loadingGrid.Visibility = Visibility.Visible;

                                var rtspUrl = videoView.Tag as string;
                                var mp = videoView.MediaPlayer;
                                if (!string.IsNullOrWhiteSpace(rtspUrl) && _libVlc != null)
                                {
                                    try
                                    {
                                        mp.Stop();
                                        using var media = new Media(_libVlc, new Uri(rtspUrl));
                                        mp.Play(media);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (loadingGrid != null)
                                            loadingGrid.Visibility = Visibility.Collapsed;
                                        contentGrid.Children.Add(CreateErrorView($"RTSP 刷新失败\n{ex.Message}"));
                                    }
                                }

                                if (loadingGrid != null)
                                    loadingGrid.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
        }

        private void ToggleFullscreen()
        {
            if (_isFullscreen)
            {
                // 退出全屏
                _isFullscreen = false;
                HeaderBar.Height = 44;
                HeaderBar.Background = new SolidColorBrush(Color.FromRgb(11, 29, 61));
                ResizeMode = ResizeMode.CanResizeWithGrip;
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 107));
                BorderThickness = new Thickness(1);

                WindowState = _restoreState;
                if (_restoreState == WindowState.Normal && _restoreBounds.Width > 0 && _restoreBounds.Height > 0)
                {
                    Left = _restoreBounds.Left;
                    Top = _restoreBounds.Top;
                    Width = _restoreBounds.Width;
                    Height = _restoreBounds.Height;
                }

                MaximizeButton.Content = "\uE922"; // 最大化图标
                MaximizeButton.ToolTip = "全屏";
            }
            else
            {
                // 进入全屏
                _isFullscreen = true;
                _restoreBounds = new Rect(Left, Top, Width, Height);
                _restoreState = WindowState;

                HeaderBar.Height = 36;
                HeaderBar.Background = new SolidColorBrush(Color.FromArgb(230, 11, 29, 61));
                ResizeMode = ResizeMode.NoResize;
                BorderThickness = new Thickness(0);

                // 必须先 Normal 再设置尺寸，才能真正覆盖任务栏
                var targetBounds = GetCurrentScreenBounds();
                WindowState = WindowState.Normal;
                Left = targetBounds.Left;
                Top = targetBounds.Top;
                Width = targetBounds.Width;
                Height = targetBounds.Height;

                MaximizeButton.Content = "\uE923"; // 还原图标
                MaximizeButton.ToolTip = "退出全屏";
            }
        }

        private Rect GetCurrentScreenBounds()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var info = new MonitorInfo();
                    info.cbSize = Marshal.SizeOf<MonitorInfo>();
                    if (GetMonitorInfo(monitor, ref info))
                    {
                        // GetMonitorInfo 返回的是物理像素坐标，需要转换成 WPF 的 DIP 坐标。
                        var source = PresentationSource.FromVisual(this);
                        if (source?.CompositionTarget != null)
                        {
                            var fromDevice = source.CompositionTarget.TransformFromDevice;
                            var topLeft = fromDevice.Transform(new Point(info.rcMonitor.Left, info.rcMonitor.Top));
                            var bottomRight = fromDevice.Transform(new Point(info.rcMonitor.Right, info.rcMonitor.Bottom));
                            return new Rect(topLeft, bottomRight);
                        }

                        return new Rect(
                            info.rcMonitor.Left,
                            info.rcMonitor.Top,
                            info.rcMonitor.Right - info.rcMonitor.Left,
                            info.rcMonitor.Bottom - info.rcMonitor.Top);
                    }
                }
            }

            return new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MonitorInfo
        {
            public int cbSize;
            public NativeRect rcMonitor;
            public NativeRect rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        private static Config? LoadConfig()
        {
            var candidates = new[]
            {
                System.IO.Path.Combine(AppContext.BaseDirectory, "config.json"),
                System.IO.Path.Combine(Environment.CurrentDirectory, "config.json"),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config.json"))
            }.Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var path in candidates)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    var json = File.ReadAllText(path, Encoding.UTF8);
                    var config = JsonSerializer.Deserialize<Config>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (JsonException) { }
            }
            return null;
        }
    }
}
