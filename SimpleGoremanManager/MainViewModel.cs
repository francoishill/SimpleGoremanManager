using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Threading;

namespace SimpleGoremanManager
{
    public class MainViewModel : BaseViewModel
    {
        private readonly WeakReference<IMainViewModelObserver> m_ObserverReference = new WeakReference<IMainViewModelObserver>(null);

        private readonly string m_ProcfilePath;

        private readonly ObservableCollection<ProcfileProcessViewModel> m_ProcfileProcesses = new ObservableCollection<ProcfileProcessViewModel>();
        private string m_LogText = "";

        private ICommand m_StartCommand;
        private ICommand m_StopCommand;

        public MainViewModel(string procfilePath)
        {
            m_ProcfilePath = procfilePath;
        }

        public void Init()
        {
            m_StartCommand = new CommandHandler(ExecuteStart, true);
            m_StopCommand = new CommandHandler(ExecuteStop, true);

            LoadProcfile();
            StartRefreshingStatusses();
        }

        public void AttachObserver(IMainViewModelObserver observer)
        {
            m_ObserverReference.SetTarget(observer);
        }

        private void LoadProcfile()
        {
            try
            {
                m_ProcfileProcesses.Clear();

                var processNames = File.ReadAllLines(m_ProcfilePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains(":"))
                    .Select(line => line.Substring(0, line.IndexOf(":")).Trim())
                    .Where(line => !line.StartsWith("#"))
                    .ToArray();

                var procfileProcesses = processNames.Select(pn => new ProcfileProcessViewModel(pn)).ToArray();

                foreach (var procfileProcess in procfileProcesses)
                {
                    m_ProcfileProcesses.Add(procfileProcess);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Exception loading Procfile. Will now exit.\n\nException:\n{exception.Message}.\n\nStack:\n{exception.StackTrace}");

                IMainViewModelObserver observer;
                if (m_ObserverReference.TryGetTarget(out observer))
                {
                    observer.OnMustClose();
                }
            }
        }

        private ProcfileProcessViewModel[] GetSelectedProcfileProcessesFromCommandParam(object param)
        {
            var selectedItemCollection = param as ObservableCollection<object>;

            return selectedItemCollection?.Select(i => (ProcfileProcessViewModel)i).ToArray();
        }

        private void ExecuteStart(object param)
        {
            var selectedProcs = GetSelectedProcfileProcessesFromCommandParam(param);
            RunGoremanCommandOnAllSelectedProcesses(selectedProcs, process => $"run start \"{process.ProcessName}\"");
        }

        private void ExecuteStop(object param)
        {
            var selectedProcs = GetSelectedProcfileProcessesFromCommandParam(param);
            RunGoremanCommandOnAllSelectedProcesses(selectedProcs, process => $"run stop \"{process.ProcessName}\"");
        }

        public string LogText
        {
            get
            {
                return m_LogText;
            }
            set
            {
                if (m_LogText == value) return;
                m_LogText = value;
                OnPropertyChanged("LogText");
            }
        }

        public ICollection<ProcfileProcessViewModel> ProcfileProcesses => m_ProcfileProcesses;

        public ICommand StartCommand => m_StartCommand;
        public ICommand StopCommand => m_StopCommand;

        private async void StartRefreshingStatusses()
        {
            while (true)
            {
                var resultProcessStatusses = await Task.Run(() =>
                {
                    var goremanArgs = "run status";

                    var outputLines = new List<string>();
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo("goreman", goremanArgs);

                        process.StartInfo.RedirectStandardError = process.StartInfo.RedirectStandardInput = process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;

                        process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
                        {
                            if (string.IsNullOrWhiteSpace(e.Data))
                            {
                                return;
                            }
                            outputLines.Add(e.Data);
                        };
                        process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
                        {
                            if (string.IsNullOrWhiteSpace(e.Data))
                            {
                                return;
                            }
                            AppendLog(true, e.Data);
                        };

                        process.Start();

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();

                        var exitCode = process.ExitCode;
                        if (exitCode != 0)
                        {
                            AppendLog(true, $"Exit code (args: {goremanArgs}) was: {exitCode}");
                        }
                    }

                    return outputLines;
                });

                foreach (var procfileProcess in m_ProcfileProcesses)
                {
                    procfileProcess.IsRunning = resultProcessStatusses.Contains("*" + procfileProcess.ProcessName);
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        private void RunGoremanCommandOnAllSelectedProcesses(IEnumerable<ProcfileProcessViewModel> selectedProcesses, Func<ProcfileProcessViewModel, string> getArgsFromProcess)
        {
            foreach (var process in selectedProcesses)
            {
                var args = getArgsFromProcess(process);
                var processRunner = new ProcessRunner(this, process.ProcessName, true);

                var exitCode = processRunner.RunProcess(new ProcessStartInfo("goreman", args));
                if (exitCode != 0)
                {
                    AppendLog(true, $"Exit code (args: {args}) was: {exitCode}");
                }
            }
        }

        private void AppendLog(bool isError, string line)
        {
            var finalLine = line;
            if (isError)
            {
                finalLine = "[[--ERROR--]] " + finalLine;
            }
            var timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                LogText += $"[{timeStamp}] {finalLine}\n";
            });
        }

        private class ProcessRunner
        {
            private readonly MainViewModel m_ViewModel;
            private readonly string m_DisplayName;
            private readonly bool m_AllowAppendLog;

            public ProcessRunner(MainViewModel viewModel, string displayName, bool allowAppendLog)
            {
                m_ViewModel = viewModel;
                m_DisplayName = displayName;
                m_AllowAppendLog = allowAppendLog;
            }

            public int RunProcess(ProcessStartInfo startInfo)
            {
                using (var process = new Process())
                {
                    process.StartInfo = startInfo;

                    startInfo.RedirectStandardError = startInfo.RedirectStandardInput = startInfo.RedirectStandardOutput = true;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;

                    process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
                    {
                        if (string.IsNullOrWhiteSpace(e.Data))
                        {
                            return;
                        }
                        AppendLog(false, e.Data);
                    };
                    process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
                    {
                        if (string.IsNullOrWhiteSpace(e.Data))
                        {
                            return;
                        }
                        AppendLog(true, e.Data);
                    };

                    AppendLog(false, "Starting process");
                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    AppendLog(false, $"Process finished (exit code {process.ExitCode})");
                    return process.ExitCode;
                }
            }

            private void AppendLog(bool isError, string line)
            {
                if (!m_AllowAppendLog)
                {
                    return;
                }
                m_ViewModel.AppendLog(isError, $"({m_DisplayName}) {line}");
            }
        }
    }
}
