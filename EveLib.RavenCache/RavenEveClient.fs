namespace EveLib.RavenCache

open System
open Raven.Client
open Raven.Client.Linq
open EveLib

type RavenEveClient(apiKey:ApiKey) =

    // TODO: get config or let the store be passed in.
    static let store = Document.DocumentStore.OpenInitializedStore()

    let baseClient = EveClient.CreateFSharp apiKey
    let character = lazy( CharacterCache(baseClient.Character, store) )
    let corporation = lazy( CorporationCache(baseClient.Corporation, store) )
    let eve = lazy( EveCache(baseClient.Eve, store) )
    let map = lazy( MapCache(baseClient.Map, store) )

    let getCharacters () = async {
        use session = store.OpenAsyncSession()

        let! charList = session.AsyncLoad<CharacterList>(sprintf "characterlists/%i" apiKey.Id)

        match charList with
        | None ->
            let! updated = baseClient.GetCharacters()
            session.Store updated
            do! session.AsyncSaveChanges()
            return updated
        | Some cl when cl.CachedUntil < DateTimeOffset.UtcNow ->
            let! updated = baseClient.GetCharacters()
            session.Advanced.Evict cl
            session.Store updated
            do! session.AsyncSaveChanges()
            return updated
        | Some cl ->
            return cl
    }

    let getServerStatus () = async {
        use session = store.OpenAsyncSession()
        
        let! status = session.Query<ServerStatus>() |> AsyncQuery.head

        match status with
        | None ->
            let! updated = baseClient.GetServerStatus()
            session.Store updated
            do! session.AsyncSaveChanges()
            return updated
        | Some st when st.CachedUntil < DateTimeOffset.UtcNow ->
            try
                let! updated = baseClient.GetServerStatus()
                session.Advanced.Evict st
                session.Store updated
                do! session.AsyncSaveChanges()
                return updated
            with _ ->
                return st
        | Some st ->
            return st
    }

    interface EveLib.FSharp.IEveClient with
        member x.GetCharacters() = getCharacters()
        member x.GetServerStatus() = getServerStatus()
        member x.Character = upcast character.Value
        member x.Corporation = upcast corporation.Value
        member x.Eve = upcast eve.Value
        member x.Map = upcast map.Value

    interface EveLib.Async.IEveClient with
        member x.GetCharacters() = getCharacters() |> Async.StartAsTask
        member x.GetServerStatus() = getServerStatus() |> Async.StartAsTask
        member x.Character = upcast character.Value
        member x.Corporation = upcast corporation.Value
        member x.Eve = upcast eve.Value
        member x.Map = upcast map.Value

    interface EveLib.Sync.IEveClient with
        member x.GetCharacters() = getCharacters() |> Async.RunSynchronously
        member x.GetServerStatus() = getServerStatus() |> Async.RunSynchronously
        member x.Character = upcast character.Value
        member x.Corporation = upcast corporation.Value
        member x.Eve = upcast eve.Value
        member x.Map = upcast map.Value

    static member CreateFSharp apiKey store = RavenEveClient(apiKey) :> EveLib.FSharp.IEveClient
    static member CreateAsync apiKey store = RavenEveClient(apiKey) :> EveLib.Async.IEveClient
    static member CreateSync apiKey = RavenEveClient(apiKey) :> EveLib.Sync.IEveClient