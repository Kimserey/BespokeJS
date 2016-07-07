namespace BespokeJS.SiteletTest

open WebSharper.Html.Server
open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.Sitelets

[<JavaScript>]
module Domain =

    type Configuration = {
        Greeting: string
        Hello: Hello
    }
    and Hello =
        | Hello
        | Hi
        | Hey