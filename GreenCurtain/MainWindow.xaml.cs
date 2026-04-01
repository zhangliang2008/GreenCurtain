using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Forms;

namespace GreenCurtain
{
    public partial class MainWindow : Window
    {
        // Windows API常量
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = -20;
        private const int WM_HOTKEY = 0x0312;

        // 热键ID
        private const int HOTKEY_EXIT = 1;
        private const int HOTKEY_TOGGLE = 2;

        // Windows API函数
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return GetWindowLongPtr(hWnd, nIndex);
        }

        private static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            return (int)SetWindowLongPtr(hWnd, nIndex, (IntPtr)dwNewLong);
        }

        // 系统托盘图标
        private NotifyIcon? notifyIcon;
        // 设置窗口
        private SettingsWindow? settingsWindow;
        // 当前设置
        private System.Windows.Media.Color currentColor = System.Windows.Media.Colors.LimeGreen;
        private double currentOpacity = 0.5;
        private byte currentCenterBrightness = 128;
        private byte currentEdgeBrightness = 176;
        // 当前显示器索引
        private int currentScreenIndex = 0;
        // 绿幕是否隐藏
        private bool isHidden = false;
        // 窗口句柄
        private IntPtr hwnd;
        // 快捷键设置
        private int exitHotkeyModifiers = 1; // Alt
        private int exitHotkeyKey = 81; // Q
        private int toggleHotkeyModifiers = 1; // Alt
        private int toggleHotkeyKey = 65; // A

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            InitializeTrayIcon();
            MoveToScreen(currentScreenIndex);
            UpdateGradientBackground();
        }

        private void LoadSettings()
        {
            AppSettings settings = AppSettings.Load();
            currentColor = System.Windows.Media.Color.FromRgb(settings.ColorR, settings.ColorG, settings.ColorB);
            currentOpacity = settings.Opacity;
            currentCenterBrightness = settings.CenterBrightness;
            currentEdgeBrightness = settings.EdgeBrightness;
            currentScreenIndex = settings.ScreenIndex;
            exitHotkeyModifiers = settings.ExitHotkeyModifiers;
            exitHotkeyKey = settings.ExitHotkeyKey;
            toggleHotkeyModifiers = settings.ToggleHotkeyModifiers;
            toggleHotkeyKey = settings.ToggleHotkeyKey;
        }

        private void SaveSettings()
        {
            AppSettings settings = new()
            {
                ColorR = currentColor.R,
                ColorG = currentColor.G,
                ColorB = currentColor.B,
                Opacity = currentOpacity,
                CenterBrightness = currentCenterBrightness,
                EdgeBrightness = currentEdgeBrightness,
                ScreenIndex = currentScreenIndex,
                ExitHotkeyModifiers = exitHotkeyModifiers,
                ExitHotkeyKey = exitHotkeyKey,
                ToggleHotkeyModifiers = toggleHotkeyModifiers,
                ToggleHotkeyKey = toggleHotkeyKey
            };
            settings.Save();
        }

        // 初始化系统托盘图标
        private void InitializeTrayIcon()
        {
            // 创建通知图标
            notifyIcon = new NotifyIcon();
            
            // 尝试加载自定义图标
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favicon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    notifyIcon.Icon = new System.Drawing.Icon(SystemIcons.Application, 40, 40);
                }
            }
            catch
            {
                notifyIcon.Icon = new System.Drawing.Icon(SystemIcons.Application, 40, 40);
            }
            
            notifyIcon.Text = "绿幕";
            notifyIcon.Visible = true;

            // 创建上下文菜单
            ContextMenuStrip contextMenuStrip = new();
            ToolStripMenuItem settingsMenuItem = new("设置");
            settingsMenuItem.Click += SettingsMenuItem_Click;
            ToolStripMenuItem exitMenuItem = new("退出");
            exitMenuItem.Click += ExitMenuItem_Click;

            contextMenuStrip.Items.Add(settingsMenuItem);
            contextMenuStrip.Items.Add(exitMenuItem);
            notifyIcon.ContextMenuStrip = contextMenuStrip;

            // 双击托盘图标
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        // 显示设置窗口
        private void SettingsMenuItem_Click(object? sender, EventArgs e)
        {
            ShowSettingsWindow();
        }

        // 退出程序
        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        // 双击托盘图标显示/隐藏设置窗口
        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowSettingsWindow();
        }

        // 显示设置窗口
        private void ShowSettingsWindow()
        {
            if (settingsWindow == null || !settingsWindow.IsVisible)
            {
                settingsWindow = new()
                {
                    Owner = this
                };
                settingsWindow.SettingsChanged += SettingsWindow_SettingsChanged;
                settingsWindow.SetCurrentValues(currentColor, currentOpacity, currentCenterBrightness, currentEdgeBrightness, currentScreenIndex, exitHotkeyModifiers, exitHotkeyKey, toggleHotkeyModifiers, toggleHotkeyKey);
                settingsWindow.Show();
            }
            else
            {
                settingsWindow.Activate();
            }
        }

        // 处理设置更改
        private void SettingsWindow_SettingsChanged(System.Windows.Media.Color color, double opacity, byte centerBrightness, byte edgeBrightness, int screenIndex, int exitMods, int exitKey, int toggleMods, int toggleKey)
        {
            currentColor = color;
            currentOpacity = opacity;
            currentCenterBrightness = centerBrightness;
            currentEdgeBrightness = edgeBrightness;
            
            // 检查快捷键是否更改
            bool hotkeyChanged = (exitHotkeyModifiers != exitMods || exitHotkeyKey != exitKey || 
                                  toggleHotkeyModifiers != toggleMods || toggleHotkeyKey != toggleKey);
            
            exitHotkeyModifiers = exitMods;
            exitHotkeyKey = exitKey;
            toggleHotkeyModifiers = toggleMods;
            toggleHotkeyKey = toggleKey;
            
            if (currentScreenIndex != screenIndex)
            {
                currentScreenIndex = screenIndex;
                MoveToScreen(screenIndex);
            }
            
            // 如果快捷键更改，重新注册热键
            if (hotkeyChanged && hwnd != IntPtr.Zero)
            {
                HotKey.UnregisterHotKey(hwnd, HOTKEY_EXIT);
                HotKey.UnregisterHotKey(hwnd, HOTKEY_TOGGLE);
                HotKey.RegisterHotKey(hwnd, HOTKEY_EXIT, exitHotkeyModifiers, exitHotkeyKey);
                HotKey.RegisterHotKey(hwnd, HOTKEY_TOGGLE, toggleHotkeyModifiers, toggleHotkeyKey);
            }

            UpdateGradientBackground();
            SaveSettings();
        }

        // 移动窗口到指定显示器
        private void MoveToScreen(int screenIndex)
        {
            Screen[] screens = Screen.AllScreens;
            
            if (screenIndex < 0 || screenIndex >= screens.Length)
            {
                screenIndex = 0;
            }

            Screen targetScreen = screens[screenIndex];
            
            // 设置窗口位置和大小
            Left = targetScreen.Bounds.Left;
            Top = targetScreen.Bounds.Top;
            Width = targetScreen.Bounds.Width;
            Height = targetScreen.Bounds.Height;
        }

        // 更新渐变背景
        private void UpdateGradientBackground()
        {
            // 创建径向渐变背景
            RadialGradientBrush brush = new()
            {
                GradientOrigin = new System.Windows.Point(0.5, 0.5),
                Center = new System.Windows.Point(0.5, 0.5),
                RadiusX = 1,
                RadiusY = 1
            };

            // 计算透明度值（0-255）
            byte alphaValue = (byte)(currentOpacity * 255);

            // 计算亮度补偿系数（0.5-1.5）
            // 128 = 1.0（原始亮度），255 = 1.5，0 = 0.5
            double centerBrightnessFactor = 0.5 + (currentCenterBrightness / 255.0);
            double edgeBrightnessFactor = 0.5 + (currentEdgeBrightness / 255.0);

            // 中心颜色（应用亮度补偿）
            System.Windows.Media.Color centerColor = System.Windows.Media.Color.FromArgb(
                alphaValue,
                (byte)Math.Min(255, currentColor.R * centerBrightnessFactor),
                (byte)Math.Min(255, currentColor.G * centerBrightnessFactor),
                (byte)Math.Min(255, currentColor.B * centerBrightnessFactor)
            );

            // 边缘颜色（应用亮度补偿）
            System.Windows.Media.Color edgeColor = System.Windows.Media.Color.FromArgb(
                alphaValue,
                (byte)Math.Min(255, currentColor.R * edgeBrightnessFactor),
                (byte)Math.Min(255, currentColor.G * edgeBrightnessFactor),
                (byte)Math.Min(255, currentColor.B * edgeBrightnessFactor)
            );

            brush.GradientStops.Add(new GradientStop(centerColor, 0.0));
            brush.GradientStops.Add(new GradientStop(edgeColor, 1.0));

            // 更新网格背景
            ((Grid)Content).Background = brush;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 获取窗口句柄
            hwnd = new WindowInteropHelper(this).Handle;

            // 设置窗口为透明点击
            IntPtr extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, (int)extendedStyle | WS_EX_TRANSPARENT);

            // 注册热键
            HotKey.RegisterHotKey(hwnd, HOTKEY_EXIT, exitHotkeyModifiers, exitHotkeyKey);
            HotKey.RegisterHotKey(hwnd, HOTKEY_TOGGLE, toggleHotkeyModifiers, toggleHotkeyKey);

            // 添加窗口消息处理
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);
        }

        // 处理窗口消息
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_EXIT:
                        // Alt+Q 退出程序
                        System.Windows.Application.Current.Shutdown();
                        handled = true;
                        break;
                    case HOTKEY_TOGGLE:
                        // Alt+A 隐藏/显示绿幕
                        ToggleGreenCurtain();
                        handled = true;
                        break;
                }
            }
            return IntPtr.Zero;
        }

        // 切换绿幕显示/隐藏
        private void ToggleGreenCurtain()
        {
            isHidden = !isHidden;
            if (isHidden)
            {
                Hide();
                if (notifyIcon != null)
                {
                    notifyIcon.Text = "绿幕 (已隐藏)";
                }
            }
            else
            {
                Show();
                if (notifyIcon != null)
                {
                    notifyIcon.Text = "绿幕";
                }
            }
        }

        // 窗口关闭时清理资源
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 注销热键
            if (hwnd != IntPtr.Zero)
            {
                HotKey.UnregisterHotKey(hwnd, HOTKEY_EXIT);
                HotKey.UnregisterHotKey(hwnd, HOTKEY_TOGGLE);
            }
            
            notifyIcon?.Dispose();
            if (settingsWindow != null)
            {
                settingsWindow.Close();
            }
        }
    }
}
