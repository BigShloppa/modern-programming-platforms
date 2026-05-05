using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using TestFramework;

namespace TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            string testAssemblyPath = "Tests.dll";
            if (!File.Exists(testAssemblyPath))
            {
                Console.WriteLine("Tests.dll not found. Build the Tests project first.");
                return;
            }

            Assembly assembly = Assembly.LoadFrom(testAssemblyPath);

            int maxParallel = 2;

            Console.WriteLine($"MaxDegreeOfParallelism = {maxParallel}");


            TestExecutionOptions seqOptions = new TestExecutionOptions { RunSequentially = true };
            TestExecutor seqExecutor = new TestExecutor(seqOptions);
            Console.WriteLine("\n--- SEQUENTIAL RUN ---");
            Stopwatch sw = Stopwatch.StartNew();
            var seqResults = seqExecutor.RunTests(assembly);
            sw.Stop();
            long seqTime = sw.ElapsedMilliseconds;
            PrintSummary(seqResults);
            Console.WriteLine($"Elapsed time: {seqTime} ms");

            TestExecutionOptions parOptions = new TestExecutionOptions { RunSequentially = false, MaxDegreeOfParallelism = maxParallel };
            TestExecutor parExecutor = new TestExecutor(parOptions);
            Console.WriteLine("\n--- PARALLEL RUN ---");
            sw.Restart();
            var parResults = parExecutor.RunTests(assembly);
            sw.Stop();
            long parTime = sw.ElapsedMilliseconds;
            PrintSummary(parResults);
            Console.WriteLine($"Elapsed time: {parTime} ms");

            double speedup = (double)seqTime / parTime;
            Console.WriteLine($"\nSpeedup: {speedup:F2}x (sequential {seqTime} ms / parallel {parTime} ms)");

            SaveResultsToFile(parResults, "parallel_test_results.txt");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void PrintSummary(List<TestResult> results)
        {
            int passed = results.Count(r => r.Passed);
            int failed = results.Count(r => !r.Passed);
            Console.WriteLine($"Passed: {passed}, Failed: {failed}, Total: {results.Count}");
        }

        static void SaveResultsToFile(List<TestResult> results, string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                foreach (TestResult r in results)
                {
                    writer.WriteLine($"{r.TestClassName}.{r.TestMethodName}: {(r.Passed ? "PASS" : "FAIL")} ({r.DurationMs:F2} ms)");
                    if (!string.IsNullOrEmpty(r.Message))
                        writer.WriteLine($"  Message: {r.Message}");
                }
            }
            Console.WriteLine($"Results saved to {path}");
        }
    }
}