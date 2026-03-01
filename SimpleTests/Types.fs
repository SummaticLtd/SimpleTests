namespace SimpleTests

open System.Threading.Tasks
open System.Collections.Generic

[<RequireQualifiedAccess>]
type Test =
    | Sync of uniqueName: string * run: (unit -> Result<unit, string>)
    | Async of uniqueName: string * run: (unit -> Task<Result<unit, string>>)
    | CasesSync of uniqueName: string * cases: IReadOnlyCollection<string * (unit -> Result<unit, string>)>
    | CasesAsync of uniqueName: string * cases: IReadOnlyCollection<string * (unit -> Task<Result<unit, string>>)>

/// A collection of Tests, in VS Test Explorer corresponding to a "Class".
type TestList(name: string, tests: IReadOnlyCollection<Test>) =
    member _.Name = name
    member _.Tests = tests

/// A collection of test lists, in VS Test Explorer corresponding to a "Namespace".
type TestFolder(namespaceName: string, testLists: IReadOnlyCollection<TestList>) =
    member _.NamespaceName = namespaceName
    member _.TestLists = testLists