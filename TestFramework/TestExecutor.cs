using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TestFramework
{
    public class TestExecutor
    {
        private readonly TestExecutionOptions _options;
        private readonly ConcurrentBag<TestResult> _results = new ConcurrentBag<TestResult>();
        private readonly object _consoleLock = new object();

        public TestExecutor(TestExecutionOptions options = null)
        {
            _options = options ?? new TestExecutionOptions();
        }

        public List<TestResult> RunTests(Assembly assembly)
        {
            _results.Clear();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null).ToList();

            if (_options.RunSequentially)
            {
                foreach (var testClass in testClasses)
                    RunTestClassSequentially(testClass);
            }
            else
            {
                Parallel.ForEach(testClasses, new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism },
                    testClass => RunTestClassParallel(testClass));
            }
            return _results.ToList();
        }

        private void RunTestClassSequentially(Type testClass)
        {
            var sharedSetUp = GetSharedSetUpMethod(testClass);
            var sharedTearDown = GetSharedTearDownMethod(testClass);
            sharedSetUp?.Invoke(null, null);
            var testMethods = GetTestMethods(testClass);
            foreach (var method in testMethods)
            {
                var result = RunSingleTest(testClass, method);
                AddResult(result);
            }
            sharedTearDown?.Invoke(null, null);
        }

        private void RunTestClassParallel(Type testClass)
        {
            var sharedSetUp = GetSharedSetUpMethod(testClass);
            var sharedTearDown = GetSharedTearDownMethod(testClass);
            bool usesSharedContext = sharedSetUp != null || sharedTearDown != null;

            sharedSetUp?.Invoke(null, null);
            var testMethods = GetTestMethods(testClass);

            if (usesSharedContext)
            {
                foreach (var method in testMethods)
                {
                    var result = RunSingleTest(testClass, method);
                    AddResult(result);
                }
            }
            else
            {
                Parallel.ForEach(testMethods, new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism },
                    method =>
                    {
                        var result = RunSingleTest(testClass, method);
                        AddResult(result);
                    });
            }

            sharedTearDown?.Invoke(null, null);
        }

        private List<MethodInfo> GetTestMethods(Type testClass)
        {
            return testClass.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null).ToList();
        }

        private TestResult RunSingleTest(Type testClass, MethodInfo method)
        {
            var result = new TestResult
            {
                TestClassName = testClass.Name,
                TestMethodName = method.Name,
                Passed = false
            };
            var instance = Activator.CreateInstance(testClass);
            var setUp = GetSetUpMethod(testClass);
            var tearDown = GetTearDownMethod(testClass);
            var timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();
            int timeoutMs = timeoutAttr?.Milliseconds ?? Timeout.Infinite;

            var start = DateTime.Now;
            var cts = new CancellationTokenSource();
            Task task = null;
            try
            {
                setUp?.Invoke(instance, null);
                task = Task.Run(() =>
                {
                    var invokeTask = method.Invoke(instance, null) as Task;
                    if (invokeTask != null)
                        invokeTask.GetAwaiter().GetResult();
                }, cts.Token);
                if (timeoutMs > 0 && !task.Wait(timeoutMs, cts.Token))
                {
                    cts.Cancel();
                    throw new TimeoutException($"Test exceeded timeout of {timeoutMs} ms");
                }
                task.Wait(cts.Token);
                result.Passed = true;
            }
            catch (AggregateException ex)
            {
                var inner = ex.InnerException;
                if (inner is TargetInvocationException tie)
                    result.Message = tie.InnerException?.Message ?? tie.Message;
                else
                    result.Message = inner?.Message ?? ex.Message;
            }
            catch (TargetInvocationException ex)
            {
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            finally
            {
                tearDown?.Invoke(instance, null);
                cts.Dispose();
            }
            result.DurationMs = (DateTime.Now - start).TotalMilliseconds;
            return result;
        }

        private void AddResult(TestResult result)
        {
            _results.Add(result);
            lock (_consoleLock)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {result.TestClassName}.{result.TestMethodName}: {(result.Passed ? "PASS" : "FAIL")} ({result.DurationMs:F2} ms)");
            }
        }

        private MethodInfo GetSetUpMethod(Type type) =>
            type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SetUpAttribute>() != null);
        private MethodInfo GetTearDownMethod(Type type) =>
            type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TearDownAttribute>() != null);
        private MethodInfo GetSharedSetUpMethod(Type type) =>
            type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SharedSetUpAttribute>() != null && m.IsStatic);
        private MethodInfo GetSharedTearDownMethod(Type type) =>
            type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SharedTearDownAttribute>() != null && m.IsStatic);
    }
}