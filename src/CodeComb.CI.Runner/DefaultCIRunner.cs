using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace CodeComb.CI.Runner
{
    public class DefaultCIRunner : ICIRunner
    {
        public DefaultCIRunner(int MaxThreads = 4, int MaxTimeLimit = 1000 * 60 * 20)
        {
            this.MaxThreads = MaxThreads;
            this.MaxTimeLimit = MaxTimeLimit;
        }

        public IDictionary<string, string> AdditionalEnvironmentVariables { get; set; }

        public int MaxThreads { get; set; }

        public int MaxTimeLimit { get; set; }

        public SecureString Password { get; set; }

        public string UserName { get; set; }

        public static int _CurrentThreads { get; set; } = 0;

        public int CurrentThreads
        {
            get { return _CurrentThreads; }
            set { _CurrentThreads = value; }
        }

        private Timer Timer { get; set; }

        public void PushTask(string Path, dynamic Identifier = null)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                while (CurrentThreads >= MaxThreads)
                {
                    Thread.Sleep(500);
                }
                _CurrentThreads++;
                var task = new Task(this, Path) { Identifier = Identifier };
                task.Run();
            });
        }
    }
}
