using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace TestFramework
{
    public static class Assert
    {
        public static void AreEqual(object expected, object actual)
        {
            if (!Equals(expected, actual))
                throw new AssertException($"Expected: {expected}, Actual: {actual}");
        }

        public static void AreNotEqual(object expected, object actual)
        {
            if (Equals(expected, actual))
                throw new AssertException($"Expected not equal: {expected}, but got same");
        }

        public static void IsTrue(bool condition)
        {
            if (!condition)
                throw new AssertException("Expected true, got false");
        }

        public static void IsFalse(bool condition)
        {
            if (condition)
                throw new AssertException("Expected false, got true");
        }

        public static void IsNull(object obj)
        {
            if (obj != null)
                throw new AssertException($"Expected null, got {obj}");
        }

        public static void IsNotNull(object obj)
        {
            if (obj == null)
                throw new AssertException("Expected not null, got null");
        }

        public static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new AssertException($"Expected {typeof(TException).Name}, got {ex.GetType().Name}");
            }
            throw new AssertException($"Expected {typeof(TException).Name}, but no exception thrown");
        }

        public static void Greater<T>(T a, T b) where T : IComparable<T>
        {
            if (a.CompareTo(b) <= 0)
                throw new AssertException($"{a} is not greater than {b}");
        }

        public static void Less<T>(T a, T b) where T : IComparable<T>
        {
            if (a.CompareTo(b) >= 0)
                throw new AssertException($"{a} is not less than {b}");
        }

        public static void Contains(string substring, string fullString)
        {
            if (!fullString.Contains(substring))
                throw new AssertException($"'{fullString}' does not contain '{substring}'");
        }

        public static void DoesNotContain(string substring, string fullString)
        {
            if (fullString.Contains(substring))
                throw new AssertException($"'{fullString}' contains '{substring}'");
        }
    }
}
