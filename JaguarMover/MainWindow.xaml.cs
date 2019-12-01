using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
//using System.Windows.Forms;

namespace JaguarMover
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool connected = false;
        private bool eStop = true;

        private int driveSpeed, turnSpeed;
        private int forward, turn;
        private int front=30, rear=30;


        private readonly JagController controller = new JagController();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChangeDirection()
        {
            if (!connected)
            {
                return;
            }

            controller.Disconnect();
            controller.Move(forward, turn);
            DriveCurrSpeed.Content = forward.ToString();
            TurnCurrSped.Content = turn.ToString();
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                controller.Connect();
                ConnectBtn.Content = "Disconnect";
                connected = !connected;
            }
            else
            {
                controller.Disconnect();
                connected = !connected;
            }
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                MotorData a = new MotorData();
                TextBox.AppendText("\n");
                TextBox.AppendText($"{a.erorMsg} {a.ch1Temp} EncS:{a.motEncS1} EncP:{a.motEncP1}");
                return;
            }
            BateryLevel.Text = controller.MotorData[0].batVoltage.ToString(CultureInfo.InvariantCulture);
            TextBox.AppendText($"/n"+controller.MotorData[0]);

        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                return;
            }

            if (eStop)
            {
                controller.Stop();
                eStop = false;
                StopBtn.Content = "Enable E-stop";
                StopBtn.Background = Brushes.Red;
            }
            else
            {
                controller.StartMove();
                eStop = false;
                StopBtn.Content = "Disable E-stop";
                StopBtn.Background = Brushes.LawnGreen;
            }
        }        


        private void DriveSlider_ValueChanged(object sender,RoutedPropertyChangedEventArgs<double> routedPropertyChangedEventArgs)
        {
            int value = Convert.ToInt32(routedPropertyChangedEventArgs.NewValue);
            DriverSliderValue.Text = value.ToString();
            driveSpeed = value;
        }

        private void DriveSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!TurnSlider.IsFocused)
            {
                if (e.Delta < 0)
                {
                    DriveSlider.Value -= 50;
                }
                else if (e.Delta > 0)
                {
                    DriveSlider.Value += 50;
                }
            }
        }

        private void TurnSlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> routedPropertyChangedEventArgs)
        {
            int value = Convert.ToInt32(routedPropertyChangedEventArgs.NewValue);
            TurnSliderValue.Text = value.ToString();
            turnSpeed = value;
        }

        private void TurnSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                TurnSlider.Value -= 50;
            }
            else if (e.Delta > 0)
            {
                TurnSlider.Value += 50;
            }
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                controller.Disconnect();
            }
            catch (Exception)
            {
                // ignored
            }
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!connected)
            {
                return;
            }
            switch (e.Key)
            {
                case Key.W:
                    forward = driveSpeed;
                    break;
                case Key.S:
                    forward = -driveSpeed;
                    break;
                case Key.A:
                    turn = -turnSpeed;
                    break;
                case Key.D:
                    turn = turnSpeed;
                    break;
                case Key.Space:
                    controller.Stop();
                    break;
                case Key.Q:
                    controller.MoveFlippers(front,0);
                    break;
                case Key.E:
                    controller.MoveFlippers(-front,0);
                    break;
                case Key.LeftCtrl:
                    controller.MoveFlippers(0,rear);
                    break;
                case Key.LeftShift:
                    controller.MoveFlippers(0,-rear);
                    break;

            }

            ChangeDirection();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (!connected)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.W:
                case Key.S:
                    forward = 0;
                    ChangeDirection();
                    break;
                case Key.A:
                case Key.D:
                    turn = 0;
                    ChangeDirection();
                    break;
            }
        }

    }
}