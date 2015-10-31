using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeComb.CI.Runner;

namespace Microsoft.Extensions.DependencyInjection
{
#if DNX451 || DOTNET5_4 || DNXCORE50
    public static class DefaultRunnerExtension
    {
        public static IServiceCollection AddDefaultCIRunner(this IServiceCollection self,int MaxThreads = 4, int MaxTimeLimit = 1000 * 60 * 20)
        {
            return self.AddSingleton<ICIRunner>(x => new DefaultCIRunner(MaxThreads, MaxTimeLimit));
        }
    }
#endif
}
