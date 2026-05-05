using System;
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
                Console.WriteLine("Tests.dll not found. Make sure it is built and in the output directory.");
                return;
            }
            Assembly assembly = Assembly.LoadFrom(testAssemblyPath);
            var executor = new TestExecutor();
            var results = executor.RunTests(assembly);
            PrintResultsToConsole(results);
            SaveResultsToFile(results, "test_results.txt");
        }

        static void PrintResultsToConsole(System.Collections.Generic.List<TestResult> results)
        {
            int passed = results.Count(r => r.Passed);
            int failed = results.Count(r => !r.Passed);
            Console.WriteLine($"Total: {results.Count}, Passed: {passed}, Failed: {failed}");
            foreach (var result in results)
            {
                Console.WriteLine($"{result.TestClassName}.{result.TestMethodName}: {(result.Passed ? "PASSED" : "FAILED")} ({result.DurationMs:F2} ms)");
                if (!result.Passed)
                    Console.WriteLine($"  Error: {result.Message}");
            }
        }

        static void SaveResultsToFile(System.Collections.Generic.List<TestResult> results, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                int passed = results.Count(r => r.Passed);
                int failed = results.Count(r => !r.Passed);
                writer.WriteLine($"Total: {results.Count}, Passed: {passed}, Failed: {failed}");
                foreach (var result in results)
                {
                    writer.WriteLine($"{result.TestClassName}.{result.TestMethodName}: {(result.Passed ? "PASSED" : "FAILED")} ({result.DurationMs:F2} ms)");
                    if (!result.Passed)
                        writer.WriteLine($"  Error: {result.Message}");
                }
            }
            Console.WriteLine($"Results saved to {filePath}");
        }
    }
}