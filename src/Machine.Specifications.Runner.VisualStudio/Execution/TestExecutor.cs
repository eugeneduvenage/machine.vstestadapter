﻿using Machine.Specifications.Runner;
using Machine.Specifications.Runner.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Machine.Specifications;
using Machine.VSTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Machine.VSTestAdapter.Execution
{
    public class TestExecutor
#if !NETSTANDARD
        : MarshalByRefObject
#endif
    {
#if !NETSTANDARD
        [System.Security.SecurityCritical]
        public override object InitializeLifetimeService()
        {
            return null;
        }
#endif

        private DefaultRunner CreateRunner(Assembly assembly, ISpecificationRunListener specificationRunListener)
        {
            var listener = new AggregateRunListener(new[] {
                specificationRunListener,
                new AssemblyLocationAwareRunListener(new[] { assembly })
            });

            return new DefaultRunner(listener, RunOptions.Default);
        }

        public void RunTestsInAssembly(string pathToAssembly, IEnumerable<VisualStudioTestIdentifier> specsToRun, ISpecificationRunListener specificationRunListener, IFrameworkHandle frameworkHandle)
        {
            DefaultRunner mspecRunner = null;
            Assembly assemblyToRun = null;

            try
            {
                assemblyToRun = AssemblyHelper.Load(pathToAssembly);
                mspecRunner = CreateRunner(assemblyToRun, specificationRunListener);

                var specsByContext = specsToRun.GroupBy(x => x.ContainerTypeFullName);

                mspecRunner.StartRun(assemblyToRun);

                foreach (var specs in specsByContext)
                {
                    var fields = specs.Select(x => x.FieldName);

                    mspecRunner.RunType(assemblyToRun, assemblyToRun.GetType(specs.Key), fields.ToArray());
                }
            }
            catch (Exception e)
            {
                specificationRunListener.OnFatalError(new ExceptionResult(e));
            }
            finally
            {
                try
                {
                    if (mspecRunner != null && assemblyToRun != null)
                        mspecRunner.EndRun(assemblyToRun);
                }
                catch (Exception exception)
                {
                    try
                    {
                        frameworkHandle?.SendMessage(TestMessageLevel.Error, "Machine Specifications Visual Studio Test Adapter - Error Ending Test Run." + Environment.NewLine + exception);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
    }
}
