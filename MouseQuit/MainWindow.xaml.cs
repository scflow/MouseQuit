using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MouseClickSimulator
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cts; // 用于取消任务

        public MainWindow()
        {
            InitializeComponent();

            // 设置定时器实时更新鼠标位置
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += UpdateMousePosition;
            timer.Start();
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", EntryPoint = "mouse_event", SetLastError = true)]
        private static extern void MouseEvent(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // 实时更新鼠标坐标
        private void UpdateMousePosition(object sender, EventArgs e)
        {
            if (GetCursorPos(out POINT point))
            {
                MousePositionText.Text = $"X: {point.X}, Y: {point.Y}";
            }
        }

        // 切换窗口置顶状态
        private void ToggleTopmost(object sender, RoutedEventArgs e)
        {
            this.Topmost = !this.Topmost;

            // 更改置顶按钮背景
            PinButton.Background = this.Topmost ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Color.FromRgb(240, 240, 240));
            PinButton.BorderBrush = this.Topmost ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.FromRgb(208, 208, 208));

            // 自动显示通知
            ShowNotification(this.Topmost ? "窗口已置顶！" : "窗口已取消置顶！");
        }

        // 显示自定义通知
        private void ShowNotification(string message)
        {
            NotificationText.Text = message;
            NotificationBorder.Visibility = Visibility.Visible;
            // 设定 3 秒后通知自动淡出
            Task.Delay(3000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    NotificationBorder.Visibility = Visibility.Collapsed;
                });
            });
        }

        // 开始或停止模拟点击
        private async void StartStopSimulation(object sender, RoutedEventArgs e)
        {
            if (_cts == null)
            {
                // 启动模拟点击
                if (!int.TryParse(ClickCountBox.Text, out int clickCount) || clickCount <= 0)
                {
                    ShowNotification("请输入有效的点击次数！");
                    return;
                }

                if (!int.TryParse(IntervalBox.Text, out int interval) || interval < 0)
                {
                    ShowNotification("请输入有效的时间间隔！");
                    return;
                }

                // 延时 1 秒再开始模拟点击
                await Task.Delay(1000);

                _cts = new CancellationTokenSource();
                StartStopButton.Content = "结束模拟点击";

                try
                {
                    await SimulateClicks(clickCount, interval, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    ShowNotification("模拟点击已取消！");
                }
                finally
                {
                    _cts = null;
                    StartStopButton.Content = "开始模拟点击";
                }
            }
            else
            {
                // 停止模拟点击
                _cts.Cancel();
                StartStopButton.Content = "开始模拟点击";
            }
        }

        // 模拟点击的逻辑
        private async Task SimulateClicks(int clickCount, int interval, CancellationToken token)
        {
            for (int i = 0; i < clickCount; i++)
            {
                token.ThrowIfCancellationRequested();

                if (GetCursorPos(out POINT point))
                {
                    MouseEvent(MOUSEEVENTF_LEFTDOWN, point.X, point.Y, 0, 0);
                    MouseEvent(MOUSEEVENTF_LEFTUP, point.X, point.Y, 0, 0);
                }

                await Task.Delay(interval, token);
            }

            ShowNotification("模拟点击完成！");
        }

        // 置顶按钮鼠标悬停效果
        private void PinButton_MouseEnter(object sender, MouseEventArgs e)
        {
            PinButton.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        }

        private void PinButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!this.Topmost)
                PinButton.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
        }
    }
}
