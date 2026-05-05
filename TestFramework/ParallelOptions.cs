namespace TestFramework
{
    public class TestExecutionOptions
    {
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        public bool RunSequentially { get; set; } = false;
    }
}