using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TestFramework
{
    public class TestExecutor
    {
        public List<TestResult> RunTests(Assembly assembly)
        {
            var results = new List<TestResult>();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null);

            foreach (var testClass in testClasses)
            {
                var sharedSetUp = GetSharedSetUpMethod(testClass);
                var sharedTearDown = GetSharedTearDownMethod(testClass);
                sharedSetUp?.Invoke(null, null);

                var testMethods = testClass.GetMethods()
                    .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null);

                foreach (var method in testMethods)
                {
                    var instance = Activator.CreateInstance(testClass);
                    var setUp = GetSetUpMethod(testClass);
                    var tearDown = GetTearDownMethod(testClass);
                    var result = RunSingleTest(instance, method, setUp, tearDown);
                    results.Add(result);
                }

                sharedTearDown?.Invoke(null, null);
            }
            return results;
        }

        private TestResult RunSingleTest(object instance, MethodInfo method, MethodInfo setUp, MethodInfo tearDown)
        {
            var result = new TestResult
            {
                TestClassName = instance.GetType().Name,
                TestMethodName = method.Name,
                Passed = false
            };
            var start = DateTime.Now;
            try
            {
                setUp?.Invoke(instance, null);
                var task = method.Invoke(instance, null) as Task;
                if (task != null)
                    task.GetAwaiter().GetResult();
                result.Passed = true;
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
            }
            result.DurationMs = (DateTime.Now - start).TotalMilliseconds;
            return result;
        }

        private MethodInfo GetSetUpMethod(Type type)
        {
            return type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SetUpAttribute>() != null);
        }

        private MethodInfo GetTearDownMethod(Type type)
        {
            return type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TearDownAttribute>() != null);
        }

        private MethodInfo GetSharedSetUpMethod(Type type)
        {
            return type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SharedSetUpAttribute>() != null && m.IsStatic);
        }

        private MethodInfo GetSharedTearDownMethod(Type type)
        {
            return type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SharedTearDownAttribute>() != null && m.IsStatic);
        }
    }
}
