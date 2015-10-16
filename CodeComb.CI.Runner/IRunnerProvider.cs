using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;

namespace CodeComb.CI.Runner
{
    public interface IRunnerProvider
    {
        int MaxTimeLimit { get; set; }
        string Username { get; set; }
        SecureString Password { get; set; }
        Queue<Task> TaskQueue { get; set; }
        int MaxThreads { get; set; }
        IDictionary<string, string> AdditionalEnvironmentVariables { get; set; }
        void PushTask();
        void Polling();
    }
}
