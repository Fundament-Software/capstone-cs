open Argu
open System
open System.Reflection
open Capnp
open Capnp.Schema
open SpectreCoff
open Fundament.Capstone.Compiler.Model
open Fundament.Capstone.Compiler

type Arguments =
    | [<First; Unique>] File of string
    | Version

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Version -> "Display the version of the compiler."
            | File _ -> "Read a binary-encoded code generation request from the file at the specified path."

let inputStreamResult =
    let VersionLine =
        $"Capstone C# Compiler Plugin Version {Assembly.GetExecutingAssembly().GetName().Version}"

    let HelpTextMessage =
        $"{VersionLine}\n\
        Normally invoked by the Capstone compiler, this program reads a binary-encoded code generation request from a file or stdin if no file is provided."

    let parser = ArgumentParser.Create<Arguments>()

    let parsedArguments = parser.ParseCommandLine(raiseOnUsage = false)

    if parsedArguments.IsUsageRequested then
        do printfn "%s" (parser.PrintUsage(HelpTextMessage))
        Error 1
    elif parsedArguments.Contains(Version) then
        do printfn "%s" VersionLine
        Error 1
    else
        let s =
            match parsedArguments.TryGetResult(File) with
            | None ->
                if not Console.IsInputRedirected then
                    do printfn $"{VersionLine}\nExpecting binary-encoded code generation request from stdin..."

                Console.OpenStandardInput()
            | Some(path) -> IO.File.OpenRead path

        Ok(s)

let inputStream =
    match inputStreamResult with
    | Ok(inputStream) -> inputStream
    | Error(code) -> exit code

let model =
    inputStream
    |> Framing.ReadSegments
    |> DeserializerState.CreateRoot
    |> CodeGeneratorRequest.READER.create
    |> buildModel

let displayTree =
    let nodeDisplay (n: Node) = node (Calm $"Node: %A{n}") []

    let folder (n: Node) =
        function
        | [] -> node (Calm $"Node: %A{n}") []
        | childNodes -> node (Calm $"Node: %A{n}") childNodes

    List.map (fun tree -> RoseTree.foldTree folder tree) model

tree (Pumped "Compilation Request") displayTree |> toOutputPayload |> toConsole
