using Grpc.Core;
using LocalShare.Desktop.KeepStates;
using LocalShare.Protocol.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LocalShare.Desktop
{
    /// <summary>
    /// AddLocalNodeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddLocalNodeWindow : Window
    {
        private readonly RpcChannelHolder? _channelHolder;
        public AddLocalNodeWindow(RpcChannelHolder? channelHolder)
        {
            InitializeComponent();
            _channelHolder = channelHolder;
        }

        public NodeModel? NodeInfo { get; set; }

        private async void BtnAddNode_Click(object sender, RoutedEventArgs e)
        {
            var ipAddress = TxtIpAddress.Text.Trim();
            var port = TxtPort.Text.Trim();
            if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(port))
            {
                HandyControl.Controls.MessageBox.Show("IP地址和端口不能为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!int.TryParse(port, out var portNum) || portNum < 1 || portNum > 65535)
            {
                HandyControl.Controls.MessageBox.Show("不是合法的端口号", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var reg = @"^((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])$";
            if (!Regex.IsMatch(ipAddress, reg))
            {
                HandyControl.Controls.MessageBox.Show("不是合法的IP地址", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (ipAddress == GlobalShared.IpAddress && portNum == GlobalShared.ServerPort)
            {
                HandyControl.Controls.MessageBox.Show("不能添加自己作为节点", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Channel? channel = null;
            try
            {
                BtnAddNode.IsEnabled = false;
                channel = await _channelHolder!.AddAndCloseOldChannel(ipAddress, int.Parse(port));
                await channel.ConnectAsync(DateTime.UtcNow.AddSeconds(5));
                LocalShareService.LocalShareServiceClient client = new LocalShareService.LocalShareServiceClient(channel);
                var nodeInfo = await client.GetServerNodeInfoAsync(new EmptyMessage());
                NodeInfo = nodeInfo;
                DialogResult = true;
                await channel.ShutdownAsync();
                Close();
            }
            catch (Exception ex)
            {
                DialogResult = false;
                HandyControl.Controls.MessageBox.Show($"添加节点失败，{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnAddNode.IsEnabled = true;
            }

        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtIpAddress.Text = GlobalShared.IpAddress;
            TxtPort.Text = GlobalShared.ServerPort.ToString();
        }
    }
}
