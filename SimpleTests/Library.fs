namespace SimpleTests

open System.Collections.Generic
open System.Reflection
open System.Threading.Tasks
open Microsoft.Testing.Platform.Capabilities.TestFramework
open Microsoft.Testing.Platform.Extensions
open Microsoft.Testing.Platform.Extensions.Messages
open Microsoft.Testing.Platform.Extensions.TestFramework
open Microsoft.Testing.Platform.Requests

// -- Capabilities (none needed for this demo) --
type SimpleCapabilities() =
    interface ITestFrameworkCapabilities with
        member _.Capabilities = [||]

// -- Test framework --
type SimpleFramework(testFolders: IReadOnlyCollection<TestFolder>) as self =
    let assemblyName = Assembly.GetEntryAssembly().GetName().Name

    let methodIdentifier (ns: string, typeName: string, methodName: string) : TestMethodIdentifierProperty =
        TestMethodIdentifierProperty(assemblyName, ns, typeName, methodName, 0, [||], "System.Void")

    let makeTestNode (uid: string, displayName: string, properties: IProperty array) : TestNode =
        TestNode(Uid = TestNodeUid(uid), DisplayName = displayName, Properties = PropertyBag(properties))

    interface IExtension with
        member _.Uid = "SimpleFramework"
        member _.Version = "1.0.0"
        member _.DisplayName = "Simple Framework"
        member _.Description = "Minimal direct Microsoft.Testing.Platform demo"
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
                                let fromCases(name: string, cases: IReadOnlyCollection<string * 'a>) = task {
                                    for caseName, _ in cases do
                                        let displayName = $"{name}({caseName})"
                                        let uid = $"{ns}.{listName}.{displayName}"
                                        let node = makeTestNode(uid, displayName, [| DiscoveredTestNodeStateProperty(); methodIdentifier(ns, listName, name) |])
                                        let message = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                }
                                match test with
                                | Test.Sync(name, _) | Test.Async(name, _) ->
                                    let uid = $"{ns}.{listName}.{name}"
                                    let node = makeTestNode(uid, name, [| DiscoveredTestNodeStateProperty(); methodIdentifier(ns, listName, name) |])
                                    let message = TestNodeUpdateMessage(sessionUid, node)
                                    do! context.MessageBus.PublishAsync(self, message)
                                | Test.CasesSync(name, cases) ->
                                    do! fromCases(name, cases)
                                | Test.CasesAsync(name, cases) ->
                                    do! fromCases(name, cases)

                | :? RunTestExecutionRequest as request ->
                    let sessionUid = request.Session.SessionUid
                    for folder in testFolders do
                        let ns = folder.NamespaceName
                        for testList in folder.TestLists do
                            let listName = testList.Name
                            for test in testList.Tests do
                                match test with
                                | Test.Sync(name, run) ->
                                    let stateProperty: IProperty =
                                        try
                                            run ()
                                            PassedTestNodeStateProperty(name)
                                        with ex ->
                                            FailedTestNodeStateProperty(ex, ex.Message)
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    let node: TestNode = makeTestNode(uid, name, [| stateProperty; methodIdentifier(ns, listName, name) |])
                                    let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                    do! context.MessageBus.PublishAsync(self, message)
                                | Test.Async(name, run) ->
                                    let! stateProperty: IProperty =
                                        task {
                                            try
                                                do! run ()
                                                return PassedTestNodeStateProperty(name) :> IProperty
                                            with ex ->
                                                return FailedTestNodeStateProperty(ex, ex.Message) :> IProperty
                                        }
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    let node: TestNode = makeTestNode(uid, name, [| stateProperty; methodIdentifier(ns, listName, name) |])
                                    let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                    do! context.MessageBus.PublishAsync(self, message)
                                | Test.CasesSync(name, cases) ->
                                    for caseName, run in cases do
                                        let displayName: string = $"{name}({caseName})"
                                        let stateProperty: IProperty =
                                            try
                                                run ()
                                                PassedTestNodeStateProperty(displayName)
                                            with ex ->
                                                FailedTestNodeStateProperty(ex, ex.Message)
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        let node: TestNode = makeTestNode(uid, displayName, [| stateProperty; methodIdentifier(ns, listName, name) |])
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                | Test.CasesAsync(name, cases) ->
                                    for caseName, run in cases do
                                        let displayName: string = $"{name}({caseName})"
                                        let! stateProperty: IProperty =
                                            task {
                                                try
                                                    do! run ()
                                                    return PassedTestNodeStateProperty(displayName) :> IProperty
                                                with ex ->
                                                    return FailedTestNodeStateProperty(ex, ex.Message) :> IProperty
                                            }
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        let node: TestNode = makeTestNode(uid, displayName, [| stateProperty; methodIdentifier(ns, listName, name) |])
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                | _ -> ()

                context.Complete()
            }
