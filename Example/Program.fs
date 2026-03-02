module Program

open SimpleTests

let testFolders =
    [
        TestFolder(
           "MathTests",
           [  TestList(
                  "Arithmetic",
                  [  Test.Sync(
                         "2 + 2 should equal 4",
                         fun () ->
                             if 2 + 2 <> 4 then failwith "Expected 2 + 2 to equal 4"
                     )
                  ]
              )
           ]
       )
    ]

[<EntryPoint>]
let main (args: string array) : int =
    Runner.run(args, testFolders)