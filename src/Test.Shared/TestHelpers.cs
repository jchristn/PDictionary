namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Lightweight assertion helpers used by the Touchstone test descriptors.
    /// A failed assertion throws <see cref="TestAssertException"/>, which Touchstone captures as a failed test.
    /// This project intentionally depends only on Touchstone.Core (no xUnit/NUnit) so the descriptors remain
    /// runner-agnostic and can be executed identically by the CLI runner, the xUnit adapter, and the NUnit adapter.
    /// </summary>
    internal static class Check
    {
        internal static void True(bool condition, string message = null)
        {
            if (!condition) throw new TestAssertException("Expected condition to be true. " + message);
        }

        internal static void False(bool condition, string message = null)
        {
            if (condition) throw new TestAssertException("Expected condition to be false. " + message);
        }

        internal static void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new TestAssertException($"Expected [{Fmt(expected)}] but got [{Fmt(actual)}]. {message}");
        }

        internal static void NotEqual<T>(T notExpected, T actual, string message = null)
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
                throw new TestAssertException($"Expected value to differ from [{Fmt(notExpected)}] but it did not. {message}");
        }

        internal static void NotNull(object value, string message = null)
        {
            if (value == null) throw new TestAssertException("Expected non-null value. " + message);
        }

        internal static void Null(object value, string message = null)
        {
            if (value != null) throw new TestAssertException("Expected null value. " + message);
        }

        /// <summary>
        /// Asserts that invoking <paramref name="action"/> throws an exception assignable to <typeparamref name="TException"/>.
        /// </summary>
        internal static void Throws<TException>(Action action, string message = null) where TException : Exception
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
                throw new TestAssertException($"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}. {message}");
            }

            throw new TestAssertException($"Expected {typeof(TException).Name} but no exception was thrown. {message}");
        }

        private static string Fmt(object value)
        {
            return value == null ? "null" : value.ToString();
        }
    }

    /// <summary>
    /// Exception thrown when a <see cref="Check"/> assertion fails.
    /// </summary>
    internal sealed class TestAssertException : Exception
    {
        internal TestAssertException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Provides isolated, self-cleaning temporary backing files for tests.
    /// </summary>
    internal static class TempFile
    {
        private static readonly string _Directory = Path.Combine(Path.GetTempPath(), "pdictionary-tests");

        /// <summary>
        /// Returns a unique, non-existent temporary file path within an isolated test directory.
        /// </summary>
        internal static string NewPath()
        {
            Directory.CreateDirectory(_Directory);
            return Path.Combine(_Directory, Guid.NewGuid().ToString("N") + ".json");
        }

        /// <summary>
        /// Runs <paramref name="body"/> with a fresh temporary backing-file path, deleting the file afterward.
        /// </summary>
        internal static void With(Action<string> body)
        {
            string path = NewPath();
            try
            {
                body(path);
            }
            finally
            {
                Cleanup(path);
            }
        }

        /// <summary>
        /// Runs <paramref name="body"/> after seeding the temporary file with <paramref name="initialContents"/>,
        /// deleting the file afterward. Used to exercise the "load an existing file" code path.
        /// </summary>
        internal static void WithExisting(string initialContents, Action<string> body)
        {
            string path = NewPath();
            try
            {
                File.WriteAllText(path, initialContents);
                body(path);
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void Cleanup(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // best-effort cleanup; never mask a test result with a cleanup failure
            }
        }
    }

    /// <summary>
    /// Simple JSON-serializable value type used to exercise the dictionary with complex value objects.
    /// </summary>
    public sealed class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Person other && Name == other.Name && Age == other.Age;
        }

        public override int GetHashCode()
        {
            return (Name == null ? 0 : Name.GetHashCode()) ^ Age;
        }

        public override string ToString()
        {
            return $"Person(Name={Name}, Age={Age})";
        }
    }
}
