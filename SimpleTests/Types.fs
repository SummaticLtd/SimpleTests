namespace SimpleTests

open System.Threading.Tasks
open System.Collections.Generic
open System.ComponentModel
open System.Collections.Immutable

type ICasesSync =
    abstract member Name: string
    abstract member Names: IReadOnlyCollection<string>
    abstract member Runs: IReadOnlyCollection<string * (unit -> unit)>

type ICasesAsync =
    abstract member Name: string
    abstract member Names: IReadOnlyCollection<string>
    abstract member Runs: IReadOnlyCollection<string * (unit -> Task<unit>)>

type private CasesSync<'a>(name: string, cases: ImmutableArray<string * 'a>, test: 'a -> unit) =
    interface ICasesSync with
        member _.Name = name
        member _.Names = ImmutableArray.CreateRange(cases, fst)
        member _.Runs = ImmutableArray.CreateRange(cases, fun (caseName, x) -> (caseName, fun () -> test x))

type private CasesAsync<'a>(name: string, cases: ImmutableArray<string * 'a>, test: 'a -> Task<unit>) =
    interface ICasesAsync with
        member _.Name = name
        member _.Names = ImmutableArray.CreateRange(cases, fst)
        member _.Runs = ImmutableArray.CreateRange(cases, fun (caseName, x) -> (caseName, fun () -> test x))

[<RequireQualifiedAccess>]
type Test =
    | [<EditorBrowsable(EditorBrowsableState.Never)>]
        ѪSync of uniqueName: string * run: (unit -> unit) * ignored: bool
    | [<EditorBrowsable(EditorBrowsableState.Never)>]
        ѪAsync of uniqueName: string * run: (unit -> Task<unit>) * ignored: bool
    | [<EditorBrowsable(EditorBrowsableState.Never)>]
        ICasesSync of ICasesSync
    | [<EditorBrowsable(EditorBrowsableState.Never)>]
        ICasesAsync of ICasesAsync
module Test =
    let Sync(name: string, test: unit -> unit) : Test =
        Test.ѪSync(name, test, false)
    let IgnoredSync(name: string, test: unit -> unit) : Test =
        Test.ѪSync(name, test, true)
    let Async(name: string, test: unit -> Task<unit>) : Test =
        Test.ѪAsync(name, test, false)
    let IgnoredAsync(name: string, test: unit -> Task<unit>) : Test =
        Test.ѪAsync(name, test, true)
    let CasesSync<'a>(name: string, cases: IReadOnlyCollection<string * 'a>, test: 'a -> unit) : Test =
        let cases = ImmutableArray.CreateRange(cases)
        let casesImpl = CasesSync(name, cases, test)
        Test.ICasesSync casesImpl
    let CasesAsync<'a>(name: string, cases: IReadOnlyCollection<string * 'a>, test: 'a -> Task<unit>) : Test =
        let cases = ImmutableArray.CreateRange(cases)
        let casesImpl = CasesAsync(name, cases, test)
        Test.ICasesAsync casesImpl

/// A collection of Tests, in VS Test Explorer corresponding to a `Class`.
type TestList(name: string, tests: IReadOnlyCollection<Test>) =
    member _.Name = name
    member _.Tests = tests

/// A collection of test lists, in VS Test Explorer corresponding to a `Namespace`.
type TestFolder(namespaceName: string, testLists: IReadOnlyCollection<TestList>) =
    member _.NamespaceName = namespaceName
    member _.TestLists = testLists
