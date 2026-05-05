using System;

namespace TestFramework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCategoryAttribute : Attribute
    {
        public string Category { get; }
        public TestCategoryAttribute(string category) => Category = category;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PriorityAttribute : Attribute
    {
        public int Priority { get; }
        public PriorityAttribute(int priority) => Priority = priority;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AuthorAttribute : Attribute
    {
        public string Name { get; }
        public AuthorAttribute(string name) => Name = name;
    }
}