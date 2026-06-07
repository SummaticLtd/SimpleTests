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

### Running a filtered subset

SimpleTests is built directly on Microsoft.Testing.Platform as a standalone test framework. It does **not** use the VSTest bridge, so the familiar VSTest expression filter (`--filter "FullyQualifiedName~..."`) and `dotnet test --filter` are **not** available. Instead, filtering uses the test's **full name**:

```
namespace.list.test
```

— that is, the `TestFolder` namespace, the `TestList` name, and the test name joined by dots. Data-driven cases append the case name in parentheses, e.g. `MathTests.DataDrivenTests.sync addition(1 + 2 = 3)`.

Three ways to filter, in order of usefulness:

- **`--filter <text>`** — runs every test whose full name *contains* `<text>`, case-insensitive (like VSTest's `~` operator). Repeatable; a test runs if it matches **any** value. This is the option to reach for.

    ```console
    dotnet run -- --filter "passing"            # any test whose name contains "passing"
    dotnet run -- --filter ".SyncTests."        # a whole list (dots anchor the segment)
    dotnet run -- --filter "addition(1 + 2"     # a single data-driven case
    dotnet run -- --filter "passing" --filter "ignored"   # union of both
    ```

    Because it's a substring match, `--filter "SyncTests"` also matches `AsyncTests` (it contains the letters `synctests`). Anchor with the surrounding dots — `--filter ".SyncTests."` — to target one list precisely.

- **`--list-tests`** — lists test display names without running them. Combine with `--filter` to preview exactly what a filter selects before running it:

    ```console
    dotnet run -- --list-tests --filter ".SyncTests."
    ```

- **`--filter-uid <uid> [<uid> ...]`** — a built-in Microsoft.Testing.Platform option that runs tests by their **exact** full name (no substring). This is what IDE Test Explorer uses under the hood. A non-matching UID silently runs zero tests, so prefer `--filter` unless you need an exact match.

    ```console
    dotnet run -- --filter-uid "MathTests.SyncTests.passing sync test"
    ```

Run `dotnet run -- --help` to see all available options.
