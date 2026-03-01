namespace SimpleTests

open System.Threading.Tasks
open System.Collections.Generic

type Test =
    | Sync of uniqueName: string * run: (unit -> Result<unit, string>)
    | Async of uniqueName: string * run: (unit -> Task<Result<unit, string>>)

type TestList(tests: IReadOnlyCollection<Test>) =
    member _.Tests = tests