﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
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
        private ICIRunner provider;
        private string FindDirectory(string path)
        {
            string[] files;
            if (OS.Current == OSType.Windows)
                files = Directory.GetFiles(path, "build.cmd", SearchOption.AllDirectories);
            else
                files = Directory.GetFiles(path, "build.sh", SearchOption.AllDirectories);
            if (files.Count() == 0)
                throw new FileNotFoundException();
            return Path.GetDirectoryName(files.First());
        }

        public Task(ICIRunner provider, string workingDirectory)
        {
            this.provider = provider;
            Process = new Process();
            workingDirectory = FindDirectory(workingDirectory);
            this.WorkingDirectory = workingDirectory;
            var fileName = "cmd.exe";
            if (OS.Current != OSType.Windows)
            {
                fileName = "bash";
            }
            var arguments = "/c \"build.cmd\"";
            if (OS.Current != OSType.Windows)
            {
                arguments = "-c \"./build.sh\"";
            }

            Process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory
            };

            if (provider.AdditionalEnvironmentVariables == null)
                provider.AdditionalEnvironmentVariables = new Dictionary<string, string>();
                    
            foreach (var ev in provider.AdditionalEnvironmentVariables)
            {
#if DNXCORE50 || DOTNET5_4
                if (Process.StartInfo.Environment[ev.Key] != null)
                    Process.StartInfo.Environment[ev.Key] = Process.StartInfo.Environment[ev.Key].TrimEnd(' ').TrimEnd(';') + ";" + ev.Value;
                else
                    Process.StartInfo.Environment.Add(ev.Key, ev.Value);
#else
                if (Process.StartInfo.EnvironmentVariables[ev.Key] != null)
                    Process.StartInfo.EnvironmentVariables[ev.Key] = Process.StartInfo.EnvironmentVariables[ev.Key].TrimEnd(' ').TrimEnd(';') + ";" + ev.Value;
                else
                    Process.StartInfo.EnvironmentVariables.Add(ev.Key, ev.Value);
#endif
                Process.ErrorDataReceived += (sender, args) =>
                {
                    if (OnOutputReceived != null)
                        OnOutputReceived(this, new OutputReceivedEventArgs { Output = args.Data + "\r\n" });
                    Output += args.Data + "\r\n";
                };
                Process.OutputDataReceived += (sender, args) =>
                {
                    if (OnOutputReceived != null)
                        OnOutputReceived(this, new OutputReceivedEventArgs { Output = args.Data + "\r\n" });
                    Output += args.Data + "\r\n";
                };
            }
        }
        public dynamic Identifier { get; set; }
        public string WorkingDirectory { get; set; }
        public Process Process { get; set; }
        public TaskStatus Status { get; set; }
        public string Output { get; private set; }

        public void Run()
        {
            Process.Start();
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();
            var flag = Process.WaitForExit(provider.MaxTimeLimit);
            if (!flag)
                Process.Kill();
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
            else if (!flag)
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
                    PeakMemoryUsage = 0,
                    TimeUsage = Process.UserProcessorTime,
                    Output = Output
                });
            }
            Clean();
            provider.CurrentThreads = provider.CurrentThreads - 1;
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
