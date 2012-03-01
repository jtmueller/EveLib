namespace Mueller.EveLib

open System
open System.Net
open System.Text
open System.Xml
open System.Xml.Linq
open Mueller.EveLib.FSharp

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

module internal ClientUtils =
    let baseUri = Uri("https://api.eveonline.com")

    let private encode = Uri.EscapeDataString

    type ParseResult = {
        QueryTime : DateTimeOffset
        CachedUntil : DateTimeOffset
        Result : XElement
    }

    let parseDoc (doc:XDocument) =
        let root = doc.Root
        if root.Name.LocalName <> "eveapi" then failwith ("Unexpected tag: " + root.Name.LocalName)
        let version = root.Attribute(xn "version")
        if isNull version || version.Value <> "2" then failwith "Unsupported API Version"
        let queryTime = DateTimeOffset.Parse(root.Element(xn "currentTime").Value + " +0")
        let result = root.Element(xn "result")
        let cachedUntil = DateTimeOffset.Parse(root.Element(xn "cachedUntil").Value + " +0") // server time is GMT
        { QueryTime = queryTime; CachedUntil = cachedUntil; Result = result }

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