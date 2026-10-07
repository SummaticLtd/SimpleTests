# SimpleTests

[![NuGet](https://img.shields.io/nuget/v/SimpleTests.svg)](https://www.nuget.org/packages/SimpleTests)

A minimal, explicit, type-safe dotnet test framework built on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-overview).

## Why SimpleTests?

- **No annotations or reflection.** Tests are created explicitly as values, so there's no magic discovery step.
- **Type-safe data-driven tests.** Test cases are generic (`Test.CasesSync<'a>`, `Test.CasesAsync<'a>`), unlike annotation-based `[InlineData]` approaches.
- **Faster test discovery.** No assembly scanning or reflection.
- **Explicit setup.** One-time setup functions can be specified per `TestFolder`, `TestList`, or the entire run, with no convention-based lifecycle.
- **Explicit hierarchy matching Visual Studio.** `TestFolder` maps to a namespace and `TestList` maps to a class in Test Explorer.
- **Standalone executable.** Built on Microsoft.Testing.Platform, each test project runs as its own process with no vstest.console.exe host, giving faster startup and simpler CI orchestration.
- **Minimal API surface.** Just `Test`, `TestList`, `TestFolder`, `Assert`, and `Runner.Run`.

## Assertions

Various assertions, such as `Assert.Equal`, `Assert.True`, and `Assert.CollectionEqual`, are provided in the `SimpleTests` namespace.

## Testing with Microsoft.Testing.Platform

SimpleTests uses Microsoft.Testing.Platform. See [Testing with `dotnet test`](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test) for how to run it.

## Filtering tests

Pass `--filter <text>` to run only the tests whose full name `namespace.list.test` (your `TestFolder`, `TestList`, and test names joined by dots) contains `<text>`.

```console
dotnet run -- --filter passing
```
