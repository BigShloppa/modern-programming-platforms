using System;
using System.Threading.Tasks;

namespace TestSubject
{
    public class Calculator
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public int Multiply(int a, int b) => a * b;
        public double Divide(int a, int b)
        {
            if (b == 0) throw new DivideByZeroException();
            return (double)a / b;
        }
        public async Task<int> SlowAddAsync(int a, int b)
        {
            await Task.Delay(10);
            return a + b;
        }
    }

    public class StringHelper
    {
        public string Concat(string a, string b) => a + b;
        public bool Contains(string str, string sub) => str.Contains(sub);
        public string ToUpper(string str) => str.ToUpper();
    }

    public class DataStore
    {
        private static int _sharedCounter = 0;
        public static int SharedCounter => _sharedCounter;
        public static void IncrementShared() => _sharedCounter++;
        public static void ResetShared() => _sharedCounter = 0;
    }
}