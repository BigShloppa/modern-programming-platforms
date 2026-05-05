using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            Console.WriteLine("=== Dynamic Thread Pool Load Simulator ===");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var assembly = Assembly.LoadFrom("Tests.dll");
            var testMethods = GetAllTestMethods(assembly);
            Console.WriteLine($"Found {testMethods.Count} test methods.");

            int minThreads = 1;
            int maxThreads = 8;
            int idleTimeoutSec = 2;
            int queueThresholdSec = 1;

            using (var pool = new DynamicThreadPool(minThreads, maxThreads, idleTimeoutSec, queueThresholdSec))
            {
                pool.OnStateChanged += (msg) => Console.WriteLine(msg);

                var stopwatch = Stopwatch.StartNew();
                var random = new Random();

                List<Action> allTasks = new List<Action>();
                foreach (var method in testMethods)
                {
                    allTasks.Add(() => RunTestMethod(method));
                }

                int totalTasks = allTasks.Count;
                int tasksToRun = Math.Max(50, totalTasks);
                while (allTasks.Count < tasksToRun)
                {
                    allTasks.AddRange(allTasks);
                }
                if (allTasks.Count > tasksToRun)
                    allTasks = allTasks.GetRange(0, tasksToRun);

                Console.WriteLine($"\nPreparing to execute {allTasks.Count} test tasks...");

                Console.WriteLine("\n--- PHASE 1: Low load (single tasks every 2 seconds) ---");
                for (int i = 0; i < Math.Min(10, allTasks.Count); i++)
                {
                    pool.EnqueueTask(allTasks[i]);
                    Thread.Sleep(2000);
                }

                Console.WriteLine("\n--- PHASE 2: Burst load (enqueue 20 tasks quickly) ---");
                int burstStart = 10;
                for (int i = burstStart; i < burstStart + 20 && i < allTasks.Count; i++)
                {
                    pool.EnqueueTask(allTasks[i]);
                }

                Console.WriteLine("\n--- PHASE 3: Idle period (no tasks for 5 seconds) ---");
                Thread.Sleep(5000);

                Console.WriteLine("\n--- PHASE 4: Continuous random load ---");
                int remainingStart = burstStart + 20;
                for (int i = remainingStart; i < allTasks.Count; i++)
                {
                    pool.EnqueueTask(allTasks[i]);
                    int delay = random.Next(100, 800);
                    Thread.Sleep(delay);
                }

                Console.WriteLine("\n--- Waiting for all tasks to complete ---");
                while (_completedTests < allTasks.Count)
                {
                    Thread.Sleep(500);
                    Console.WriteLine($"Completed: {_completedTests}/{allTasks.Count}, Failed: {_failedTests}, Pool threads: {pool.CurrentThreadCount}, Queue: {pool.PendingTaskCount}");
                }

                stopwatch.Stop();
                Console.WriteLine($"\nAll tasks completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
                Console.WriteLine($"Total tests executed: {_completedTests}, Failed: {_failedTests}");
                Console.WriteLine($"Peak thread count observed: {pool.CurrentThreadCount}");
                Console.WriteLine("Dynamic pool demonstrated scaling.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void RunTestMethod(MethodInfo method)
        {
            string testName = $"{method.DeclaringType.Name}.{method.Name}";
            try
            {
                var testClass = Activator.CreateInstance(method.DeclaringType);
                var setUp = method.DeclaringType.GetMethod("SetUp", BindingFlags.Instance | BindingFlags.Public);
                var tearDown = method.DeclaringType.GetMethod("TearDown", BindingFlags.Instance | BindingFlags.Public);
                setUp?.Invoke(testClass, null);
                var task = method.Invoke(testClass, null) as System.Threading.Tasks.Task;
                if (task != null)
                    task.GetAwaiter().GetResult();
                tearDown?.Invoke(testClass, null);
                lock (_statsLock)
                {
                    _completedTests++;
                }
                Console.WriteLine($"[OK] {testName}");
            }
            catch (Exception ex)
            {
                lock (_statsLock)
                {
                    _completedTests++;
                    _failedTests++;
                }
                Console.WriteLine($"[FAIL] {testName}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static List<MethodInfo> GetAllTestMethods(Assembly assembly)
        {
            var result = new List<MethodInfo>();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null
                            && t.Name != "SharedContextTests");   /**/ 
            foreach (var cls in testClasses)
            {
                var methods = cls.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null);
                result.AddRange(methods);
            }
            return result;
        }
    }
}