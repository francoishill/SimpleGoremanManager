using System;
using System.ComponentModel;
using System.Windows.Input;

namespace SimpleGoremanManager
{
    public sealed class CommandHandler : INotifyPropertyChanged, ICommand
    {
        private readonly Action<object> m_Action;
        private bool m_CanExecute;

        public CommandHandler(Action<object> action, bool initialCanExecute)
        {
            m_Action = action;
            m_CanExecute = initialCanExecute;
        }

        public bool CanExecuteProperty
        {
            get
            {
                return m_CanExecute;
            }
            set
            {
                if (m_CanExecute == value)
                    return;

                m_CanExecute = value;
                if (CanExecuteChanged != null)
                    CanExecuteChanged(this, new EventArgs());
                OnPropertyChanged("CanExecuteProperty");
            }
        }

        public bool CanExecute(object parameter)
        {
            return m_CanExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            m_Action(parameter);
            /*try
            {
                m_Action(parameter);
            }
            catch (Exception exc)
            {
                ShowUserMessagePopup.ShowException(exc, "Cannot excute command.");
            }*/
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
