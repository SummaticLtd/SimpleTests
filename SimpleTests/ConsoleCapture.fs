namespace SimpleTests

open System
open System.IO
open System.Threading
open System.Threading.Tasks

module ConsoleCapture =

    let private asyncLocalWriter: AsyncLocal<StringWriter> = AsyncLocal<StringWriter>()
    let private originalConsoleOut: TextWriter = Console.Out

    do
        let delegating: TextWriter =
            {
                new TextWriter() with
                    override _.Encoding = Text.Encoding.UTF8
                    override _.Write(c: char) =
                        match asyncLocalWriter.Value with
                        | NonNull w -> w.Write(c)
                        | Null -> originalConsoleOut.Write(c)
                    override _.Write(s: string) =
                        match asyncLocalWriter.Value with
                        | NonNull w -> w.Write(s)
                        | Null -> originalConsoleOut.Write(s)
                    override _.WriteLine(s: string) =
                        match asyncLocalWriter.Value with
                        | NonNull w -> w.WriteLine(s)
                        | Null -> originalConsoleOut.WriteLine(s)
                    override _.Flush() =
                        match asyncLocalWriter.Value with
                        | NonNull w -> w.Flush()
                        | Null -> originalConsoleOut.Flush()
            }
        Console.SetOut(delegating)

    let captureOutput (action: unit -> unit) : exn option * string =
        use writer: StringWriter = new StringWriter()
        asyncLocalWriter.Value <- writer
        try
            action ()
            asyncLocalWriter.Value <- null
            (None, writer.ToString())
        with ex ->
            asyncLocalWriter.Value <- null
            (Some ex, writer.ToString())

    let captureOutputAsync (action: unit -> Task<unit>) : Task<exn option * string> =
        task {
            use writer: StringWriter = new StringWriter()
            asyncLocalWriter.Value <- writer
            try
                do! action ()
                asyncLocalWriter.Value <- null
                return (None, writer.ToString())
            with ex ->
                asyncLocalWriter.Value <- null
                return (Some ex, writer.ToString())
        }
