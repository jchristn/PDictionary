namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// NUnit adapter for the shared PDictionary test descriptors.
    /// <para>
    /// Each Touchstone <see cref="TestCaseDescriptor"/> defined in <see cref="PDictionaryTests.Suites"/> is surfaced as
    /// its own NUnit test case via <see cref="TouchstoneTestCaseSource"/>, giving individual discovery and reporting
    /// under <c>dotnet test</c>. The descriptors in Test.Shared remain the single source of truth - no logic is copied.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class PDictionaryNunitTests
    {
        /// <summary>
        /// One NUnit test case per shared test descriptor.
        /// </summary>
        public static IEnumerable Cases => new TouchstoneTestCaseSource(PDictionaryTests.Suites);

        [Test]
        [TestCaseSource(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            if (testCase.Skip)
            {
                Assert.Ignore(testCase.SkipReason ?? "Skipped");
                return;
            }

            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
