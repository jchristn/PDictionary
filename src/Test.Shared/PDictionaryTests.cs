namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using PersistentDictionary;
    using Touchstone.Core;

    /// <summary>
    /// Central, runner-agnostic source of truth for the PDictionary test suite.
    /// <para>
    /// Every behavior of <see cref="PDictionary{TKey, TValue}"/> is described here as a set of Touchstone
    /// <see cref="TestSuiteDescriptor"/> / <see cref="TestCaseDescriptor"/> objects. The same descriptors are executed
    /// unchanged by:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Test.Automated - the Touchstone CLI runner</description></item>
    /// <item><description>Test.Xunit - the Touchstone xUnit adapter</description></item>
    /// <item><description>Test.Nunit - the Touchstone NUnit adapter</description></item>
    /// </list>
    /// Both positive (expected success) and negative (expected exception / false result) cases are covered.
    /// </summary>
    public static class PDictionaryTests
    {
        /// <summary>
        /// All test suites, exposed to the CLI runner and framework adapters.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    ConstructorSuite(),
                    AddSuite(),
                    IndexerSuite(),
                    RemoveSuite(),
                    QuerySuite(),
                    EnumerationSuite(),
                    ClearSuite(),
                    SerializerSuite(),
                    PersistenceSuite(),
                    TypeCoverageSuite()
                };
            }
        }

        #region Case-Helper

        private static TestCaseDescriptor Case(string suiteId, string caseId, string displayName, Action body)
        {
            return new TestCaseDescriptor(
                suiteId,
                caseId,
                displayName,
                executeAsync: _ =>
                {
                    body();
                    return Task.CompletedTask;
                });
        }

        #endregion

        #region Constructor-Suite

        private static TestSuiteDescriptor ConstructorSuite()
        {
            const string s = "Constructor";
            return new TestSuiteDescriptor(s, "Constructor and file bootstrap", new List<TestCaseDescriptor>
            {
                Case(s, "NullFilename", "Constructor with null filename throws ArgumentNullException", () =>
                {
                    Check.Throws<ArgumentNullException>(() => new PDictionary<string, string>(null));
                }),

                Case(s, "EmptyFilename", "Constructor with empty filename throws ArgumentNullException", () =>
                {
                    Check.Throws<ArgumentNullException>(() => new PDictionary<string, string>(string.Empty));
                }),

                Case(s, "CreatesFileWhenMissing", "Constructor creates an empty backing file when it does not exist", () =>
                {
                    TempFile.With(path =>
                    {
                        Check.False(File.Exists(path), "Precondition: file should not exist yet");
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.True(File.Exists(path), "Backing file should be created on construction");
                        Check.Equal(0, dict.Count, "New dictionary should be empty");
                        Check.Equal("{}", File.ReadAllText(path).Trim(), "New backing file should contain an empty JSON object");
                    });
                }),

                Case(s, "LoadsExistingFile", "Constructor loads contents from an existing backing file", () =>
                {
                    TempFile.WithExisting("{\"alpha\":\"1\",\"beta\":\"2\"}", path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.Equal(2, dict.Count, "Both entries should load");
                        Check.Equal("1", dict["alpha"]);
                        Check.Equal("2", dict["beta"]);
                    });
                }),

                Case(s, "LoadsEmptyObjectFile", "Constructor loads an existing empty-object file as an empty dictionary", () =>
                {
                    TempFile.WithExisting("{}", path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.Equal(0, dict.Count);
                    });
                })
            });
        }

        #endregion

        #region Add-Suite

        private static TestSuiteDescriptor AddSuite()
        {
            const string s = "Add";
            return new TestSuiteDescriptor(s, "Add operations", new List<TestCaseDescriptor>
            {
                Case(s, "AddSingle", "Add inserts a key-value pair and increments Count", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("hello", "world");
                        Check.Equal(1, dict.Count);
                        Check.True(dict.ContainsKey("hello"));
                        Check.Equal("world", dict["hello"]);
                    });
                }),

                Case(s, "AddMultiple", "Add supports many distinct keys", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, int> dict = new PDictionary<string, int>(path);
                        for (int i = 0; i < 100; i++) dict.Add("k" + i, i);
                        Check.Equal(100, dict.Count);
                        Check.Equal(42, dict["k42"]);
                    });
                }),

                Case(s, "AddKeyValuePair", "Add(KeyValuePair) inserts the pair", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add(new KeyValuePair<string, string>("k", "v"));
                        Check.Equal("v", dict["k"]);
                        Check.Equal(1, dict.Count);
                    });
                }),

                Case(s, "AddNullKeyThrows", "Add with a null key throws ArgumentNullException", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.Throws<ArgumentNullException>(() => dict.Add(null, "v"));
                    });
                }),

                Case(s, "AddDuplicateKeyOverwrites", "Add with an existing key overwrites the value rather than throwing", () =>
                {
                    // PDictionary.Add routes through the indexer setter, so a duplicate key updates the value
                    // instead of throwing (this differs from Dictionary<TKey,TValue>.Add).
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", "first");
                        dict.Add("k", "second");
                        Check.Equal(1, dict.Count, "Duplicate key should not create a second entry");
                        Check.Equal("second", dict["k"], "Value should be overwritten");
                    });
                }),

                Case(s, "AddNullValueAllowed", "Add accepts a null value for reference-typed values", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", null);
                        Check.Equal(1, dict.Count);
                        Check.Null(dict["k"]);
                    });
                }),

                Case(s, "AddPersistsToDisk", "Add writes through to the backing file", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("persisted", "yes");
                        string contents = File.ReadAllText(path);
                        Check.True(contents.Contains("persisted"), "Backing file should contain the new key");
                        Check.True(contents.Contains("yes"), "Backing file should contain the new value");
                    });
                })
            });
        }

        #endregion

        #region Indexer-Suite

        private static TestSuiteDescriptor IndexerSuite()
        {
            const string s = "Indexer";
            return new TestSuiteDescriptor(s, "Indexer get/set", new List<TestCaseDescriptor>
            {
                Case(s, "SetThenGet", "Indexer set adds a value that can be read back", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict["a"] = "1";
                        Check.Equal("1", dict["a"]);
                        Check.Equal(1, dict.Count);
                    });
                }),

                Case(s, "SetUpdatesExisting", "Indexer set updates an existing key in place", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict["a"] = "1";
                        dict["a"] = "2";
                        Check.Equal(1, dict.Count);
                        Check.Equal("2", dict["a"]);
                    });
                }),

                Case(s, "GetMissingThrows", "Indexer get on a missing key throws KeyNotFoundException", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.Throws<KeyNotFoundException>(() => { string _ = dict["missing"]; });
                    });
                }),

                Case(s, "SetPersists", "Indexer set writes through to the backing file", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict["key"] = "value";
                        PDictionary<string, string> reloaded = new PDictionary<string, string>(path);
                        Check.Equal("value", reloaded["key"]);
                    });
                })
            });
        }

        #endregion

        #region Remove-Suite

        private static TestSuiteDescriptor RemoveSuite()
        {
            const string s = "Remove";
            return new TestSuiteDescriptor(s, "Remove operations", new List<TestCaseDescriptor>
            {
                Case(s, "RemoveExistingKey", "Remove(key) removes an existing entry and returns true", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", "v");
                        Check.True(dict.Remove("k"));
                        Check.Equal(0, dict.Count);
                        Check.False(dict.ContainsKey("k"));
                    });
                }),

                Case(s, "RemoveMissingKeyReturnsFalse", "Remove(key) on an absent key returns false without throwing", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.False(dict.Remove("missing"));
                    });
                }),

                Case(s, "RemoveNullKeyThrows", "Remove(key) with a null key throws ArgumentNullException", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.Throws<ArgumentNullException>(() => dict.Remove((string)null));
                    });
                }),

                Case(s, "RemovePairMatching", "Remove(KeyValuePair) removes when key and value both match", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", "v");
                        Check.True(dict.Remove(new KeyValuePair<string, string>("k", "v")));
                        Check.Equal(0, dict.Count);
                    });
                }),

                Case(s, "RemovePairValueMismatch", "Remove(KeyValuePair) returns false when the value does not match", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", "v");
                        Check.False(dict.Remove(new KeyValuePair<string, string>("k", "other")));
                        Check.Equal(1, dict.Count, "Entry should remain since the value did not match");
                    });
                }),

                Case(s, "RemovePairAbsent", "Remove(KeyValuePair) returns false when the key is absent", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.False(dict.Remove(new KeyValuePair<string, string>("missing", "v")));
                    });
                }),

                Case(s, "RemovePairNullKeyThrows", "Remove(KeyValuePair) with a null key throws ArgumentNullException", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.Throws<ArgumentNullException>(() => dict.Remove(new KeyValuePair<string, string>(null, "v")));
                    });
                }),

                Case(s, "RemovePersists", "Remove writes through to the backing file", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        dict.Remove("a");
                        PDictionary<string, string> reloaded = new PDictionary<string, string>(path);
                        Check.False(reloaded.ContainsKey("a"));
                        Check.True(reloaded.ContainsKey("b"));
                    });
                })
            });
        }

        #endregion

        #region Query-Suite

        private static TestSuiteDescriptor QuerySuite()
        {
            const string s = "Query";
            return new TestSuiteDescriptor(s, "Lookup, containment, and count", new List<TestCaseDescriptor>
            {
                Case(s, "ContainsKeyTrue", "ContainsKey returns true for a present key", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", "v");
                        Check.True(dict.ContainsKey("k"));
                    });
                }),

                Case(s, "ContainsKeyFalse", "ContainsKey returns false for an absent key", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.False(dict.ContainsKey("nope"));
                    });
                }),

                Case(s, "ContainsPairTrue", "Contains(KeyValuePair) returns true for an exact match", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", "v");
                        Check.True(dict.Contains(new KeyValuePair<string, string>("k", "v")));
                    });
                }),

                Case(s, "ContainsPairValueMismatch", "Contains(KeyValuePair) returns false when the value differs", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", "v");
                        Check.False(dict.Contains(new KeyValuePair<string, string>("k", "different")));
                    });
                }),

                Case(s, "TryGetValueHit", "TryGetValue returns true and the value for a present key", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("k", "v");
                        bool found = dict.TryGetValue("k", out string value);
                        Check.True(found);
                        Check.Equal("v", value);
                    });
                }),

                Case(s, "TryGetValueMiss", "TryGetValue returns false and default for an absent key", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, int> dict = new PDictionary<string, int>(path);
                        bool found = dict.TryGetValue("nope", out int value);
                        Check.False(found);
                        Check.Equal(0, value, "Out value should be default(int) on a miss");
                    });
                }),

                Case(s, "CountReflectsMutations", "Count reflects adds and removes", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.Equal(0, dict.Count);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        Check.Equal(2, dict.Count);
                        dict.Remove("a");
                        Check.Equal(1, dict.Count);
                    });
                }),

                Case(s, "IsReadOnlyFalse", "IsReadOnly is false", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.False(dict.IsReadOnly);
                    });
                })
            });
        }

        #endregion

        #region Enumeration-Suite

        private static TestSuiteDescriptor EnumerationSuite()
        {
            const string s = "Enumeration";
            return new TestSuiteDescriptor(s, "Keys, Values, CopyTo, and iteration", new List<TestCaseDescriptor>
            {
                Case(s, "KeysReflectsContents", "Keys returns all present keys", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        List<string> keys = dict.Keys.OrderBy(k => k).ToList();
                        Check.Equal(2, keys.Count);
                        Check.Equal("a", keys[0]);
                        Check.Equal("b", keys[1]);
                    });
                }),

                Case(s, "ValuesReflectsContents", "Values returns all present values", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        List<string> values = dict.Values.OrderBy(v => v).ToList();
                        Check.Equal(2, values.Count);
                        Check.Equal("1", values[0]);
                        Check.Equal("2", values[1]);
                    });
                }),

                Case(s, "EnumerateAllPairs", "Iterating yields every key-value pair", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        dict.Add("c", "3");
                        Dictionary<string, string> seen = new Dictionary<string, string>();
                        foreach (KeyValuePair<string, string> kvp in dict) seen[kvp.Key] = kvp.Value;
                        Check.Equal(3, seen.Count);
                        Check.Equal("1", seen["a"]);
                        Check.Equal("2", seen["b"]);
                        Check.Equal("3", seen["c"]);
                    });
                }),

                Case(s, "EnumerateEmpty", "Iterating an empty dictionary yields nothing", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        int count = 0;
                        foreach (KeyValuePair<string, string> kvp in dict) count++;
                        Check.Equal(0, count);
                    });
                }),

                Case(s, "EnumeratorIsSnapshot", "Enumerating returns a snapshot that tolerates concurrent mutation", () =>
                {
                    // GetEnumerator materializes a ToList() snapshot under a read lock, so mutating the
                    // dictionary while iterating must not throw an InvalidOperationException.
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        int iterated = 0;
                        foreach (KeyValuePair<string, string> kvp in dict)
                        {
                            iterated++;
                            dict["c" + iterated] = "x"; // mutate mid-enumeration
                        }
                        Check.Equal(2, iterated, "Snapshot should reflect the two entries present when iteration began");
                    });
                }),

                Case(s, "CopyToCopiesEntries", "CopyTo copies entries into an array at the given offset", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[4];
                        dict.CopyTo(array, 1);
                        List<string> copiedKeys = array.Skip(1).Take(2).Select(p => p.Key).OrderBy(k => k).ToList();
                        Check.Equal("a", copiedKeys[0]);
                        Check.Equal("b", copiedKeys[1]);
                        Check.Null(array[0].Key, "Offset slot should be untouched");
                    });
                }),

                Case(s, "CopyToNullArrayThrows", "CopyTo with a null array throws ArgumentNullException", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        Check.Throws<ArgumentNullException>(() => dict.CopyTo(null, 0));
                    });
                }),

                Case(s, "CopyToNegativeIndexThrows", "CopyTo with a negative index throws ArgumentOutOfRangeException", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[2];
                        Check.Throws<ArgumentOutOfRangeException>(() => dict.CopyTo(array, -1));
                    });
                }),

                Case(s, "CopyToInsufficientSpaceThrows", "CopyTo into an undersized array throws ArgumentException", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[1];
                        Check.Throws<ArgumentException>(() => dict.CopyTo(array, 0));
                    });
                })
            });
        }

        #endregion

        #region Clear-Suite

        private static TestSuiteDescriptor ClearSuite()
        {
            const string s = "Clear";
            return new TestSuiteDescriptor(s, "Clear operation", new List<TestCaseDescriptor>
            {
                Case(s, "ClearEmpties", "Clear removes all entries", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Add("b", "2");
                        dict.Clear();
                        Check.Equal(0, dict.Count);
                        Check.False(dict.ContainsKey("a"));
                    });
                }),

                Case(s, "ClearPersists", "Clear writes an empty dictionary through to disk", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("a", "1");
                        dict.Clear();
                        PDictionary<string, string> reloaded = new PDictionary<string, string>(path);
                        Check.Equal(0, reloaded.Count);
                    });
                }),

                Case(s, "ClearEmptyIsNoop", "Clear on an already-empty dictionary is a harmless no-op", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Clear();
                        Check.Equal(0, dict.Count);
                    });
                })
            });
        }

        #endregion

        #region Serializer-Suite

        private static TestSuiteDescriptor SerializerSuite()
        {
            const string s = "Serializer";
            return new TestSuiteDescriptor(s, "Serializer property", new List<TestCaseDescriptor>
            {
                Case(s, "DefaultSerializerNotNull", "Serializer is non-null by default", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.NotNull(dict.Serializer);
                    });
                }),

                Case(s, "SetSerializerNullThrows", "Setting Serializer to null throws ArgumentNullException", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        Check.Throws<ArgumentNullException>(() => dict.Serializer = null);
                    });
                }),

                Case(s, "SetSerializerReplaces", "Setting a new Serializer replaces the instance", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        SerializationHelper.Serializer replacement = new SerializationHelper.Serializer();
                        dict.Serializer = replacement;
                        Check.True(ReferenceEquals(replacement, dict.Serializer));
                    });
                })
            });
        }

        #endregion

        #region Persistence-Suite

        private static TestSuiteDescriptor PersistenceSuite()
        {
            const string s = "Persistence";
            return new TestSuiteDescriptor(s, "Round-trip persistence across instances", new List<TestCaseDescriptor>
            {
                Case(s, "RoundTripReload", "A new instance over the same file sees prior writes", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> first = new PDictionary<string, string>(path);
                        first.Add("a", "1");
                        first.Add("b", "2");
                        first["c"] = "3";

                        PDictionary<string, string> second = new PDictionary<string, string>(path);
                        Check.Equal(3, second.Count);
                        Check.Equal("1", second["a"]);
                        Check.Equal("2", second["b"]);
                        Check.Equal("3", second["c"]);
                    });
                }),

                Case(s, "UpdatesPersistAcrossReload", "Value updates survive a reload", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> first = new PDictionary<string, string>(path);
                        first.Add("a", "1");
                        first["a"] = "updated";

                        PDictionary<string, string> second = new PDictionary<string, string>(path);
                        Check.Equal("updated", second["a"]);
                    });
                }),

                Case(s, "BackingFileIsValidJson", "The backing file is well-formed JSON that round-trips", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, string> dict = new PDictionary<string, string>(path);
                        dict.Add("key", "value");
                        string json = File.ReadAllText(path);
                        Dictionary<string, string> parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        Check.Equal(1, parsed.Count);
                        Check.Equal("value", parsed["key"]);
                    });
                })
            });
        }

        #endregion

        #region Type-Coverage-Suite

        private static TestSuiteDescriptor TypeCoverageSuite()
        {
            const string s = "TypeCoverage";
            return new TestSuiteDescriptor(s, "Non-string key and complex value types", new List<TestCaseDescriptor>
            {
                Case(s, "IntKeyStringValue", "PDictionary<int,string> stores and reloads entries", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<int, string> dict = new PDictionary<int, string>(path);
                        dict.Add(1, "one");
                        dict.Add(2, "two");
                        Check.Equal("one", dict[1]);

                        PDictionary<int, string> reloaded = new PDictionary<int, string>(path);
                        Check.Equal(2, reloaded.Count);
                        Check.Equal("two", reloaded[2]);
                    });
                }),

                Case(s, "StringKeyIntValue", "PDictionary<string,int> stores and reloads entries", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, int> dict = new PDictionary<string, int>(path);
                        dict.Add("answer", 42);
                        PDictionary<string, int> reloaded = new PDictionary<string, int>(path);
                        Check.Equal(42, reloaded["answer"]);
                    });
                }),

                Case(s, "ComplexValueType", "PDictionary<string,Person> stores and reloads complex values", () =>
                {
                    TempFile.With(path =>
                    {
                        PDictionary<string, Person> dict = new PDictionary<string, Person>(path);
                        dict.Add("joel", new Person { Name = "Joel", Age = 30 });
                        dict.Add("jane", new Person { Name = "Jane", Age = 25 });

                        PDictionary<string, Person> reloaded = new PDictionary<string, Person>(path);
                        Check.Equal(2, reloaded.Count);
                        Check.Equal(new Person { Name = "Joel", Age = 30 }, reloaded["joel"]);
                        Check.Equal(25, reloaded["jane"].Age);
                    });
                })
            });
        }

        #endregion
    }
}
