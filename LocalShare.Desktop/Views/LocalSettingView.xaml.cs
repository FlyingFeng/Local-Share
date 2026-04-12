using LocalShare.Desktop.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalShare.Desktop.Views
{
    /// <summary>
    /// LocalSettingView.xaml 的交互逻辑
    /// </summary>
    public partial class LocalSettingView : UserControl
    {
        public LocalSettingView(LocalSettingViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is IClosable closable)
                closable.Close();
        }
    }
}
