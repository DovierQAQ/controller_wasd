using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace wasd
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool keyW, keyA, keyS, keyD, keyShift;
        private string[] ports;
        private SerialPort comPort = new SerialPort();
        private bool serialPort_isOpen = false;
        private DispatcherTimer controller = new DispatcherTimer();
        private bool controller_isStarted = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshComPort();

            combobox_baudrate.SelectedIndex = 9;
            combobox_checkbit.SelectedIndex = 0;
            combobox_databit.SelectedIndex = 3;
            combobox_stopbit.SelectedIndex = 0;

            comPort.ReadTimeout = 8000;
            comPort.WriteTimeout = 8000;
            comPort.ReadBufferSize = 1024;
            comPort.WriteBufferSize = 1024;
            comPort.DataReceived += new SerialDataReceivedEventHandler(Com_DataReceived);

            controller.Tick += new EventHandler(ControllerSend);
        }

        private void RefreshComPort()
        {
            ports = SerialPort.GetPortNames();

            combobox_ports.Items.Clear();
            foreach (string com in ports)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = com;
                combobox_ports.Items.Add(item);
            }
            combobox_ports.SelectedIndex = 0;
        }

        private void Com_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] reDatas = new byte[comPort.BytesToRead];

            comPort.Read(reDatas, 0, reDatas.Length);
            SetTextCallback d = new SetTextCallback(SetText);
            this.Dispatcher.Invoke(d, new object[] { Encoding.Default.GetString(reDatas) });
        }

        delegate void SetTextCallback(string text);

        private void Button_open_Click(object sender, RoutedEventArgs e)
        {
            if (combobox_ports.SelectedValue == null)
            {
                MessageBox.Show("无可用串口，请检查硬件连接。");
                return;
            }

            if (serialPort_isOpen == false)
            {
                try
                {
                    comPort.PortName = combobox_ports.Text;
                    comPort.BaudRate = int.Parse(combobox_baudrate.Text);
                    comPort.Parity = (Parity)combobox_checkbit.SelectedIndex;
                    comPort.DataBits = int.Parse(combobox_databit.Text);
                    comPort.StopBits = (StopBits)double.Parse(combobox_stopbit.Text);
                    comPort.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    RefreshComPort();
                    return;
                }

                button_open.Content = "关闭串口";
                serialPort_isOpen = true;

                combobox_ports.IsEnabled = false;
                combobox_baudrate.IsEnabled = false;
                combobox_checkbit.IsEnabled = false;
                combobox_databit.IsEnabled = false;
                combobox_stopbit.IsEnabled = false;

                if (checkbox_control.IsChecked == true)
                {
                    controller.Interval = TimeSpan.FromMilliseconds(100);
                    controller.Start();
                    controller_isStarted = true;
                    button_state.Background = Brushes.GreenYellow;
                    button_state.Content = "ON";
                }
            }
            else
            {
                try
                {
                    controller.Stop();
                    controller_isStarted = false;
                    button_state.Background = Brushes.Pink;
                    button_state.Content = "OFF";

                    comPort.DiscardInBuffer();
                    comPort.DiscardOutBuffer();

                    comPort.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                button_open.Content = "打开串口";
                serialPort_isOpen = false;

                combobox_ports.IsEnabled = true;
                combobox_baudrate.IsEnabled = true;
                combobox_checkbit.IsEnabled = true;
                combobox_databit.IsEnabled = true;
                combobox_stopbit.IsEnabled = true;
            }
        }

        private void Checkbox_control_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort_isOpen == true)
            {
                if (controller_isStarted == false)
                {
                    controller.Interval = TimeSpan.FromMilliseconds(100);
                    controller.Start();
                    controller_isStarted = true;
                    button_state.Background = Brushes.GreenYellow;
                    button_state.Content = "ON";
                }
                else
                {
                    controller.Stop();
                    controller_isStarted = false;
                    button_state.Background = Brushes.Pink;
                    button_state.Content = "OFF";
                }
            }
        }

        private void SetText(string text)
        {
            textBox_receivedData.Text = text;
        }

        private void ControllerSend(object sender, EventArgs e)
        {
            string ctrlStr = "CTR";
            string speedStr;
            byte[] ctrlData;

            if ((keyA && !keyD) || (!keyA && keyD))
            {
                if (keyA)
                {
                    speedStr = "0500";
                    ctrlStr += "L";
                    ctrlStr += speedStr;
                }
                else if (keyD)
                {
                    speedStr = "0500";
                    ctrlStr += "R";
                    ctrlStr += speedStr;
                }
            }
            else
            {
                speedStr = "0000";
                ctrlStr += "L";
                ctrlStr += speedStr;
            }

            if ((keyW && keyS) || keyShift)
            {
                speedStr = "0000";
                ctrlStr += "S";
                ctrlStr += speedStr;
            }
            else if (keyW)
            {
                if (int.Parse(textbox_forwardSpeed.Text) > 10000 || int.Parse(textbox_forwardSpeed.Text) < 1000)
                {
                    textBox_receivedData.Text = "错误的前进速度值设置！";
                    return;
                }
                speedStr = textbox_forwardSpeed.Text;
                ctrlStr += "F";
                ctrlStr += speedStr;
            }
            else if (keyS)
            {
                if (int.Parse(textbox_backwardSpeed.Text) > 10000 || int.Parse(textbox_backwardSpeed.Text) < 1000)
                {
                    textBox_receivedData.Text = "错误的后退速度值设置！";
                    return;
                }
                speedStr = textbox_backwardSpeed.Text;
                ctrlStr += "B";
                ctrlStr += speedStr;
            }
            else if (keyW == false && keyS == false)
            {
                speedStr = "0000";
                ctrlStr += "F";
                ctrlStr += speedStr;
            }

            ctrlStr += "\r\n";

            ctrlData = Encoding.Default.GetBytes(ctrlStr);

            SendData(ctrlData);
        }

        private bool SendData(byte[] data)
        {
            if (comPort.IsOpen)
            {
                try
                {
                    comPort.Write(data, 0, data.Length);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("串口未打开！");
            }

            return false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W)
            {
                button_W.Background = Brushes.Pink;
                keyW = true;
            }
            else if (e.Key == Key.A)
            {
                button_A.Background = Brushes.Pink;
                keyA = true;
            }
            else if (e.Key == Key.S)
            {
                button_S.Background = Brushes.Pink;
                keyS = true;
            }
            else if (e.Key == Key.D)
            {
                button_D.Background = Brushes.Pink;
                keyD = true;
            }
            else if (e.Key == Key.RightShift)
            {
                button_Shift.Background = Brushes.Pink;
                keyShift = true;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W)
            {
                button_W.Background = Brushes.LightBlue;
                keyW = false;
            }
            else if (e.Key == Key.A)
            {
                button_A.Background = Brushes.LightBlue;
                keyA = false;
            }
            else if (e.Key == Key.S)
            {
                button_S.Background = Brushes.LightBlue;
                keyS = false;
            }
            else if (e.Key == Key.D)
            {
                button_D.Background = Brushes.LightBlue;
                keyD = false;
            }
            else if (e.Key == Key.RightShift)
            {
                button_Shift.Background = Brushes.LightBlue;
                keyShift = false;
            }
        }
    }
}
