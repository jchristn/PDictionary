namespace PersistentDictionary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using SerializationHelper;

    /// <summary>
    /// Persistent dictionary.  Key and value types must both be JSON serializable.
    /// </summary>
    /// <typeparam name="TKey">Type for key.</typeparam>
    /// <typeparam name="TValue">Type for value.</typeparam>
    public class PDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        #region Public-Members

        /// <summary>
        /// Serializer.
        /// </summary>
        public Serializer Serializer
        {
            get
            {
                return _Serializer;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Serializer));
                _Serializer = value;
            }
        }

        /// <summary>
        /// Get or set a specific key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value.</returns>
        public TValue this[TKey key]
        {
            get
            {
                return GetValue(key);
            }
            set
            {
                SetValue(key, value);
            }
        }

        /// <summary>
        /// Retrieve keys.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get
            {
                _Lock.EnterReadLock();
                try
                {
                    return _Dictionary.Keys.ToList();
                }
                finally
                {
                    _Lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Retrieve values.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                _Lock.EnterReadLock();
                try
                {
                    return _Dictionary.Values.ToList();
                }
                finally
                {
                    _Lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Retrieve the count of the number of entries.
        /// </summary>
        public int Count
        {
            get
            {
                _Lock.EnterReadLock();
                try
                {
                    return _Dictionary.Count;
                }
                finally
                {
                    _Lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Indicates if the dictionary is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        #endregion

        #region Private-Members

        private string _Filename = null;
        private Serializer _Serializer = new Serializer();
        private Dictionary<TKey, TValue> _Dictionary = new Dictionary<TKey, TValue>();
        private readonly ReaderWriterLockSlim _Lock = new ReaderWriterLockSlim();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate using the specified file.  If the file exists, its contents will be loaded into the dictionary.
        /// </summary>
        /// <param name="filename">Filename.</param>
        public PDictionary(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            _Filename = filename;

            if (File.Exists(filename))
                _Dictionary = _Serializer.DeserializeJson<Dictionary<TKey, TValue>>(File.ReadAllText(filename));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add a key-value pair.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            SetValue(key, value);
        }

        /// <summary>
        /// Add a key-value pair.
        /// </summary>
        /// <param name="item">Key-value pair.</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Clear the dictionary.
        /// </summary>
        public void Clear()
        {
            _Lock.EnterWriteLock();
            try
            {
                _Dictionary.Clear();
                WriteFile();
            }
            finally
            {
                _Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Check if a key-value pair exists.
        /// </summary>
        /// <param name="item">Key-value pair.</param>
        /// <returns>True if exists.</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            _Lock.EnterReadLock();
            try
            {
                return _Dictionary.Contains(item);
            }
            finally
            {
                _Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Check if a key exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if exists.</returns>
        public bool ContainsKey(TKey key)
        {
            _Lock.EnterReadLock();
            try
            {
                return _Dictionary.ContainsKey(key);
            }
            finally
            {
                _Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Copy to a key-value pair array.
        /// </summary>
        /// <param name="array">Array.</param>
        /// <param name="arrayIndex">Array index.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _Lock.EnterReadLock();
            try
            {
                (_Dictionary as IDictionary<TKey, TValue>).CopyTo(array, arrayIndex);
            }
            finally
            {
                _Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Retrieve the enumerator.
        /// </summary>
        /// <returns>Enumerator.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            _Lock.EnterReadLock();
            try
            {
                return _Dictionary.ToList().GetEnumerator();
            }
            finally
            {
                _Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Remove a key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if removed.</returns>
        public bool Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return RemoveKey(key);
        }

        /// <summary>
        /// Remove a key-value pair.
        /// </summary>
        /// <param name="item">Key-value pair.</param>
        /// <returns>True if found.</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null) throw new ArgumentNullException(nameof(item.Key));

            return RemoveKeyValue(item.Key, item.Value);
        }

        /// <summary>
        /// Try to retrieve a value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns>True if found.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            _Lock.EnterReadLock();
            try
            {
                return _Dictionary.TryGetValue(key, out value);
            }
            finally
            {
                _Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Retrieve the enumerator.
        /// </summary>
        /// <returns>IEnumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Private-Methods

        private TValue GetValue(TKey key)
        {
            _Lock.EnterReadLock();
            try
            {
                return _Dictionary[key];
            }
            finally
            {
                _Lock.ExitReadLock();
            }
        }

        private void SetValue(TKey key, TValue val)
        {
            _Lock.EnterWriteLock();
            try
            {
                _Dictionary[key] = val;
                WriteFile();
            }
            finally
            {
                _Lock.ExitWriteLock();
            }
        }

        private bool RemoveKey(TKey key)
        {
            _Lock.EnterWriteLock();
            try
            {
                bool removed = _Dictionary.Remove(key);
                if (removed)
                {
                    WriteFile();
                }
                return removed;
            }
            finally
            {
                _Lock.ExitWriteLock();
            }
        }

        private bool RemoveKeyValue(TKey key, TValue val)
        {
            _Lock.EnterWriteLock();
            try
            {
                if (_Dictionary.TryGetValue(key, out TValue existingValue) && EqualityComparer<TValue>.Default.Equals(existingValue, val))
                {
                    _Dictionary.Remove(key);
                    WriteFile();
                    return true;
                }
                return false;
            }
            finally
            {
                _Lock.ExitWriteLock();
            }
        }

        private void WriteFile()
        {
            string tempFilename = Path.GetTempFileName();

            using (FileStream fs = new FileStream(tempFilename, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(_Serializer.SerializeJson(_Dictionary, false));
                    writer.Flush();
                    fs.Flush(true);
                }
            }

            // to ensure file is fully flushed
            File.Copy(tempFilename, _Filename, true);
            File.Delete(tempFilename);
        }

        #endregion
    }
}