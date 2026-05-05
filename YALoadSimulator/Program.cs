using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using CustomThreadPool;
using TestFramework;

namespace LoadSimulator
{
    class Program
    {
        private static int _completedTests = 0;
        private static readonly object _statsLock = new object();
        private static int _failedTests = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Parameterized tests, filtering, events, expression assert");
            var assembly = Assembly.LoadFrom("Tests.dll");

            var allTestInfos = GetAllTestCases(assembly);
            Console.WriteLine($"Total test cases generated: {allTestInfos.Count}");

            var filter = new Func<TestInfo, bool>(t =>
                t.Categories.Contains("Math") && t.Priority <= 2
            );
            Console.WriteLine("\nFilter applied: Categories contains 'Math' AND Priority <= 2");
            var filteredTests = allTestInfos.Where(filter).ToList();
            Console.WriteLine($"Tests after filter: {filteredTests.Count}");

            int tasksToRun = filteredTests.Count;
            if (tasksToRun < 10) tasksToRun = 10;
            var tasks = new List<Action>();
            for (int i = 0; i < tasksToRun; i++)
            {
                var test = filteredTests[i % filteredTests.Count];
                tasks.Add(() => ExecuteTest(test));
            }

            using (var pool = new DynamicThreadPool(1, 4, 2, 1))
            {
                pool.OnStateChanged += (msg) => Console.WriteLine(msg);

                var sw = Stopwatch.StartNew();

                foreach (var task in tasks)
                    pool.EnqueueTask(task);

                while (_completedTests < tasks.Count)
                {
                    Thread.Sleep(500);
                    Console.WriteLine($"Progress: {_completedTests}/{tasks.Count}, Failed: {_failedTests}, Threads: {pool.CurrentThreadCount}, Queue: {pool.PendingTaskCount}");
                }

                sw.Stop();
                Console.WriteLine($"\nAll tasks completed in {sw.Elapsed.TotalSeconds:F2}s. Failed: {_failedTests}");
                Console.WriteLine($"Peak threads: {pool.CurrentThreadCount}");
            }
        }

        static List<TestInfo> GetAllTestCases(Assembly assembly)
        {
            var result = new List<TestInfo>();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null
                            && t.Name != "SharedContextTests");

            foreach (var cls in testClasses)
            {
                var setup = cls.GetMethod("SetUp", BindingFlags.Instance | BindingFlags.Public);
                var teardown = cls.GetMethod("TearDown", BindingFlags.Instance | BindingFlags.Public);

                var normalMethods = cls.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null);
                foreach (var method in normalMethods)
                {
                    result.Add(new TestInfo
                    {
                        TestClass = cls,
                        Method = method,
                        IsParameterized = false,
                        Parameters = null,
                        Setup = setup,
                        Teardown = teardown,
                        Categories = method.GetCustomAttributes<TestCategoryAttribute>().Select(a => a.Category).ToList(),
                        Priority = method.GetCustomAttribute<PriorityAttribute>()?.Priority ?? 0,
                        Author = method.GetCustomAttribute<AuthorAttribute>()?.Name ?? ""
                    });
                }

                var paramMethods = cls.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<ParameterizedTestAttribute>() != null);
                foreach (var method in paramMethods)
                {
                    var enumerable = method.Invoke(Activator.CreateInstance(cls), null) as System.Collections.IEnumerable;
                    if (enumerable != null)
                    {
                        int idx = 0;
                        foreach (var paramSet in enumerable)
                        {
                            var parameters = paramSet as object[];
                            if (parameters != null)
                            {
                                result.Add(new TestInfo
                                {
                                    TestClass = cls,
                                    Method = method,
                                    IsParameterized = true,
                                    Parameters = parameters,
                                    Setup = setup,
                                    Teardown = teardown,
                                    Categories = method.GetCustomAttributes<TestCategoryAttribute>().Select(a => a.Category).ToList(),
                                    Priority = method.GetCustomAttribute<PriorityAttribute>()?.Priority ?? 0,
                                    Author = method.GetCustomAttribute<AuthorAttribute>()?.Name ?? "",
                                    ParameterSetIndex = idx++
                                });
                            }
                        }
                    }
                }
            }
            return result;
        }

        static void ExecuteTest(TestInfo info)
        {
            string testName = info.IsParameterized
                ? $"{info.TestClass.Name}.{info.Method.Name}({string.Join(",", info.Parameters)})"
                : $"{info.TestClass.Name}.{info.Method.Name}";

            try
            {
                var instance = Activator.CreateInstance(info.TestClass);
                info.Setup?.Invoke(instance, null);
                if (info.IsParameterized)
                {
                    info.Method.Invoke(instance, info.Parameters);
                }
                else
                {
                    var task = info.Method.Invoke(instance, null) as System.Threading.Tasks.Task;
                    task?.GetAwaiter().GetResult();
                }
                info.Teardown?.Invoke(instance, null);
                lock (_statsLock)
                {
                    _completedTests++;
                }
                Console.WriteLine($"[OK] {testName}");
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                lock (_statsLock)
                {
                    _completedTests++;
                    _failedTests++;
                }
                Console.WriteLine($"[FAIL] {testName}: {inner.Message}");
            }
        }
    }

    public class TestInfo
    {
        public Type TestClass { get; set; }
        public MethodInfo Method { get; set; }
        public bool IsParameterized { get; set; }
        public object[] Parameters { get; set; }
        public MethodInfo Setup { get; set; }
        public MethodInfo Teardown { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        public int Priority { get; set; }
        public string Author { get; set; } = "";
        public int ParameterSetIndex { get; set; }
    }
}