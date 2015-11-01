﻿#if DNX451 || DOTNET5_4 || DNXCORE50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeComb.CI.Runner;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CIRunnerExtension
    {
        public static IServiceCollection AddCIRunner(this IServiceCollection self,int MaxThreads = 4, int MaxTimeLimit = 1000 * 60 * 20)
        {
            return self.AddInstance(new CIRunner(MaxThreads, MaxTimeLimit));
        }
    }
}
#endif