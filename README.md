# SimpleTests

A minimal, explicit, type-safe dotnet test framework built on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-overview).

## Why SimpleTests?

- **No annotations or reflection.** Tests are created explicitly as values, so there's no magic discovery step — just standard dotnet code.
- **Type-safe data-driven tests.** Test cases are generic (`Test.CasesSync<'a>`, `Test.CasesAsync<'a>`), so data and test logic are checked at compile time, unlike annotation-based `[InlineData]` approaches.
- **Faster test discovery.** No assembly scanning or reflection — tests are registered directly.
- **Explicit setup at every level.** One-time setup functions can be specified per `TestFolder`, `TestList`, or the entire run, with no convention-based lifecycle to learn.
- **Explicit hierarchy matching Visual Studio.** `TestFolder` maps to a namespace and `TestList` maps to a class in Test Explorer, so the tree you define is the tree you see.
- **Standalone executable.** Built on Microsoft.Testing.Platform, each test project runs as its own process with no vstest.console.exe host, giving faster startup and simpler CI orchestration.
- **Minimal API surface.** Just `Test`, `TestList`, `TestFolder`, and `Runner.Run`.
