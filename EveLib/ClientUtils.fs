namespace EveLib

open System
open System.Net
open System.Text
open System.Xml
open System.Xml.Linq
open EveLib.FSharp

type Row(el:XElement) =
    member x.Element = el
    member x.Item with get key = el.Attribute(xn key).Value
    member x.NestedRowset =
        // TODO: some rows can contain multiple rowsets, plus other stuff nested inside
        match el.Element(xn "rowset") with
        | null -> None
        | rs   -> Some(RowSet(rs))
    static member (?) (row:Row, key:string) = row.Element.Attribute(xn key)

and RowSet(el:XElement) =
    do if el.Name.LocalName <> "rowset" then failwith ("Unexpected tag: " + el.Name.LocalName)
    member x.Name = el.Attribute(xn "name").Value
    member x.Key = el.Attribute(xn "key").Value
    member x.Columns = el.Attribute(xn "columns").Value.Split(',')
    member x.Rows = el.Elements(xn "row") |> Seq.map (fun r -> Row(r))

type EveServerError(code:int, msg:string) =
    inherit ApplicationException(msg)
    member x.ErrorCode = code

module internal ClientUtils =
    let baseUri = Uri("https://api.eveonline.com")

    let private encode = Uri.EscapeDataString

    type ParseResult = {
        QueryTime : DateTimeOffset
        CachedUntil : DateTimeOffset
        Result : XElement
    }

    let private parseDateEl (el:XElement) =
        if isNull el then
            DateTimeOffset.MinValue
        else
            el.Value + " +0"  // eve server time is GMT
            |> DateTimeOffset.Parse

    let parseDoc (doc:XDocument) =
        match doc.Root, doc.Root.Attribute(xn "version") with
        | Element "eveapi" root, Attribute "version" ver when ver.Value = "2" ->
            match root.Element(xn "error"), root.Element(xn "result") with
            | Element "error" error, _ ->
                raise <| EveServerError(error.Attribute(xn "code") |> int, error.Value)
            | _, Element "result" result ->
                let queryTime = root.Element(xn "currentTime") |> parseDateEl
                let cachedUntil = root.Element(xn "cachedUntil") |> parseDateEl
                { QueryTime = queryTime; CachedUntil = cachedUntil; Result = result }
            | _ ->
                failwith "Could not find either error or result tag."
        | Element "eveapi" _, _ ->
            failwith "Unsupported API version."
        | _ ->
            failwithf "Unexpected tag: %s" doc.Root.Name.LocalName

    let getResponse path values = async {
        let postData =
            values
            |> Seq.map (fun (k,v) -> (encode k) + "=" + (encode v))
            |> fun data -> String.Join("&", data)
            |> Encoding.UTF8.GetBytes
        let builder = UriBuilder(baseUri, Path = path)
        let request = WebRequest.CreateHttp(builder.Uri)
        request.Method <- "POST"
        request.ContentType <- "application/x-www-form-urlencoded"
        use! rs = request.AsyncGetRequestStream()
        do! rs.AsyncWrite(postData)
        use! response = request.AsyncGetResponse()
        return response.GetResponseStream()
               |> XDocument.Load
               |> parseDoc
    }