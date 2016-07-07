namespace BespokeJS.Library

open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html

[<JavaScript>]
module Client =
    open Domain

    let main (config: Configuration) =
        div [ text config.Greeting ] 