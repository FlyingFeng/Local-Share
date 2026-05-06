using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using LocalShare.Desktop.Models;
using LocalShare.Desktop.Models.Sends;
using LocalShare.Protocol.Define;
using Serilog;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace LocalShare.Desktop
{

    public class MessageInfo
    {
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// 0:send
        /// 1:received
        /// </summary>
        public int Type { get; set; }

        public string Message { get; set; } = string.Empty;

        public long Time { get; set; }

    }

    /// <summary>
    /// ChatWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ChatWindow : Window, IRecipient<MessageModel>
    {
        public ChatWindow()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.Register<MessageModel>(this);
        }

        public Channel? NodeChannel { get; set; }
        public LocalNode? Node { get; set; }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSendMessage.Text))
            {
                return;
            }
            try
            {
                await SendMessage();
            }
            catch (Exception ex)
            {
                Log.Error($"Send chat message error, {ex.Message}\n{ex.StackTrace}");
                HandyControl.Controls.MessageBox.Show("发送失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task SendMessage()
        {
            if (NodeChannel != null && Node != null && !string.IsNullOrWhiteSpace(TxtSendMessage.Text))
            {
                if (Node.State == 1)
                {
                    HandyControl.Controls.MessageBox.Show($"节点已经离线", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }


                LocalShareService.LocalShareServiceClient client = new LocalShareService.LocalShareServiceClient(NodeChannel);
                var request = new ChatRequest
                {
                    ChatId = Guid.NewGuid().ToString(),
                    Message = TxtSendMessage.Text,
                    Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    SendNodeIp = GlobalShared.IpAddress,
                    SendNodeName = GlobalShared.NodeName,
                    ReceiveNodeIp = Node.IpAddress,
                    ReceiveNodeName = Node.NodeName
                };
                await client.ChatAsync(request);
                ChatMessageHolder.AddChatHistoryData(Node.NodeName, new MessageInfo
                {
                    Type = 0,
                    ChatId = request.ChatId,
                    Message = request.Message,
                    Time = request.Time
                });

                TextBox t = new TextBox();
                t.IsReadOnly = true;
                t.Background = new SolidColorBrush(Color.FromRgb(0x03, 0xde, 0x6d));
                t.BorderThickness = new Thickness(0);
                t.TextWrapping = TextWrapping.Wrap;
                t.Text = TxtSendMessage.Text;
                ListBoxItem item = new ListBoxItem();
                LbChatMessage.Items.Add(item);
                item.HorizontalAlignment = HorizontalAlignment.Right;
                item.Content = t;
                TxtSendMessage.Text = string.Empty;
            }
        }


        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void Receive(MessageModel message)
        {
            try
            {
                if (message.MessageType == MessageType.ChatMessageArrived &&
                    message.Data is MessageInfo msg)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TextBox t = new TextBox();
                        t.IsReadOnly = true;
                        t.Background = new SolidColorBrush(Color.FromRgb(0xe0, 0xe0, 0xe0));
                        t.BorderThickness = new Thickness(0);
                        t.TextWrapping = TextWrapping.Wrap;
                        t.Text = msg.Message;
                        ListBoxItem item = new ListBoxItem();
                        LbChatMessage.Items.Add(item);
                        item.HorizontalAlignment = HorizontalAlignment.Left;
                        item.Content = t;
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Receive chat message error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Node != null)
                {
                    var list = ChatMessageHolder.GetChatHistoryData(Node.NodeName);
                    foreach (var msg in list)
                    {
                        TextBox t = new TextBox();
                        t.IsReadOnly = true;
                        t.BorderThickness = new Thickness(0);
                        t.Text = msg.Message;
                        t.TextWrapping = TextWrapping.Wrap;
                        ListBoxItem item = new ListBoxItem();
                        LbChatMessage.Items.Add(item);
                        item.Content = t;
                        if (msg.Type == 1)
                        {
                            t.Background = new SolidColorBrush(Color.FromRgb(0xe0, 0xe0, 0xe0));
                            item.HorizontalAlignment = HorizontalAlignment.Left;
                        }
                        else if (msg.Type == 0)
                        {
                            t.Background = new SolidColorBrush(Color.FromRgb(0x03, 0xde, 0x6d));
                            item.HorizontalAlignment = HorizontalAlignment.Right;
                        }
                    }
                    TxtSendMessage.Focus();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Load chat window error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WeakReferenceMessenger.Default.Unregister<MessageModel>(this);
        }

        private async void TxtSendMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    await SendMessage();
                }
                catch(Exception ex)
                {
                    HandyControl.Controls.MessageBox.Show("发送失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Send chat message error(key down), {ex.Message}\n{ex.StackTrace}");
                }
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
