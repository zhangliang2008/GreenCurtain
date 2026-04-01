using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace GreenCurtain
{
    public partial class SettingsWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // Windows API 用于获取屏幕像素颜色
        [LibraryImport("user32.dll")]
        private static partial IntPtr GetDC(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        private static partial int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [LibraryImport("gdi32.dll")]
        private static partial uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetCursorPos(ref POINT lpPoint);

        // 当前颜色
        private System.Windows.Media.Color currentColor = System.Windows.Media.Colors.LimeGreen;
        // 当前透明度
        private double currentOpacity = 0.5;
        // 当前中心亮度和边缘亮度
        private byte currentCenterBrightness = 128;
        private byte currentEdgeBrightness = 176;
        // 当前显示器索引
        private int currentScreenIndex = 0;

        // 快捷键设置
        private int exitHotkeyModifiers = 1; // Alt
        private int exitHotkeyKey = 81; // Q
        private int toggleHotkeyModifiers = 1; // Alt
        private int toggleHotkeyKey = 65; // A

        // 事件委托
        public event Action<System.Windows.Media.Color, double, byte, byte, int, int, int, int, int>? SettingsChanged;

        public SettingsWindow()
        {
            InitializeComponent();
            InitializeScreenList();
            InitializeUI();
        }

        // 初始化显示器列表
        private void InitializeScreenList()
        {
            ScreenComboBox.Items.Clear();
            Screen[] screens = Screen.AllScreens;
            
            for (int i = 0; i < screens.Length; i++)
            {
                string screenName = $"显示器 {i + 1}";
                if (screens[i].Primary)
                {
                    screenName += " (主显示器)";
                }
                screenName += $" [{screens[i].Bounds.Width}x{screens[i].Bounds.Height}]";
                ScreenComboBox.Items.Add(screenName);
            }
            
            if (screens.Length > 0)
            {
                ScreenComboBox.SelectedIndex = 0;
            }
            
            ScreenComboBox.SelectionChanged += ScreenComboBox_SelectionChanged;
        }

        private void InitializeUI()
        {
            // 设置初始颜色
            UpdateColorButtonAppearance();
            // 设置滑块事件
            OpacitySlider.ValueChanged += OpacitySlider_ValueChanged;
            CenterBrightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;
            EdgeBrightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;
            // 更新文本显示
            UpdateOpacityText();
            UpdateBrightnessText();
        }

        private void ScreenComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScreenComboBox.SelectedIndex >= 0)
            {
                currentScreenIndex = ScreenComboBox.SelectedIndex;
                OnSettingsChanged();
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开颜色选择器
            ColorDialog colorDialog = new();
            colorDialog.Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B);
            colorDialog.FullOpen = true;

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // 更新当前颜色
                currentColor = System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                UpdateColorButtonAppearance();
                // 触发设置更改事件
                OnSettingsChanged();
            }
        }

        private void PickColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColorButton.Content = "点击屏幕...";
            PickColorButton.IsEnabled = false;
            
            double left = this.Left;
            double top = this.Top;
            this.Hide();
            
            System.Windows.Media.Color transparentColor = System.Windows.Media.Color.FromArgb(1, 0, 0, 0);
            System.Windows.Media.SolidColorBrush transparentBrush = new (transparentColor);
            
            Window pickWindow = new()
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = transparentBrush,
                Topmost = true,
                Left = System.Windows.SystemParameters.VirtualScreenLeft,
                Top = System.Windows.SystemParameters.VirtualScreenTop,
                Width = System.Windows.SystemParameters.VirtualScreenWidth,
                Height = System.Windows.SystemParameters.VirtualScreenHeight,
                Cursor = System.Windows.Input.Cursors.Cross,
                Focusable = true
            };
            
            pickWindow.MouseLeftButtonDown += (s, args) =>
            {
                System.Drawing.Point mousePoint = System.Windows.Forms.Cursor.Position;
                
                IntPtr hdc = GetDC(IntPtr.Zero);
                uint pixel = GetPixel(hdc, mousePoint.X, mousePoint.Y);
                ReleaseDC(IntPtr.Zero, hdc);
                
                byte r = (byte)(pixel & 0x000000FF);
                byte g = (byte)((pixel & 0x0000FF00) >> 8);
                byte b = (byte)((pixel & 0x00FF0000) >> 16);
                
                currentColor = System.Windows.Media.Color.FromRgb(r, g, b);
                
                pickWindow.Close();
                
                StopPickingColor();
                
                this.Left = left;
                this.Top = top;
                this.Show();
                
                UpdateColorButtonAppearance();
                OnSettingsChanged();
            };
            
            pickWindow.KeyDown += (s, args) =>
            {
                if (args.Key == System.Windows.Input.Key.Escape)
                {
                    pickWindow.Close();
                    StopPickingColor();
                    this.Left = left;
                    this.Top = top;
                    this.Show();
                }
            };
            
            pickWindow.Loaded += (s, args) =>
            {
                pickWindow.Activate();
                pickWindow.Focus();
            };
            
            pickWindow.Show();
        }

        private void StopPickingColor()
        {
            PickColorButton.Content = "吸取";
            PickColorButton.IsEnabled = true;
        }

        private void UpdateColorButtonAppearance()
        {
            // 创建颜色预览
            LinearGradientBrush brush = new (currentColor, currentColor, 0);
            ColorButton.Background = brush;
            ColorButton.Content = $"RGB({currentColor.R}, {currentColor.G}, {currentColor.B})";
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentOpacity = e.NewValue;
            UpdateOpacityText();
            OnSettingsChanged();
        }

        private void UpdateOpacityText()
        {
            OpacityText.Text = $"{(int)(currentOpacity * 100)}%";
        }

        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender == CenterBrightnessSlider)
            {
                currentCenterBrightness = (byte)e.NewValue;
            }
            else if (sender == EdgeBrightnessSlider)
            {
                currentEdgeBrightness = (byte)e.NewValue;
            }
            UpdateBrightnessText();
            OnSettingsChanged();
        }

        private void UpdateBrightnessText()
        {
            CenterBrightnessText.Text = currentCenterBrightness.ToString();
            EdgeBrightnessText.Text = currentEdgeBrightness.ToString();
        }

        protected virtual void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(currentColor, currentOpacity, currentCenterBrightness, currentEdgeBrightness, currentScreenIndex, exitHotkeyModifiers, exitHotkeyKey, toggleHotkeyModifiers, toggleHotkeyKey);
        }

        // 设置当前值（用于从主窗口传递初始设置）
        public void SetCurrentValues(System.Windows.Media.Color color, double opacity, byte centerBrightness, byte edgeBrightness, int screenIndex, int exitMods, int exitKey, int toggleMods, int toggleKey)
        {
            currentColor = color;
            currentOpacity = opacity;
            currentCenterBrightness = centerBrightness;
            currentEdgeBrightness = edgeBrightness;
            currentScreenIndex = screenIndex;
            exitHotkeyModifiers = exitMods;
            exitHotkeyKey = exitKey;
            toggleHotkeyModifiers = toggleMods;
            toggleHotkeyKey = toggleKey;
            
            // 更新UI控件
            OpacitySlider.Value = opacity;
            CenterBrightnessSlider.Value = centerBrightness;
            EdgeBrightnessSlider.Value = edgeBrightness;
            
            // 更新显示器选择
            if (screenIndex >= 0 && screenIndex < ScreenComboBox.Items.Count)
            {
                ScreenComboBox.SelectedIndex = screenIndex;
            }
            
            // 更新快捷键显示
            ExitHotkeyTextBox.Text = GetHotkeyString(exitMods, exitKey);
            ToggleHotkeyTextBox.Text = GetHotkeyString(toggleMods, toggleKey);
            
            // 更新显示文本
            UpdateColorButtonAppearance();
            UpdateOpacityText();
            UpdateBrightnessText();
        }
        
        // 获取快捷键显示字符串
        private string GetHotkeyString(int modifiers, int key)
        {
            string result = "";
            if ((modifiers & 1) != 0) result += "Alt+";
            if ((modifiers & 2) != 0) result += "Ctrl+";
            if ((modifiers & 4) != 0) result += "Shift+";
            if ((modifiers & 8) != 0) result += "Win+";
            result += ((System.Windows.Forms.Keys)key).ToString();
            return result;
        }
        
        // 解析快捷键
        private void ParseHotkey(System.Windows.Input.KeyEventArgs e, out int modifiers, out int key)
        {
            modifiers = 0;
            key = 0;
            
            if (e.KeyboardDevice.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                modifiers |= 1;
            if (e.KeyboardDevice.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                modifiers |= 2;
            if (e.KeyboardDevice.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                modifiers |= 4;
            
            // 获取按键
            System.Windows.Input.Key pressedKey = e.Key;
            if (pressedKey == System.Windows.Input.Key.System)
                pressedKey = e.SystemKey;
            
            if (pressedKey != System.Windows.Input.Key.None)
            {
                key = KeyInterop.VirtualKeyFromKey(pressedKey);
            }
        }
        
        // 退出快捷键输入
        private void ExitHotkeyTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ParseHotkey(e, out int mods, out int key);
            if (key > 0 && mods > 0)
            {
                exitHotkeyModifiers = mods;
                exitHotkeyKey = key;
                ExitHotkeyTextBox.Text = GetHotkeyString(mods, key);
                OnSettingsChanged();
            }
            e.Handled = true;
        }
        
        private void ExitHotkeyTextBox_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ExitHotkeyTextBox.Focus();
            e.Handled = true;
        }
        
        // 切换快捷键输入
        private void ToggleHotkeyTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ParseHotkey(e, out int mods, out int key);
            if (key > 0 && mods > 0)
            {
                toggleHotkeyModifiers = mods;
                toggleHotkeyKey = key;
                ToggleHotkeyTextBox.Text = GetHotkeyString(mods, key);
                OnSettingsChanged();
            }
            e.Handled = true;
        }
        
        private void ToggleHotkeyTextBox_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToggleHotkeyTextBox.Focus();
            e.Handled = true;
        }
    }
}