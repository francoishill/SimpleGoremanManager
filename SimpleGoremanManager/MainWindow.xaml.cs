using System;
using System.Windows;

namespace SimpleGoremanManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel m_ViewModel;
        private readonly IMainViewModelObserver m_ViewModelObserver;

        public MainWindow()
        {
            InitializeComponent();

            var cliArgs = Environment.GetCommandLineArgs();
            if (cliArgs.Length < 2)
            {
                throw new Exception("The first commandline argument must be a path to the Procfile");
            }

            var procfilePath = cliArgs[1];
            Title = $"{Title} | {procfilePath}";

            m_ViewModel = new MainViewModel(procfilePath);
            m_ViewModel.Init();
            DataContext = m_ViewModel;

            m_ViewModelObserver = new MainViewModelObserver(this);
            m_ViewModel.AttachObserver(m_ViewModelObserver);
        }

        public class MainViewModelObserver : IMainViewModelObserver
        {
            private readonly MainWindow m_MainWindow;

            public MainViewModelObserver(MainWindow mainWindow)
            {
                m_MainWindow = mainWindow;
            }

            public void OnMustClose()
            {
                m_MainWindow.Close();
            }
        }
    }
}
