![alt tag](https://github.com/jchristn/PDictionary/raw/main/Assets/icon.ico)

# PDictionary

[![NuGet Version](https://img.shields.io/nuget/v/PDictionary.svg?style=flat)](https://www.nuget.org/packages/PDictionary/) | [![NuGet](https://img.shields.io/nuget/dt/PDictionary.svg)](https://www.nuget.org/packages/PDictionary)

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

## Testing

The library is covered by an exhaustive, runner-agnostic test suite built on [Touchstone](https://www.nuget.org/packages/Touchstone.Core). All test cases (both positive and negative) are defined once in **`Test.Shared`** (the single source of truth) and executed unchanged by three hosts:

- **`Test.Automated`** &mdash; Touchstone CLI runner. Run with `dotnet run --project src/Test.Automated`. Prints a colored pass/fail table and returns a non-zero exit code on failure (CI-friendly). Pass a path argument to also export JSON results.
- **`Test.Xunit`** &mdash; Touchstone xUnit adapter. Run with `dotnet test src/Test.Xunit`.
- **`Test.Nunit`** &mdash; Touchstone NUnit adapter. Run with `dotnet test src/Test.Nunit`.

The `Test` project remains an interactive console app for exercising the library by hand.

## Version History

Refer to `CHANGELOG.md` for version history.
