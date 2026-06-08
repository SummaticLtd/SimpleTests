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

## Running tests

A SimpleTests project is a normal executable, so run it with `dotnet run` (or invoke the built `.exe` directly). Everything after `--` is passed to the test app:

```console
dotnet run -- [options]
```

### Running a subset

Filter by a test's **full name**, `namespace.list.test` — the `TestFolder`, `TestList`, and test names joined by dots (data-driven cases append `(case)`, e.g. `MathTests.DataDrivenTests.sync addition(1 + 2 = 3)`).

- **`--filter <text>`** — runs tests whose full name contains `<text>` on a word boundary, case-insensitive. Repeatable; a test runs if it matches **any** value.

    ```console
    dotnet run -- --filter passing            # tests with the word "passing"
    dotnet run -- --filter SyncTests          # a whole list (matches the segment, not "AsyncTests")
    dotnet run -- --filter "addition(1 + 2"   # a single data-driven case
    dotnet run -- --filter passing --filter ignored   # union of both
    ```

- **`--list-tests [--filter <text>]`** — lists matching tests without running them, to preview a filter.
- **`--filter-uid <uid>`** — exact full-name match; what IDE Test Explorer uses under the hood.

> The VSTest expression filter (`--filter "FullyQualifiedName~..."`) and `dotnet test --filter` aren't supported — SimpleTests runs directly on Microsoft.Testing.Platform without the VSTest bridge. Run `dotnet run -- --help` for all options.
