namespace BespokeJS.SiteletTest

open WebSharper.Html.Server
open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.Sitelets


[<JavaScript>]
module Domain =
    type Test = {
        Greeting: string
        Hello: Hello
    }
    and Hello =
        | Hello
        | Hi
        | Hey

[<JavaScript>]
module Client =
    open Domain
    open WebSharper.UI.Next.Client

    let page hello =
        match hello.Hello with
        | Hello -> text ("Hello, " + hello.Greeting)
        | Hi -> text ("Hi, " + hello.Greeting + " " + (Json.Serialize(hello)))
        | Hey -> text ("Hey, " + hello.Greeting)

module Site =
    open Domain
    open WebSharper.UI.Next.Server
    open System.Text.RegularExpressions

    type Index = Templating.Template<"Main.html">
    
    let Main =
        let hello = {
            Greeting = "TEST TEST TEST"
            Hello = Hi
        }

        Sitelet.Sum 
            [
                Sitelet.Content "" "" (fun ctx -> 
                    
                    let x = client <@ Client.page hello @>
                    
                    let escape (s: string) =
                        Regex.Replace(s, @"[&<>']",
                            new MatchEvaluator(fun m ->
                                match m.Groups.[0].Value.[0] with
                                | '&'-> "&amp;"
                                | '<'-> "&lt;"
                                | '>' -> "&gt;"
                                | '\'' -> "&#39;"
                                | _ -> failwith "unreachable"))

                    let meta =
                        x.Encode(ctx.Metadata, ctx.Json)
                        |> WebSharper.Core.Json.Encoded.Object
                        |> ctx.Json.Pack
                        |> WebSharper.Core.Json.Stringify
                        |> escape

                    Index.Doc("Hello world", [ x ]) |> Content.Page)
                Sitelet.Content "test" "test" (fun _ -> Content.Text "test")
                Sitelet.Content "more" "more" (fun ctx -> 
                    Content.Text "more")
            ]


module SelfHostedServer =
    
    
    open System.IO
    open System.Text
    open System.Web.UI

    open global.Owin
    open Microsoft.Owin.Hosting
    open Microsoft.Owin.StaticFiles
    open Microsoft.Owin.FileSystems
    open WebSharper.Owin


    [<EntryPoint>]
    let Main args =

        let rootDirectory, url =
            match args with
            | [| rootDirectory; url |] -> rootDirectory, url
            | [| url |] -> "..", url
            | [| |] -> "..", "http://localhost:9000/"
            | _ -> eprintfn "Usage: BespokeJS.SiteletTest ROOT_DIRECTORY URL"; exit 1
    
        let options = 
            new WebSharperOptions<string>(
                Debug = true, 
                ServerRootDirectory = rootDirectory)
    
        use server = WebApp.Start(url, fun appB ->
            appB.UseStaticFiles(StaticFileOptions(FileSystem = PhysicalFileSystem(rootDirectory)))
                .UseWebSharper(options.WithSitelet(Site.Main)) |> ignore)
        stdout.WriteLine("Serving {0}", url)
        stdin.ReadLine() |> ignore
        0
