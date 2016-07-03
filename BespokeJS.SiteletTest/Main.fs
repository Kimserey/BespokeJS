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
        | Hi -> text ("Hi, " + hello.Greeting)
        | Hey -> text ("Hey, " + hello.Greeting)

module Site =
    open Domain
    open WebSharper.UI.Next.Server
    
    type Index = Templating.Template<"Main.html">
    
    let Main =
        let hello = {
            Greeting = "TEST TEST TEST"
            Hello = Hey
        }

        Sitelet.Sum 
            [
                Sitelet.Content "" "" (fun _ -> 
                    Index.Doc("Hello world", [ client <@ Client.page hello @> ]) |> Content.Page)
                Sitelet.Content "test" "test" (fun _ -> Content.Text "test")
                Sitelet.Content "more" "more" (fun _ -> Content.Text "more")
            ]


module SelfHostedServer =

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
