using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETCOREAPP1_0
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyModel;
#endif
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using TestMethodDisplay = Xunit.Sdk.TestMethodDisplay;

namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void TestTest()
        {
           // var env = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");

           // Assert.NotNull(env);
           // Assert.NotEqual("", env);

            var path = typeof(UnitTest1).GetTypeInfo().Assembly.Location;

#if NETCOREAPP1_0
            var reporters = GetAvailableRunnerReporters(new[] { path });

            Assert.NotEmpty(reporters);
#endif

            // var reporter = reporters.FirstOrDefault(r => r.IsEnvironmentallyEnabled);

            //Assert.NotNull(reporter);


            //Assert.Equal("appveyor", reporter.RunnerSwitch);

            //var sink = reporter.CreateMessageHandler(new RunLogger());

            //sink.OnMessage(new TestAssemblyStarting(Enumerable.Empty<ITestCase>(), new TestAssembly(new ReflectionAssemblyInfo(typeof(UnitTest1).GetTypeInfo().Assembly)),
            //    DateTime.UtcNow, "env", "name"));


            //sink.OnMessage(new Xunit.TestStarting(new XunitTest(new XunitTestCase(null, TestMethodDisplay.ClassAndMethod, new TestMethod(new TestClass(new TestCollection()))))))
        }


        [Fact(Skip="skipped")]
        public void TestAsyncMethod()
        {
            var dll = "XUnitTestProject1.dll";
            var helper = new DiaSessionWrapperHelper(dll);

            var method = "Test2";
            var type = "XUnitTestProject1.UnitTest1";
            var path = "";

            helper.Normalize(ref type, ref method, ref path);

            Assert.NotEqual("", path);

            var session = new DiaSession(dll);

            var data = session.GetNavigationData(type, method, path);

            Assert.NotNull(data?.FileName);
        }

        [Fact]
        public void TestSyncMethod()
        {

            var dll = "XUnitTestProject1.dll";
            var helper = new DiaSessionWrapperHelper(dll);

            var method = "TestAsyncMethod";
            var type = "XUnitTestProject1.UnitTest1";
            var path = "";

            helper.Normalize(ref type, ref method, ref path);



            Assert.NotEqual("", path);

            var session = new DiaSession(dll);

            var data = session.GetNavigationData(type, method, path);

            Assert.NotNull(data?.FileName);
        }

        [Fact]
        public async Task Test2()
        {
            await Task.CompletedTask;
        }

        [Fact(Skip = "skipped")]
        public async void Test311()
        {
            await Task.CompletedTask;

           // await Task.Delay(10000);
        }

#if NETCOREAPP1_0
        static List<object> GetAvailableRunnerReporters(IEnumerable<string> sources)
        {
            // Combine all input libs and merge their contexts to find the potential reporters
            var result = new List<object>();
            var dcjr = new DependencyContextJsonReader();
            var deps = sources
                        .Select(Path.GetFullPath)
                        .Select(s => s.Replace(".dll", ".deps.json"))
                        .Where(File.Exists)
                        .Select(f => new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(f))))
                        .Select(dcjr.Read);
            var ctx = deps.Aggregate(DependencyContext.Default, (context, dependencyContext) => context.Merge(dependencyContext));
            dcjr.Dispose();

            var depsAssms = ctx.GetRuntimeAssemblyNames(RuntimeEnvironment.GetRuntimeIdentifier())
                               .ToList();

            // Make sure to also check assemblies within the directory of the sources
            var dllsInSources = sources
                        .Select(Path.GetFullPath)
                        .Select(Path.GetDirectoryName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .SelectMany(p => Directory.GetFiles(p, "*.dll").Select(f => Path.Combine(p, f)))
                        .Select(f => new AssemblyName { Name = Path.GetFileNameWithoutExtension(f) })
                        .ToList();

            foreach (var assemblyName in depsAssms.Concat(dllsInSources))
           // foreach (var assemblyName in dllsInSources)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    foreach (var type in assembly.DefinedTypes)
                    {
                        //#pragma warning disable CS0618
                        //                        if (type == null || type.IsAbstract || type == typeof(DefaultRunnerReporter).GetTypeInfo() || type == typeof(DefaultRunnerReporterWithTypes).GetTypeInfo() || type.ImplementedInterfaces.All(i => i != typeof(IRunnerReporter)))
                        //                            continue;
                        //#pragma warning restore CS0618

                        //                        var ctor = type.DeclaredConstructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                        //                        if (ctor == null)
                        //                        {
                        //                            Console.ForegroundColor = ConsoleColor.Yellow;
                        //                            Console.WriteLine($"Type {type.FullName} in assembly {assembly} appears to be a runner reporter, but does not have an empty constructor.");
                        //                            Console.ResetColor();
                        //                            continue;
                        //                        }

                        //result.Add((IRunnerReporter)ctor.Invoke(new object[0]));
                    }
                    if (assembly.FullName.Contains("reporters"))
                    {
                        result.Add(assembly);
                    }
                }
                catch
                {
                    continue;
                }
            }

            return result;
        }
