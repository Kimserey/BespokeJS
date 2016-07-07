namespace BespokeJS.Library

open WebSharper

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