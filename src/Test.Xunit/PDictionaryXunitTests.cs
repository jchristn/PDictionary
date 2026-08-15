namespace Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;
    using global::Xunit;

    /// <summary>
    /// xUnit adapter for the shared PDictionary test descriptors.
    /// <para>
    /// Each Touchstone <see cref="TestCaseDescriptor"/> defined in <see cref="PDictionaryTests.Suites"/> is projected
    /// into a single xUnit theory row via <see cref="TouchstoneTheoryData"/>, so every shared case shows up as its own
    /// discoverable, individually-reportable xUnit test. No test logic is duplicated here - the descriptors in
    /// Test.Shared remain the single source of truth.
    /// </para>
    /// </summary>
    public sealed class PDictionaryXunitTests
    {
        /// <summary>
        /// One theory row per shared test case.
        /// </summary>
        public static TheoryData<TestCaseDescriptor> Cases => new TouchstoneTheoryData(PDictionaryTests.Suites);

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            if (testCase.Skip) return;
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