#endif

    }

    public class MyFactAttribute : FactAttribute
    {
        public override string DisplayName
        {
            get
            {
             //   Debugger.Launch();
                return base.DisplayName;
            }
            set
            {
                
            }
        }
    }



    class DiaSessionWrapperHelper 
    {
        readonly Assembly assembly;
        readonly Dictionary<string, Type> typeNameMap;

        public DiaSessionWrapperHelper(string assemblyFileName)
        {
            try
            {
                assembly = Assembly.Load(new AssemblyName { Name = Path.GetFileNameWithoutExtension(assemblyFileName) });
            }
            catch { }

            if (assembly != null)
            {
                Type[] types = null;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch { }  // Ignore anything other than ReflectionTypeLoadException

                if (types != null)
                    typeNameMap = types.Where(t => t != null && !string.IsNullOrEmpty(t.FullName))
                                       .ToDictionary(k => k.FullName);
                else
                    typeNameMap = new Dictionary<string, Type>();
            }
        }


        public void Normalize(ref string typeName, ref string methodName, ref string assemblyPath)
        {
            try
            {
                if (assembly == null)
                    return;

                Type type;
                if (typeNameMap.TryGetValue(typeName, out type) && type != null)
                {
                    MethodInfo method = type.GetMethod(methodName);
                    if (method != null)
                    {
                        // DiaSession only ever wants you to ask for the declaring type
                        typeName = method.DeclaringType.FullName;
                        assemblyPath = method.DeclaringType.GetTypeInfo().Assembly.Location;

                        var stateMachineType = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;

                        if (stateMachineType != null)
                        {
                            typeName = stateMachineType.FullName;
                            methodName = "MoveNext";
                        }
                    }
                }
            }
            catch { }
        }
    }
    class DiaSession : IDisposable
    {
        static readonly MethodInfo methodGetNavigationData;
        static readonly PropertyInfo propertyFileName;
        static readonly PropertyInfo propertyMinLineNumber;
        static readonly Type typeDiaSession;
        static readonly Type typeDiaNavigationData;

        public readonly string AssemblyFileName;
        bool sessionHasErrors;
        readonly Dictionary<string, IDisposable> wrappedSessions;

        static DiaSession()
        {
            typeDiaSession = Type.GetType("Microsoft.VisualStudio.TestPlatform.ObjectModel.DiaSession, Microsoft.VisualStudio.TestPlatform.ObjectModel", false);
            typeDiaNavigationData = Type.GetType("Microsoft.VisualStudio.TestPlatform.ObjectModel.DiaNavigationData, Microsoft.VisualStudio.TestPlatform.ObjectModel", false);

            if (typeDiaSession != null && typeDiaNavigationData != null)
            {
                methodGetNavigationData = typeDiaSession.GetMethod("GetNavigationData", new[] { typeof(string), typeof(string) });
                propertyFileName = typeDiaNavigationData.GetProperty("FileName");
                propertyMinLineNumber = typeDiaNavigationData.GetProperty("MinLineNumber");
            }
        }

        public DiaSession(string assemblyFileName)
        {
            this.AssemblyFileName = assemblyFileName;
            sessionHasErrors |= (typeDiaSession == null || Environment.GetEnvironmentVariable("XUNIT_SKIP_DIA") != null);
            wrappedSessions = new Dictionary<string, IDisposable>();
        }

        public void Dispose()
        {
            foreach (var wrappedSession in wrappedSessions.Values)
                wrappedSession.Dispose();
        }

        public DiaNavigationData GetNavigationData(string typeName, string methodName, string owningAssemblyFilename)
        {
            if (!sessionHasErrors)
                try
                {
                    if (!wrappedSessions.ContainsKey(owningAssemblyFilename))
                        wrappedSessions[owningAssemblyFilename] = (IDisposable)Activator.CreateInstance(typeDiaSession, owningAssemblyFilename);

                    var data = methodGetNavigationData.Invoke(wrappedSessions[owningAssemblyFilename], new[] { typeName, methodName });
                    if (data == null)
                        return null;

                    var noIndex = new object[0];
                    return new DiaNavigationData
                    {
                        FileName = (string)propertyFileName.GetValue(data, noIndex),
                        LineNumber = (int)propertyMinLineNumber.GetValue(data, noIndex)
                    };
                }
                catch
                {
                    sessionHasErrors = true;
                }

            return null;
        }
    }

    class DiaNavigationData
    {
        public string FileName { get; set; }
        public int LineNumber { get; set; }
    }

    //class RunLogger : IRunnerLogger
    //{
    //    public RunLogger()
    //    {
    //        LockObject = new object();
    //    }
    //    public void LogMessage(StackFrameInfo stackFrame, string message)
    //    {
    //    }

    //    public void LogImportantMessage(StackFrameInfo stackFrame, string message)
    //    {
    //    }

    //    public void LogWarning(StackFrameInfo stackFrame, string message)
    //    {
    //    }

    //    public void LogError(StackFrameInfo stackFrame, string message)
    //    {
    //    }

    //    public object LockObject { get; }
    //}
}
