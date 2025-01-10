namespace Pridr_IntergrationTest.Helpers;

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

public class TestOrderAttribute : Attribute
{
    public int Order { get; }

    public TestOrderAttribute(int order)
    {
        Order = order;
    }
}

public class CustomTestCaseOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        var sortedCases = testCases
            .Select(tc => new
            {
                TestCase = tc,
                Order = tc.TestMethod.Method
                    .GetCustomAttributes(typeof(TestOrderAttribute))
                    .FirstOrDefault()
                    ?.GetNamedArgument<int>("Order") ?? 0
            })
            .OrderBy(tc => tc.Order)
            .Select(tc => tc.TestCase);

        return sortedCases;
    }
}
