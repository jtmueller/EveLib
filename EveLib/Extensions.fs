#nowarn "77"
namespace EveLib.FSharp

open System
open System.Collections.Generic
open System.Xml.Linq
open System.Net

[<AutoOpen>]
module Extensions =

    let isNull (o:obj) = Object.ReferenceEquals(o, null)
    let isNotNull : obj -> bool = (isNull >> not)

    let ns = XNamespace.op_Implicit
    let xn = XName.op_Implicit
    let inline xval (x : ^a when ^a :> XObject) : ^b = ((^a or ^b) : (static member op_Explicit : ^a -> ^b) x)
    let inline xopt (x: #XAttribute) =
        if isNull x then None
        elif String.IsNullOrEmpty(x.Value) then None
        else Some(xval x)

    let inline xelv name (parent:XElement) =
        let el = parent.Element(xn name)
        if isNull el then failwithf "Element not found: %s" name
        else xval el

    let inline xelvo name (parent:XElement) =
        let el = parent.Element(xn name)
        if isNull el then None
        else Some(xval el)

    let (|Element|_|) name (node:XElement) =
        if isNull node then
            None
        elif node.Name.LocalName = name then
            Some node
        else
            None

    let (|Attribute|_|) name (attr:XAttribute) =
        if isNull attr then
            None
        elif attr.Name.LocalName = name then
            Some attr
        else
            None
    
    type WebRequest with
        member x.AsyncGetRequestStream() =
            Async.FromBeginEnd(x.BeginGetRequestStream, x.EndGetRequestStream)
