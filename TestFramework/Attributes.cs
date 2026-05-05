using System;

namespace TestFramework
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
        public string Name { get; set; }
        public TestMethodAttribute() { }
        public TestMethodAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SetUpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TearDownAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SharedSetUpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SharedTearDownAttribute : Attribute
    {
    }
}