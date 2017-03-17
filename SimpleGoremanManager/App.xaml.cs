using System.Windows;
using GalaSoft.MvvmLight.Threading;

namespace SimpleGoremanManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherHelper.Initialize();
        }
    }
}
