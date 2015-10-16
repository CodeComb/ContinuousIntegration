using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using CodeComb.Package;
using CodeComb.CI.Runner.EventArgs;

namespace CodeComb.CI.Runner
{
    public enum TaskStatus
    {
        Queued,
        Building,
        Successful,
        Failed
    }

    public class Task
    {
        private IRunProvider provider;
        public Task(IRunProvider provider, string WorkingDirectory)
        {
            this.provider = provider;
            Process = new Process();
            this.WorkingDirectory = WorkingDirectory;
            var fileName = "build.cmd";
            if (OS.Current != OSType.Windows)
            {
                fileName = "build.sh";
            }
            Process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WorkingDirectory = WorkingDirectory,
                UserName = provider.UserName,
                Password = provider.Password
            };
            foreach (var ev in provider.AdditionalEnvironmentVariables)
            {
#if DNXCORE50
                Process.StartInfo.Environment.Add(ev.Key, ev.Value);
#else
                Process.StartInfo.EnvironmentVariables.Add(ev.Key, ev.Value);
#endif
            }
        }
        public dynamic Identifier { get; set; }
        public string WorkingDirectory { get; set; }
        public Process Process { get; set; }
        public TaskStatus Status { get; set; }
        public string Output { get; private set; }
        public void Run()
        {
            Process.ErrorDataReceived += (sender, args) =>
            {
                if (OnOutputReceived != null)
                    OnOutputReceived(this, new OutputReceivedEventArgs { Output = args.Data });
                Output += args.Data;
            };
            Process.OutputDataReceived += (sender, args) =>
            {
                if (OnOutputReceived != null)
                    OnOutputReceived(this, new OutputReceivedEventArgs { Output = args.Data });
                Output += args.Data;
            };
            Process.Start();
            Process.WaitForExit(provider.MaxTimeLimit);
            if (Process.ExitCode == 0)
            {
                Status = TaskStatus.Successful;
                OnBuildSuccessful(this, new BuildSuccessfulArgs
                {
                    ExitCode = Process.ExitCode,
                    StartTime = Process.StartTime,
                    ExitTime = Process.ExitTime,
                    PeakMemoryUsage = Process.PeakWorkingSet64,
                    TimeUsage = Process.UserProcessorTime,
                    Output = Output
                });
            }
            else if (Process.ExitCode == -1 && Process.UserProcessorTime.TotalMilliseconds >= provider.MaxTimeLimit)
            {
                Status = TaskStatus.Failed;
                OnTimeLimitExceeded(this, new TimeLimitExceededArgs
                {
                    ExitCode = Process.ExitCode,
                    StartTime = Process.StartTime,
                    ExitTime = Process.ExitTime,
                    PeakMemoryUsage = Process.PeakWorkingSet64,
                    TimeUsage = Process.UserProcessorTime,
                    Output = Output
                });
            }
            else
            {
                Status = TaskStatus.Failed;
                OnBuiledFailed(this, new BuildFailedArgs
                {
                    ExitCode = Process.ExitCode,
                    StartTime = Process.StartTime,
                    ExitTime = Process.ExitTime,
                    PeakMemoryUsage = Process.PeakWorkingSet64,
                    TimeUsage = Process.UserProcessorTime,
                    Output = Output
                });
            }
            Clean();
        }

        public void Clean()
        {
            Directory.Delete(WorkingDirectory, true);
        }

        public delegate void OutputReceivedHandle(object sender, OutputReceivedEventArgs args); 
        public static event OutputReceivedHandle OnOutputReceived;
        public delegate void BuildSuccessfulHandle(object sender, BuildSuccessfulArgs args);
        public static event BuildSuccessfulHandle OnBuildSuccessful;
        public delegate void BuildFailedHandle(object sender, BuildFailedArgs args);
        public static event BuildFailedHandle OnBuiledFailed;
        public delegate void TimeLimitExceededHandle (object sender, TimeLimitExceededArgs args);
        public static event TimeLimitExceededHandle OnTimeLimitExceeded;
    }
}
