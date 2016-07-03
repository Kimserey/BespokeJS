namespace BespokeJS.SiteletTest

open WebSharper.Html.Server
open WebSharper
open WebSharper.Sitelets

module Site =


    let Main =
        Sitelet.Sum 
            [
                Sitelet.Content "" "" (fun _ -> Content.Text "hello world")
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
        use server = WebApp.Start(url, fun appB ->
            appB.UseStaticFiles(
                    StaticFileOptions(
                        FileSystem = PhysicalFileSystem(rootDirectory)))
                .UseSitelet(rootDirectory, Site.Main)
            |> ignore)
        stdout.WriteLine("Serving {0}", url)
        stdin.ReadLine() |> ignore
        0
