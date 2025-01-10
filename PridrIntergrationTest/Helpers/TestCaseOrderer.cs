using Xunit.Abstractions;
using Xunit.Sdk;

namespace PridrIntergrationTest.Helpers;

public class TestCaseOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        Console.WriteLine("TestCaseOrderer is actief.");
        
        string assemblyName = typeof(TestPriorityAttribute).AssemblyQualifiedName!;
        var sortedMethods = new SortedDictionary<int, List<TTestCase>>();
        
        foreach (var testCase in testCases)
        {
            int priority = testCase.TestMethod.Method
                .GetCustomAttributes(assemblyName)
                .FirstOrDefault()
                ?.GetNamedArgument<int>(nameof(TestPriorityAttribute.Priority)) ?? 0;

            GetOrCreate(sortedMethods, priority).Add(testCase);
        }

        foreach (var testCase in sortedMethods.SelectMany(kvp => kvp.Value))
        {
            yield return testCase;
        }
    }

    private static TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : new()
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            value = new TValue();
            dictionary[key] = value;
        }
        return value;
    }
}


[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; }
    public TestPriorityAttribute(int priority) => Priority = priority;
}