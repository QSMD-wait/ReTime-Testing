using System.Configuration;
using System.Data;
using System.Windows;
using ReTime_Testing.Views;

namespace ReTime_Testing
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 打开主窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // 打开 TimeTopDesktop 窗口
            var timeTopDesktop = new TimeTopDesktop();
            timeTopDesktop.Show();
        }
    }
}
