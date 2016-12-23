using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [Fact]
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

            var method = "Test1";
            var type = "XUnitTestProject1.UnitTest1";
            var path = "";

            helper.Normalize(ref type, ref method, ref path);



            Assert.NotEqual("", path);

            var session = new DiaSession(dll);

            var data = session.GetNavigationData(type, method, path);

            Assert.NotNull(data?.FileName);
        }


        [MyFact]
        public async Task Test2()
        {
            await Task.CompletedTask;
        }

        [MyFact]
        public async void Test311()
        {
            await Task.CompletedTask;
        }

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
}
