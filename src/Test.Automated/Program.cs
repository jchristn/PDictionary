namespace Test.Automated
{
    using System;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Cli;

    /// <summary>
    /// Touchstone CLI runner for the PDictionary test suite.
    /// <para>
    /// Executes every suite defined in <see cref="PDictionaryTests.Suites"/> (the shared source of truth) and
    /// prints a colored, tabular pass/fail report. Returns a non-zero exit code if any test fails, so the runner
    /// can be wired into CI. Pass an optional path argument to also export machine-readable JSON results.
    /// </para>
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string resultsPath = args != null && args.Length > 0 ? args[0] : null;
            return await ConsoleRunner.RunAsync(PDictionaryTests.Suites, resultsPath: resultsPath);
        }
    }
}
