namespace SimpleTests

open System
open System.Collections.Generic

type Assert =
    [<Diagnostics.DebuggerHidden>]
    static member Equal<'a when 'a:equality and 'a: not null>(expected:'a, actual:'a, ?errorMsg:string) =
        if expected <> actual
        then
            let suffix = match errorMsg with None -> "" | Some msg -> Environment.NewLine + msg
            failwith(
                "Expected: " + expected.ToString() + Environment.NewLine
                + "But was: " + actual.ToString() + suffix
            )
    [<Diagnostics.DebuggerHidden>]
    static member NotEqual<'a when 'a:equality and 'a: not null>(x: 'a, y: 'a, ?errorMsg:string) =
        if x = y
        then
            let suffix = match errorMsg with None -> "" | Some msg -> Environment.NewLine + msg
            failwith(
                "Expected to be equal but were different: " + x.ToString() + ", " + y.ToString() + suffix
            )
    [<Diagnostics.DebuggerHidden>]
    static member ApproxEqual(expected:float, actual:float, margin:float, ?errorMsg:string) =
        if abs(expected - actual) > margin
        then
            let suffix = match errorMsg with None -> "" | Some msg -> Environment.NewLine + msg
            failwith(
                "Expected: " + expected.ToString() + Environment.NewLine
                + "But was: " + actual.ToString() + suffix
            )
    static member CollectionEqual<'a when 'a:>IEquatable<'a>>(expected: IReadOnlyCollection<'a>, actual: IReadOnlyCollection<'a>, ?errorMsg:string) =
        if expected.Count <> actual.Count || ((expected, actual) ||> Seq.exists2(fun exp act -> not ((exp :> IEquatable<'a>).Equals act))) then
            let suffix = match errorMsg with None -> "" | Some msg -> Environment.NewLine + msg
            failwith(
                "Collections do not match." + Environment.NewLine
                + "Expect: " + (expected |> Seq.map string |> String.concat ", ") + Environment.NewLine
                + "Actual: " + (actual |> Seq.map string |> String.concat ", ") + suffix
            )
    /// Assumes expected is distinct. Checks that actual consists of the same elements as expected, up to rearrangement.
    static member CollectionSetEqual<'a when 'a:>IEquatable<'a>>(expected: IReadOnlyCollection<'a>, actual: IReadOnlyCollection<'a>, ?errorMsg:string) =
        if expected.Count <> actual.Count || expected |> Seq.exists(fun exp -> actual |> Seq.exists(fun act -> (exp :> IEquatable<'a>).Equals act) |> not) then
            let suffix = match errorMsg with None -> "" | Some msg -> Environment.NewLine + msg
            failwith(
                "Collection sets do not match (up to reorder)." + Environment.NewLine
                + "Expect: " + (expected |> Seq.map string |> String.concat ", ") + Environment.NewLine
                + "Actual: " + (actual |> Seq.map string |> String.concat ", ") + suffix
            )
    [<Diagnostics.DebuggerHidden>]
    static member TrueWithGeneratedError(condition: bool, errorMsg: unit -> string) =
        if not condition then
            failwith(errorMsg())
    static member True(condition: bool, ?errorMsg:string) =
        match errorMsg with
        | None -> if not condition then failwith "Expected condition to be true but was false."
        | Some em -> if not condition then failwith("Expected condition to be true but was false." + Environment.NewLine + em)
    static member private Rel<'a when 'a: not null>(relation: ('a * 'a -> bool), relationStr: string, first: 'a, second: 'a, ?errorMsg:string) =
        if not (relation(first, second)) then
            let suffix = match errorMsg with None -> "" | Some msg -> Environment.NewLine + msg
            failwith(
                "Expected: first " + relationStr + "second." + System.Environment.NewLine +
                "First:" + first.ToString() + ", second:" + second.ToString() + suffix
            )
    static member LessOrEqual<'a when 'a: not null and 'a: comparison>(first: 'a, second: 'a, ?errorMsg:string) =
        Assert.Rel((fun (a, b) -> a <= b), "<=", first, second, ?errorMsg = errorMsg)
    static member Less<'a when 'a: not null and 'a: comparison>(first: 'a, second: 'a, ?errorMsg:string) =
        Assert.Rel((fun (a, b) -> a < b), "<", first, second, ?errorMsg = errorMsg)
    static member GreaterOrEqual<'a when 'a: not null and 'a: comparison>(first: 'a, second: 'a, ?errorMsg:string) =
        Assert.Rel((fun (a, b) -> a >= b), ">=", first, second, ?errorMsg = errorMsg)
    static member Greater<'a when 'a: not null and 'a: comparison>(first: 'a, second: 'a, ?errorMsg:string) =
        Assert.Rel((fun (a, b) -> a > b), ">", first, second, ?errorMsg = errorMsg)
    static member Fail(errorMsg: string) = failwith errorMsg