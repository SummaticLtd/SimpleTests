namespace SimpleTests

// StandardOutputProperty is marked [Experimental("TPEXP")] in Microsoft.Testing.Platform
#nowarn "57"

open System
open System.Collections.Generic
open System.Diagnostics
open System.Reflection
open System.Threading.Tasks
open Microsoft.Testing.Platform.Builder
open Microsoft.Testing.Platform.Capabilities.TestFramework
open Microsoft.Testing.Platform.Extensions
open Microsoft.Testing.Platform.Extensions.Messages
open Microsoft.Testing.Platform.Extensions.TestFramework
open Microsoft.Testing.Platform.Requests
open Microsoft.Testing.Extensions
open Microsoft.Testing.Extensions.TrxReport.Abstractions

// -- Capabilities --
type SimpleTrxCapability() =
    interface ITrxReportCapability with
        member _.IsSupported = true
        member _.Enable() = ()

type SimpleCapabilities() =
    interface ITestFrameworkCapabilities with
        member _.Capabilities = [| SimpleTrxCapability() |]

// -- Test framework --
type SimpleFramework(testFolders: IReadOnlyCollection<TestFolder>, [<Struct>] ?oneTimeSetup: unit -> unit) as self =
    let assemblyName = Assembly.GetEntryAssembly().GetName().Name

    let methodIdentifier (ns: string, typeName: string, methodName: string) : TestMethodIdentifierProperty =
        TestMethodIdentifierProperty(assemblyName, ns, typeName, methodName, 0, [||], "System.Void")

    let fileLocation (filePath: string, lineNumber: int) : TestFileLocationProperty =
        let pos: LinePosition = LinePosition(lineNumber, 0)
        TestFileLocationProperty(filePath, LinePositionSpan(pos, pos))

    let makeTestNode (uid: string, displayName: string, properties: IProperty array) : TestNode =
        TestNode(Uid = TestNodeUid(uid), DisplayName = displayName, Properties = PropertyBag(properties))

    let buildProperties (stateProperty: IProperty, methodId: IProperty, loc: IProperty, output: string, startTime: DateTimeOffset, elapsed: TimeSpan, failure: exn option) : IProperty array =
        let timing: IProperty = TimingProperty(TimingInfo(startTime, startTime + elapsed, elapsed))
        let trxException: IProperty array =
            match failure with
            | Some ex -> [| TrxExceptionProperty(ex.Message, ex.ToString()) |]
            | None -> [||]
        let standardOutput: IProperty array =
            if String.IsNullOrEmpty(output) then [||]
            else [| StandardOutputProperty(output) |]
        Array.concat [| [| stateProperty; methodId; loc; timing |]; trxException; standardOutput |]

    interface IExtension with
        member _.Uid = "SimpleTests"
        member _.Version = "0.1.0"
        member _.DisplayName = "SimpleTests"
        member _.Description = "Minimal direct testing framework based on Microsoft.Testing.Platform"
        member _.IsEnabledAsync() = Task.FromResult(true)

    interface IDataProducer with
        member _.DataTypesProduced = [| typeof<TestNodeUpdateMessage> |]

    interface ITestFramework with
        member _.CreateTestSessionAsync(_context) =
            Task.FromResult(CreateTestSessionResult(IsSuccess = true))

        member _.CloseTestSessionAsync(_context) =
            Task.FromResult(CloseTestSessionResult(IsSuccess = true))

        member _.ExecuteRequestAsync(context) =
            task {
                match context.Request with
                | :? DiscoverTestExecutionRequest as request ->
                    let sessionUid = request.Session.SessionUid
                    for folder in testFolders do
                        let ns = folder.NamespaceName
                        for testList in folder.TestLists do
                            let listName = testList.Name
                            for test in testList.Tests do
                                let discoverCases(name: string, caseNames: IReadOnlyCollection<string>, filePath: string, lineNumber: int) : Task = task {
                                    for caseName in caseNames do
                                        let displayName: string = $"{name}({caseName})"
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        let node: TestNode = makeTestNode(uid, displayName, [| DiscoveredTestNodeStateProperty(); methodIdentifier(ns, listName, name); fileLocation(filePath, lineNumber) |])
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                }
                                match test with
                                | Test.ѪSync(name, _, _, filePath, lineNumber) | Test.ѪAsync(name, _, _, filePath, lineNumber) ->
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    let node: TestNode = makeTestNode(uid, name, [| DiscoveredTestNodeStateProperty(); methodIdentifier(ns, listName, name); fileLocation(filePath, lineNumber) |])
                                    let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                    do! context.MessageBus.PublishAsync(self, message)
                                | Test.ICasesSync(cases, _) ->
                                    do! discoverCases(cases.Name, cases.Names, cases.FilePath, cases.LineNumber)
                                | Test.ICasesAsync(cases, _) ->
                                    do! discoverCases(cases.Name, cases.Names, cases.FilePath, cases.LineNumber)

                | :? RunTestExecutionRequest as request ->
                    let sessionUid = request.Session.SessionUid
                    let filterUids: HashSet<string> option =
                        match request.Filter with
                        | :? TestNodeUidListFilter as f ->
                            Some(HashSet(f.TestNodeUids |> Array.map (fun uid -> uid.Value)))
                        | _ -> None
                    let shouldRun (uid: string) : bool =
                        match filterUids with
                        | Some uids -> uids.Contains(uid)
                        | None -> true
                    oneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                    for folder in testFolders do
                        let ns = folder.NamespaceName
                        folder.OneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                        for testList in folder.TestLists do
                            let listName = testList.Name
                            testList.OneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                            for test in testList.Tests do
                                match test with
                                | Test.ѪSync(name, run, ignored, filePath, lineNumber) ->
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    if shouldRun uid then
                                        let startTime: DateTimeOffset = DateTimeOffset.UtcNow
                                        let sw: Stopwatch = Stopwatch.StartNew()
                                        let (stateProperty: IProperty), (output: string), (failure: exn option) =
                                            if ignored then
                                                (SkippedTestNodeStateProperty("Ignored") :> IProperty), "", None
                                            else
                                                match ConsoleCapture.captureOutput run with
                                                | None, captured -> (PassedTestNodeStateProperty(name) :> IProperty), captured, None
                                                | Some ex, captured -> (FailedTestNodeStateProperty(ex, ex.Message) :> IProperty), captured, Some ex
                                        sw.Stop()
                                        let methodId: IProperty = methodIdentifier(ns, listName, name)
                                        let loc: IProperty = fileLocation(filePath, lineNumber)
                                        let node: TestNode = makeTestNode(uid, name, buildProperties(stateProperty, methodId, loc, output, startTime, sw.Elapsed, failure))
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        context.MessageBus.PublishAsync(self, message).GetAwaiter().GetResult()
                                | Test.ѪAsync(name, run, ignored, filePath, lineNumber) ->
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    if shouldRun uid then
                                        let startTime: DateTimeOffset = DateTimeOffset.UtcNow
                                        let sw: Stopwatch = Stopwatch.StartNew()
                                        let! (stateProperty: IProperty), (output: string), (failure: exn option) =
                                            task {
                                                if ignored then
                                                    return (SkippedTestNodeStateProperty("Ignored") :> IProperty), "", None
                                                else
                                                    let! (failure, captured) = ConsoleCapture.captureOutputAsync run
                                                    match failure with
                                                    | None -> return (PassedTestNodeStateProperty(name) :> IProperty), captured, None
                                                    | Some ex -> return (FailedTestNodeStateProperty(ex, ex.Message) :> IProperty), captured, Some ex
                                            }
                                        sw.Stop()
                                        let methodId: IProperty = methodIdentifier(ns, listName, name)
                                        let loc: IProperty = fileLocation(filePath, lineNumber)
                                        let node: TestNode = makeTestNode(uid, name, buildProperties(stateProperty, methodId, loc, output, startTime, sw.Elapsed, failure))
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                | Test.ICasesSync(cases, isParallel) ->
                                    let name: string = cases.Name
                                    let loc: IProperty = fileLocation(cases.FilePath, cases.LineNumber) :> IProperty
                                    let methodId: IProperty = methodIdentifier(ns, listName, name)
                                    let runCase (caseName: string, run: unit -> unit) =
                                        let displayName: string = $"{name}({caseName})"
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        if shouldRun uid then
                                            let startTime: DateTimeOffset = DateTimeOffset.UtcNow
                                            let sw: Stopwatch = Stopwatch.StartNew()
                                            let (stateProperty: IProperty), (output: string), (failure: exn option) =
                                                match ConsoleCapture.captureOutput run with
                                                | None, captured -> (PassedTestNodeStateProperty(displayName) :> IProperty), captured, None
                                                | Some ex, captured -> (FailedTestNodeStateProperty(ex, ex.Message) :> IProperty), captured, Some ex
                                            sw.Stop()
                                            let node: TestNode = makeTestNode(uid, displayName, buildProperties(stateProperty, methodId, loc, output, startTime, sw.Elapsed, failure))
                                            let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                            context.MessageBus.PublishAsync(self, message).GetAwaiter().GetResult()
                                    if isParallel then
                                        Parallel.ForEach(cases.Runs, fun (caseName, run) -> runCase(caseName, run)) |> ignore
                                    else
                                        for caseName, run in cases.Runs do
                                            runCase(caseName, run)
                                | Test.ICasesAsync(cases, isParallel) ->
                                    let name: string = cases.Name
                                    let loc: IProperty = fileLocation(cases.FilePath, cases.LineNumber) :> IProperty
                                    let methodId: IProperty = methodIdentifier(ns, listName, name)
                                    let runCase (caseName: string, run: unit -> Task<unit>) : Task =
                                        task {
                                            let displayName: string = $"{name}({caseName})"
                                            let uid: string = $"{ns}.{listName}.{displayName}"
                                            if shouldRun uid then
                                                let startTime: DateTimeOffset = DateTimeOffset.UtcNow
                                                let sw: Stopwatch = Stopwatch.StartNew()
                                                let! (stateProperty: IProperty), (output: string), (failure: exn option) =
                                                    task {
                                                        let! (failure, captured) = ConsoleCapture.captureOutputAsync run
                                                        match failure with
                                                        | None -> return (PassedTestNodeStateProperty(displayName) :> IProperty), captured, None
                                                        | Some ex -> return (FailedTestNodeStateProperty(ex, ex.Message) :> IProperty), captured, Some ex
                                                    }
                                                sw.Stop()
                                                let node: TestNode = makeTestNode(uid, displayName, buildProperties(stateProperty, methodId, loc, output, startTime, sw.Elapsed, failure))
                                                let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                                do! context.MessageBus.PublishAsync(self, message)
                                        }
                                    if isParallel then
                                        do! Parallel.ForEachAsync(cases.Runs, fun (caseName, run) _ct -> ValueTask(runCase(caseName, run)))
                                    else
                                        for caseName, run in cases.Runs do
                                            do! runCase(caseName, run)
                | _ -> ()

                context.Complete()
            }

type Runner =
    static member Run(args: string array, testFolders: IReadOnlyCollection<TestFolder>, ?oneTimeSetup: unit -> unit) : int =
        task {
            let! builder: ITestApplicationBuilder = TestApplication.CreateBuilderAsync(args)

            builder.RegisterTestFramework(
                (fun _ -> SimpleCapabilities() :> ITestFrameworkCapabilities),
                (fun _ _ ->
                    match oneTimeSetup with
                    | Some setup -> SimpleFramework(testFolders, oneTimeSetup = setup) :> ITestFramework
                    | None -> SimpleFramework(testFolders) :> ITestFramework)
            )
            |> ignore

            builder.AddTrxReportProvider() |> ignore

            use! app = builder.BuildAsync()
            return! app.RunAsync()
        }
        |> fun t -> t.GetAwaiter().GetResult()