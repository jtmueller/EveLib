namespace EveLib.RavenCache

// From: https://github.com/robertpi/FsRavenDbTools

open System
open System.Reflection
open System.Linq
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open Raven.Client.Document
open Raven.Abstractions.Data
open Raven.Client.Linq

[<AutoOpen>]
module RavenExt =
    type OptionTypeConverter() =
        inherit JsonConverter()
        override x.CanConvert(typ:Type) =
            typ.IsGenericType && 
                typ.GetGenericTypeDefinition() = typedefof<option<OptionTypeConverter>>

        override x.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
            if value <> null then
                let t = value.GetType()
                let fieldInfo = t.GetField("value", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Instance)
                let value = fieldInfo.GetValue(value)
                serializer.Serialize(writer, value)

        override x.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) = 
            let cases = Microsoft.FSharp.Reflection.FSharpType.GetUnionCases(objectType)
            let t = objectType.GetGenericArguments().[0]
            let t = 
                if t.IsValueType then 
                    let nullable = typedefof<Nullable<int>> 
                    nullable.MakeGenericType [|t|]
                else 
                    t
            let value = serializer.Deserialize(reader, t)
            if value <> null then
                FSharpValue.MakeUnion(cases.[1], [|value|])
            else
                FSharpValue.MakeUnion(cases.[0], [||])


    type UnionTypeConverter() =
        inherit JsonConverter()

        override x.CanConvert(typ:Type) =
            FSharpType.IsUnion typ 

        override x.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
            let t = value.GetType()
            let (info, fields) = FSharpValue.GetUnionFields(value, t)
            writer.WriteStartObject()
            writer.WritePropertyName("_tag")
            writer.WriteValue(info.Tag)
            let cases = FSharpType.GetUnionCases(t)
            let case = cases.[info.Tag]
            let fields = case.GetFields()
            for field in fields do
                writer.WritePropertyName(field.Name)
                serializer.Serialize(writer, field.GetValue(value, [||]))
            writer.WriteEndObject()

        override x.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) =
              reader.Read() |> ignore //pop start obj type label
              reader.Read() |> ignore //pop tag prop name
              let union = FSharpType.GetUnionCases(objectType)
              let case = union.[int(reader.Value :?> int64)]
              let fieldValues =  [| 
                     for field in case.GetFields() do
                         reader.Read() |> ignore //pop item name
                         reader.Read() |> ignore
                         yield serializer.Deserialize(reader, field.PropertyType)
               |] 

              reader.Read() |> ignore
              FSharpValue.MakeUnion(case, fieldValues)

    let private addConverters (serializer:JsonSerializer) =
        let converters = serializer.Converters
        converters.Add(OptionTypeConverter())
        converters.Add(UnionTypeConverter())

    type Raven.Client.Document.DocumentStore with
        static member OpenInitializedStore(?url) =
            let url = defaultArg url "http://localhost:8080"
            let store = new DocumentStore(Url = url)
            store.Conventions.CustomizeJsonSerializer <- Action<_> addConverters
            store.Initialize()

        static member OpenInitializedStore(opts: RavenConnectionStringOptions) =
            let store = new DocumentStore(Url = opts.Url, ApiKey = opts.ApiKey)
            store.Conventions.CustomizeJsonSerializer <- Action<_> addConverters
            store.Initialize()

    type Raven.Client.IAsyncDocumentSession with
        member x.AsyncLoad(id: string) =
            x.LoadAsync(id) |> Async.AwaitTask

        member x.AsyncLoad(ids: string[]) =
            x.LoadAsync(ids) |> Async.AwaitTask

        member x.AsyncLoad(id: ValueType) =
            x.LoadAsync(id) |> Async.AwaitTask

        member x.AsyncSaveChanges() =
            x.SaveChangesAsync() |> (Async.AwaitIAsyncResult >> Async.Ignore)

    type System.Linq.IQueryable<'a> with
        member x.AsyncToList() =
            x.ToListAsync() |> Async.AwaitTask

    type Raven.Client.Linq.IRavenQueryable<'a> with
        member x.AsyncSuggest() =
            x.SuggestAsync() |> Async.AwaitTask

        member x.AsyncSuggest(query) =
            x.SuggestAsync(query) |> Async.AwaitTask
        
        member x.AsyncCount() =
            x.CountAsync() |> Async.AwaitTask
        
        member x.AsyncToFacets(facetDoc) =
            x.ToFacetsAsync(facetDoc) |> Async.AwaitTask
       
module AsyncQuery =

    let asIList<'a> (queryable: IQueryable<'a>) =
        queryable.AsyncToList()

    let head<'a> (queryable: IQueryable<'a>) = async {
        let! items = queryable.AsyncToList()
        return 
            if items.Count = 0
                then None
                else Some(items.[0])
    }