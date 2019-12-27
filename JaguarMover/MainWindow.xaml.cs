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
        private bool connected;
        private bool eStop = true;
        private bool light = true;

        private int driveSpeed, turnSpeed;
        private int frontSpeed, rearSpeed;

        private int forward, turn;
        private int fFlipper = 30, rFlipper = 30;


        private readonly JagController controller = new JagController();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Move()
        {
            if (!connected)
            {
                return;
            }

//            controller.Disconnect(); WTF
            controller.Move(forward, turn);
            DriveCurrSpeed.Content = forward.ToString();
            TurnCurrSped.Content = turn.ToString();
        }

        private void MoveFlipers()
        {
            if (!connected)
            {
                return;
            }

            controller.MoveFlippers(frontSpeed, rearSpeed);
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                controller.Connect();
                ConnectBtn.Content = "Disconnect";
                connected = !connected;
                BateryLevel.Text = controller.MotorData[0].batVoltage.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                controller.Disconnect();
                ConnectBtn.Content = "Connect";
                connected = !connected;
            }
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                return;
            }

            BateryLevel.Text = controller.MotorData[0].batVoltage.ToString(CultureInfo.InvariantCulture);
            TextBox.AppendText("\n");
            MotorData a = controller.MotorData[1];
            TextBox.AppendText($"{a.reg5VVoltage}  {a.ch1Temp}   EncS:{a.motEncS1} EncP:{a.motEncP1}");
            for (int i = 0; i < 4; i++)
            {
                controller.MotorData[i].erorMsg = "";
            }
        }

        private void EStopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                return;
            }

            if (!eStop)
            {
                controller.Stop();
                eStop = true;
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


        private void DriveSlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> routedPropertyChangedEventArgs)
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

        private void FlipperSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            if (e.Delta < 0)
            {
                FlipperSlider.Value -= 50;
            }
            else if (e.Delta > 0)
            {
                FlipperSlider.Value += 50;
            }
        }

        private void FlipperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> routedPropertyChangedEventArgs)
        {
            int value = Convert.ToInt32(routedPropertyChangedEventArgs.NewValue);
            FlipperSliderValue.Text = value.ToString();
            frontSpeed = value;
        }


        private void LightBtn_Click(object sender, RoutedEventArgs e)
        {
            controller.Light(!light);
            light = !light;
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

            Application.Current.Shutdown();
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool chMovement = false, chFlippers = false;
            if (!connected)
            {
                return;
            }

            frontSpeed = 0;
            rearSpeed = 0;

            switch (e.Key)
            {
                case Key.W:
                    forward = driveSpeed;
                    chMovement = true;
                    break;
                case Key.S:
                    forward = -driveSpeed;
                    chMovement = true;
                    break;
                case Key.A:
                    turn = -turnSpeed;
                    chMovement = true;
                    break;
                case Key.D:
                    turn = turnSpeed;
                    chMovement = true;
                    break;
                case Key.Space:
                    controller.Stop();
                    eStop = true;
                    break;
                case Key.Q:
                    frontSpeed = fFlipper;
                    chFlippers = true;
                    break;
                case Key.E:
                    frontSpeed = -fFlipper;
                    chFlippers = true;
                    break;
                case Key.LeftCtrl:
                    rearSpeed = rFlipper;
                    chFlippers = true;
                    break;
                case Key.LeftShift:
                    rearSpeed = -rFlipper;
                    chFlippers = true;
                    break;
            }

            if (chMovement)
                Move();
            else if(chFlippers)
                MoveFlipers();
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
                    Move();
                    break;
                case Key.A:
                case Key.D:
                    turn = 0;
                    Move();
                    break;
            }
        }
    }
}