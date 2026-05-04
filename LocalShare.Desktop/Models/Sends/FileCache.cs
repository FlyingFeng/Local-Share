using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace LocalShare.Desktop.Models.Sends
{
    public partial class FileCache : ObservableObject
    {
        [ObservableProperty]
        private string fileName = string.Empty;
        [ObservableProperty]
        private string filePath = string.Empty;
        [ObservableProperty]
        private bool isSelected;
        [ObservableProperty]
        private long fileSize;
        [ObservableProperty]
        private string md5 = string.Empty;
        [ObservableProperty]
        private bool isOpenFromDir;

        [RelayCommand]
        private void ShowDownloadLink()
        {
            var id = Guid.NewGuid().ToString();
            DownloadLinkHolder.AddDownloadLink(id, FilePath);
            ShowDownloadLinkWindow window = new ShowDownloadLinkWindow();
            window.Id = id;
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }


    }
}
