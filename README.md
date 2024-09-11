# PDictionary

PDictionary is a persistent dictionary that stores dictionary contents to a JSON file on the filesystem.

## New in v1.0.x

- Initial release

## Usage

Refer to the `Test` project for an example of how to exercise the library.  `PDictionary` implements the `IDictionary` interface and can be used like a regular dictionary.  Any changes to the dictionary are written to the backing file specified in the constructor.

```csharp
using PersistentDictionary;

// Instantiate and specify the backing JSON file
// Key and value types must be JSON-serializable
// If the file already exists, its contents are loaded in
// If the file does not exist, it will be created when data is added to the dictionary
PDictionary<string, string> pdict = new PDictionary<string, string>("pdict.json");

pdict.Add("hello", "world"); // automatically re-writes the backing file
```

## Version History

Refer to `CHANGELOG.md` for version history.
