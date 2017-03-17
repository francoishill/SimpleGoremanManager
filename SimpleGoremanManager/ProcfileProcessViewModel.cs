namespace SimpleGoremanManager
{
    public class ProcfileProcessViewModel : BaseViewModel
    {
        private string m_ProcessName;
        private bool m_IsRunning;

        public ProcfileProcessViewModel(string processName)
        {
            m_ProcessName = processName;
        }

        public string ProcessName
        {
            get
            {
                return m_ProcessName;
            }
            set
            {
                if (m_ProcessName == value) return;
                m_ProcessName = value;
                OnPropertyChanged("ProcessName");
            }
        }

        public bool IsRunning
        {
            get
            {
                return m_IsRunning;
            }
            set
            {
                if (m_IsRunning == value) return;
                m_IsRunning = value;
                OnPropertyChanged("IsRunning");
            }
        }
    }
}
