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
    /// <summary>
    /// ShowDownloadLinkWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShowDownloadLinkWindow : Window
    {
        public ShowDownloadLinkWindow()
        {
            InitializeComponent();
        }

        public string Id { get; set; } = string.Empty;


        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(TxtLink.Text);
                HandyControl.Controls.MessageBox.Show("复制到剪切板成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                HandyControl.Controls.MessageBox.Show("复制到剪切板失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var url = $"http://{GlobalShared.IpAddress}:{GlobalShared.HttpPort}/api/local/download/{Id}";
            TxtLink.Text = url;
        }
    }
}
