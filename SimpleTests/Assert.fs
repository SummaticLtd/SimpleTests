namespace SimpleTests

open System
open System.Collections.Generic

type Assert =
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member private Suffix(errorMsg: string option) =
        match errorMsg with
        | None -> ""
        | Some msg -> Environment.NewLine + msg
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member Equal<'a when 'a: equality and 'a: not null>(expected: 'a, actual: 'a, ?errorMsg: string) =
        if expected <> actual then
            failwith(
                "Expected: " + expected.ToString() + Environment.NewLine
                + "But was: " + actual.ToString() + Assert.Suffix errorMsg
            )
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member NotEqual<'a when 'a: equality and 'a: not null>(x: 'a, y: 'a, ?errorMsg: string) =
        if x = y then
            failwith(
                "Expected to be different but were equal: " + x.ToString() + ", " + y.ToString()
                + Assert.Suffix errorMsg
            )
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member ApproxEqual(expected: float, actual: float, margin: float, ?errorMsg: string) =
        // NaN fails every comparison, so without this a NaN operand would satisfy the tolerance.
        let nanInvolved = Double.IsNaN expected || Double.IsNaN actual
        if nanInvolved || abs(expected - actual) > margin then
            failwith(
                "Expected: " + expected.ToString() + Environment.NewLine
                + "But was: " + actual.ToString()
                + (if nanInvolved then Environment.NewLine + "NaN is never within a tolerance." else "")
                + Assert.Suffix errorMsg
            )
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member CollectionEqual<'a when 'a :> IEquatable<'a>>(expected: IReadOnlyCollection<'a>, actual: IReadOnlyCollection<'a>, ?errorMsg: string) =
        if expected.Count <> actual.Count || ((expected, actual) ||> Seq.exists2(fun exp act -> not ((exp :> IEquatable<'a>).Equals act))) then
            failwith(
                "Collections do not match." + Environment.NewLine
                + "Expect: " + (expected |> Seq.map string |> String.concat ", ") + Environment.NewLine
                + "Actual: " + (actual |> Seq.map string |> String.concat ", ") + Assert.Suffix errorMsg
            )
    /// Assumes expected is distinct. Checks that actual consists of the same elements as expected, up to rearrangement.
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member CollectionSetEqual<'a when 'a :> IEquatable<'a>>(expected: IReadOnlyCollection<'a>, actual: IReadOnlyCollection<'a>, ?errorMsg: string) =
        if expected.Count <> actual.Count || expected |> Seq.exists(fun exp -> actual |> Seq.exists(fun act -> (exp :> IEquatable<'a>).Equals act) |> not) then
            failwith(
                "Collection sets do not match (up to reorder)." + Environment.NewLine
                + "Expect: " + (expected |> Seq.map string |> String.concat ", ") + Environment.NewLine
                + "Actual: " + (actual |> Seq.map string |> String.concat ", ") + Assert.Suffix errorMsg
            )
    /// Fails listing every item, so one run shows the whole problem rather than its first case.
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member Empty<'a>(items: seq<'a>, ?errorMsg: string) =
        let items = List.ofSeq items
        if not items.IsEmpty then
            failwith(
                "Expected empty but had " + string items.Length + ":"
                + (match errorMsg with None -> "" | Some msg -> " " + msg) + Environment.NewLine
                + String.Join(Environment.NewLine, items |> Seq.map string)
            )
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member True(condition: bool, ?errorMsg: string) =
        if not condition then
            failwith("Expected condition to be true but was false." + Assert.Suffix errorMsg)
    /// Avoids building the message unless the assertion fails.
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member TrueWithGeneratedError(condition: bool, errorMsg: unit -> string) =
        if not condition then failwith(errorMsg())
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member Throws(f: unit -> unit, ?errorMsg: string) =
        let mutable threw = false
        try f() with _ -> threw <- true
        if not threw then
            failwith("Expected an exception but none was raised." + Assert.Suffix errorMsg)
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member private Rel<'a when 'a: not null>(relation: ('a * 'a -> bool), relationStr: string, first: 'a, second: 'a, ?errorMsg: string) =
        if not (relation(first, second)) then
            failwith(
                "Expected: first " + relationStr + " second." + Environment.NewLine
                + "First: " + first.ToString() + ", second: " + second.ToString() + Assert.Suffix errorMsg
            )
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member LessOrEqual<'a when 'a: not null and 'a: comparison>(first: 'a, second: 'a, ?errorMsg: string) =
        Assert.Rel((fun (a, b) -> a <= b), "<=", first, second, ?errorMsg = errorMsg)
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member Less<'a when 'a: not null and 'a: comparison>(first: 'a, second: 'a, ?errorMsg: string) =
        Assert.Rel((fun (a, b) -> a < b), "<", first, second, ?errorMsg = errorMsg)
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member GreaterOrEqual<'a when 'a: not null and 'a: comparison>(first: 'a, second: 'a, ?errorMsg: string) =
        Assert.Rel((fun (a, b) -> a >= b), ">=", first, second, ?errorMsg = errorMsg)
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member Greater<'a when 'a: not null and 'a: comparison>(first: 'a, second: 'a, ?errorMsg: string) =
        Assert.Rel((fun (a, b) -> a > b), ">", first, second, ?errorMsg = errorMsg)
    [<Diagnostics.DebuggerHidden; Diagnostics.StackTraceHidden>]
    static member Fail(errorMsg: string) = failwith errorMsg
