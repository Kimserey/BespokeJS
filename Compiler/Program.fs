namespace Singapore.Ui.Common
open System

module Compiler =
    open System.Reflection

    /// Code from WebSharper.Warp
    /// https://github.com/intellifactory/websharper.warp/blob/master/WebSharper.Warp/Warp.fs
    ///
    /// Warp compiles the current assembly to WebSharper and 
    /// then produce and save scripts and content files
    /// and finally return the WebSharper metadata of the assembly
    module Warp = 
        open WebSharper
        open WebSharper.Sitelets
        open System
        open System.IO
        open IntelliFactory.Core
        
        module PC = WebSharper.PathConventions
        module FE = WebSharper.Compiler.FrontEnd

        let compile asm =
            let localDir = Directory.GetCurrentDirectory()
            let websharperDir = Path.GetDirectoryName typeof<Sitelet<_>>.Assembly.Location
            let fsharpDir = Path.GetDirectoryName typeof<option<_>>.Assembly.Location
        
            let loadPaths =
                [
                    localDir
                    websharperDir
                    fsharpDir
                ]
            let loader =
                let aR =
                    AssemblyResolver.Create()
                        .SearchDirectories(loadPaths)
                        .WithBaseDirectory(fsharpDir)
                FE.Loader.Create aR stderr.WriteLine

            let refs =
                [
                    for dll in Directory.EnumerateFiles(websharperDir, "*.dll") do
                        if Path.GetFileName(dll) <> "FSharp.Core.dll" then
                            yield dll
                    let dontRef (n: string) =
                        [
                            "FSharp.Compiler.Interactive.Settings,"
                            "FSharp.Compiler.Service,"
                            "FSharp.Core,"
                            "FSharp.Data.TypeProviders,"
                            "Mono.Cecil"
                            "mscorlib,"
                            "System."
                            "System,"
                        ] |> List.exists n.StartsWith
                    let rec loadRefs (asms: Assembly[]) (loaded: Map<string, Assembly>) =
                        let refs =
                            asms
                            |> Seq.collect (fun asm -> asm.GetReferencedAssemblies())
                            |> Seq.map (fun n -> n.FullName)
                            |> Seq.distinct
                            |> Seq.filter (fun n -> not (dontRef n || Map.containsKey n loaded))
                            |> Seq.choose (fun n ->
                                try Some (AppDomain.CurrentDomain.Load n)
                                with _ -> None)
                            |> Array.ofSeq
                        if Array.isEmpty refs then
                            loaded
                        else
                            (loaded, refs)
                            ||> Array.fold (fun loaded ref -> loaded |> Map.add ref.FullName ref)
                            |> loadRefs refs
                    let asms =
                        AppDomain.CurrentDomain.GetAssemblies()
                        |> Array.filter (fun a -> not (dontRef a.FullName))
                    yield! asms
                    |> Array.map (fun asm -> asm.FullName, asm)
                    |> Map.ofArray
                    |> loadRefs asms
                    |> Seq.choose (fun (KeyValue(_, asm)) ->
                        try Some asm.Location
                        with :? NotSupportedException ->
                            // The dynamic assembly does not support `.Location`.
                            // No problem, if it's from the dynamic assembly then
                            // it doesn't incur a dependency anyway.
                            None)
                ]
                |> Seq.distinctBy Path.GetFileName
                |> Seq.choose (fun x ->
                    try
                        Some (loader.LoadFile x)
                    with ex -> 
                        printfn "%s" ex.Message
                        None)
                |> Seq.toList
            let opts = { FE.Options.Default with References = refs }
            let compiler = FE.Prepare opts (eprintfn "%O")
        

            compiler.Compile(<@ () @>, context = asm)
            |> Option.map (fun asm -> asm, refs)

        let outputFiles root (refs: Compiler.Assembly list) =
            let pc = PC.PathUtility.FileSystem(root)
            let writeTextFile path contents =
                Directory.CreateDirectory (Path.GetDirectoryName path) |> ignore
                File.WriteAllText(path, contents)
            let writeBinaryFile path contents =
                Directory.CreateDirectory (Path.GetDirectoryName path) |> ignore
                File.WriteAllBytes(path, contents)
            let emit text path =
                match text with
                | Some text -> writeTextFile path text
                | None -> ()
            let script = PC.ResourceKind.Script
            let content = PC.ResourceKind.Content

            for a in refs do
                let aid = PC.AssemblyId.Create(a.FullName)
                emit a.ReadableJavaScript (pc.JavaScriptPath aid)
                emit a.CompressedJavaScript (pc.MinifiedJavaScriptPath aid)
                let writeText k fn c =
                    let p = pc.EmbeddedPath(PC.EmbeddedResource.Create(k, aid, fn))
                    writeTextFile p c
                let writeBinary k fn c =
                    let p = pc.EmbeddedPath(PC.EmbeddedResource.Create(k, aid, fn))
                    writeBinaryFile p c
                for r in a.GetScripts() do
                    writeText script r.FileName r.Content
                for r in a.GetContents() do
                    writeBinary content r.FileName (r.GetContentData())

        let private (+/) x y = Path.Combine(x, y)

        let outputFile root (asm: FE.CompiledAssembly) =
            let dir = root +/ "Scripts" +/ "WebSharper"
            Directory.CreateDirectory(dir) |> ignore
            File.WriteAllText(dir +/ "WebSharper.EntryPoint.js", asm.ReadableJavaScript)
            File.WriteAllText(dir +/ "WebSharper.EntryPoint.min.js", asm.CompressedJavaScript)

        /// Compiles the caller assembly to WebSharper and unpack \Contents and \Scripts in root folder.
        /// When compiling .fsx, must be called from the .fsx.
        let compileAndUnpack root =
            match compile (Assembly.GetCallingAssembly()) with
            | Some (asm, refs) ->
                outputFiles root refs
                outputFile root asm
                asm.Info
            | None -> failwith "Failed to compile with WebSharper."

module Main =
    open System
    open System.IO
    open System.Text
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open Microsoft.FSharp.Compiler.Interactive.Shell
    
    type FsiEvaluationSession with
        member x.ReferenceDll filename = x.EvalInteraction("#r @\"" + filename + "\"")
        member x.ReferenceDlls filenames = filenames |> Seq.iter x.ReferenceDll
    
    let compile<'expected>() =
        let sbOut = new StringBuilder()
        let sbErr = new StringBuilder()
        let inStream = new StringReader("")
        let outStream = new StringWriter(sbOut)
        let errStream = new StringWriter(sbErr)

        let allArgs = [|"--noninteractive"; "--define:HOSTED" |]

        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
        use fsiSession = FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream, collectible = false)

        fsiSession.ReferenceDll (Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "BespokeJS.Library.dll"))

        // Run the main script.
        fsiSession.EvalScript( "configs/moon/Configuration.Moon.fsx")

        // Now eval the expression to, presumably, retrieve some result object that the main script computed.
        match fsiSession.EvalExpression("Configuration.ScriptRoot.config") with
        | Some v -> 
            let x = v.ReflectionValue :?> 'expected
            ()
        | None -> failwith "Coudln't evaluate config."

    [<EntryPoint>]
    let main argv = 
        printfn "%A" argv
        let x = compile<BespokeJS.Library.Domain.Configuration>()

        0 // return an integer exit code
