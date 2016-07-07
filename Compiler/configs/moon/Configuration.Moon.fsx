module Configuration.ScriptRoot
#load @"..\_common\References.fsx"

open BespokeJS.Library
open BespokeJS.Library.Domain

let config = {
    Greeting = "Hello"
    Hello = Hi
}
    