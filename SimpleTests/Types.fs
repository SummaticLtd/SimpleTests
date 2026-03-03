namespace SimpleTests

open System.Threading.Tasks
open System.Collections.Generic
open System.ComponentModel
open System.Collections.Immutable
open System.Runtime.CompilerServices

type ICasesSync =
    abstract member Name: string
    abstract member FilePath: string
    abstract member LineNumber: int
    abstract member Names: IReadOnlyCollection<string>
    abstract member Runs: IReadOnlyCollection<string * (unit -> unit)>

type ICasesAsync =
    abstract member Name: string
    abstract member FilePath: string
    abstract member LineNumber: int
    abstract member Names: IReadOnlyCollection<string>
    abstract member Runs: IReadOnlyCollection<string * (unit -> Task<unit>)>

type private CasesSync<'a>(name: string, filePath: string, lineNumber: int, cases: ImmutableArray<string * 'a>, test: 'a -> unit) =
    interface ICasesSync with
        member _.Name = name
        member _.FilePath = filePath
        member _.LineNumber = lineNumber
        member _.Names = ImmutableArray.CreateRange(cases, fst)
        member _.Runs = ImmutableArray.CreateRange(cases, fun (caseName, x) -> (caseName, fun () -> test x))

type private CasesAsync<'a>(name: string, filePath: string, lineNumber: int, cases: ImmutableArray<string * 'a>, test: 'a -> Task<unit>) =
    interface ICasesAsync with
        member _.Name = name
        member _.FilePath = filePath
        member _.LineNumber = lineNumber
        member _.Names = ImmutableArray.CreateRange(cases, fst)
        member _.Runs = ImmutableArray.CreateRange(cases, fun (caseName, x) -> (caseName, fun () -> test x))

[<RequireQualifiedAccess>]
type Test =
    | [<EditorBrowsable(EditorBrowsableState.Never)>]
        ѪSync of uniqueName: string * run: (unit -> unit) * ignored: bool * filePath: string * lineNumber: int
    | [<EditorBrowsable(EditorBrowsableState.Never)>]
        ѪAsync of uniqueName: string * run: (unit -> Task<unit>) * ignored: bool * filePath: string * lineNumber: int
    | [<EditorBrowsable(EditorBrowsableState.Never)>]
        ICasesSync of ICasesSync
    | [<EditorBrowsable(EditorBrowsableState.Never)>]
        ICasesAsync of ICasesAsync
type Test with
    static member Sync(name: string, test: unit -> unit, [<CallerFilePath>] ?filePath: string, [<CallerLineNumber>] ?lineNumber: int) : Test =
        Test.ѪSync(name, test, false, defaultArg filePath "", defaultArg lineNumber 0)
    static member IgnoredSync(name: string, test: unit -> unit, [<CallerFilePath>] ?filePath: string, [<CallerLineNumber>] ?lineNumber: int) : Test =
        Test.ѪSync(name, test, true, defaultArg filePath "", defaultArg lineNumber 0)
    static member Async(name: string, test: unit -> Task<unit>, [<CallerFilePath>] ?filePath: string, [<CallerLineNumber>] ?lineNumber: int) : Test =
        Test.ѪAsync(name, test, false, defaultArg filePath "", defaultArg lineNumber 0)
    static member IgnoredAsync(name: string, test: unit -> Task<unit>, [<CallerFilePath>] ?filePath: string, [<CallerLineNumber>] ?lineNumber: int) : Test =
        Test.ѪAsync(name, test, true, defaultArg filePath "", defaultArg lineNumber 0)
    static member CasesSync<'a>(name: string, cases: IReadOnlyCollection<string * 'a>, test: 'a -> unit, [<CallerFilePath>] ?filePath: string, [<CallerLineNumber>] ?lineNumber: int) : Test =
        let cases = ImmutableArray.CreateRange(cases)
        let casesImpl = CasesSync(name, defaultArg filePath "", defaultArg lineNumber 0, cases, test)
        Test.ICasesSync casesImpl
    static member CasesAsync<'a>(name: string, cases: IReadOnlyCollection<string * 'a>, test: 'a -> Task<unit>, [<CallerFilePath>] ?filePath: string, [<CallerLineNumber>] ?lineNumber: int) : Test =
        let cases = ImmutableArray.CreateRange(cases)
        let casesImpl = CasesAsync(name, defaultArg filePath "", defaultArg lineNumber 0, cases, test)
        Test.ICasesAsync casesImpl

/// A collection of Tests, in VS Test Explorer corresponding to a `Class`.
type TestList(name: string, tests: IReadOnlyCollection<Test>, [<Struct>] ?oneTimeSetup: unit -> unit) =
    member _.Name = name
    member _.Tests = tests
    member _.OneTimeSetup: (unit -> unit) voption = oneTimeSetup

/// A collection of test lists, in VS Test Explorer corresponding to a `Namespace`.
type TestFolder(namespaceName: string, testLists: IReadOnlyCollection<TestList>, [<Struct>] ?oneTimeSetup: unit -> unit) =
    member _.NamespaceName = namespaceName
    member _.TestLists = testLists
    member _.OneTimeSetup: (unit -> unit) voption = oneTimeSetup
