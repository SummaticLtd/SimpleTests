module Program

open System.Threading.Tasks
open SimpleTests

let validatePositive (n: int) : unit =
    if n <= 0 then failwith $"{n} is not positive"

let checkSum (a: int, b: int, expected: int) : unit =
    let actual: int = a + b
    validatePositive actual
    if actual <> expected then failwith $"Expected {expected} but got {actual}"

let checkSumAsync (a: int, b: int, expected: int) : Task<unit> =
    task {
        do! Task.Delay(10)
        let actual: int = a + b
        validatePositive actual
        if actual <> expected then failwith $"Expected {expected} but got {actual}"
    }

let additionData: (string * (int * int * int)) list =
    [ "1 + 2 = 3", (1, 2, 3)
      "1 + 1 = 3 (wrong)", (1, 1, 3) ]

let testFolders: TestFolder list =
    [
        TestFolder(
           "MathTests",
           [  TestList(
                  "SyncTests",
                  [  Test.Sync(
                         "passing sync test",
                         fun () -> checkSum(2, 2, 4)
                     )
                     Test.Sync(
                         "failing sync test",
                         fun () -> checkSum(1, -5, 10)
                     )
                     Test.IgnoredSync(
                         "ignored sync test",
                         fun () -> checkSum(2, 2, 4)
                     )
                  ]
              )
              TestList(
                  "AsyncTests",
                  [  Test.Async(
                         "passing async test",
                         fun () ->
                             task {
                                 do! Task.Delay(10)
                                 System.Console.WriteLine("Async test completed successfully")
                             }
                     )
                     Test.Async(
                         "failing async test",
                         fun () -> checkSumAsync(1, -5, 10)
                     )
                     Test.IgnoredAsync(
                         "ignored async test",
                         fun () -> checkSumAsync(2, 2, 4)
                     )
                  ]
              )
              TestList(
                  "DataDrivenTests",
                  [  Test.CasesSync(
                         "sync addition",
                         additionData,
                         fun (a, b, expected) -> checkSum(a, b, expected)
                     )
                     Test.CasesAsync(
                         "async addition",
                         additionData,
                         fun (a, b, expected) -> checkSumAsync(a, b, expected)
                     )
                  ]
              )
           ]
       )
    ]

[<EntryPoint>]
let main (args: string array) : int =
    Runner.Run(args, testFolders)