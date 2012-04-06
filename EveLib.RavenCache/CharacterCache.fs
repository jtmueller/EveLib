namespace EveLib.RavenCache

open System
open Raven.Client
open Raven.Client.Linq
open EveLib

type internal CharacterCache(baseClient: FSharp.ICharQueries, store: IDocumentStore) =
    
    let getAccountBalance charId = async {
        use session = store.OpenAsyncSession()

        let! wallets =
            query {
                for set in session.Query<WalletSet>() do
                where (set.Type = WalletType.Personal && set.Id = charId)
            } |> AsyncQuery.head

        match wallets with
        | None ->
            let! updated = baseClient.GetAccountBalance(charId)
            session.Store updated
            do! session.AsyncSaveChanges()
            return updated
        | Some set when set.CachedUntil < DateTimeOffset.UtcNow ->
            try
                let! updated = baseClient.GetAccountBalance(charId)
                session.Advanced.Evict set
                session.Store updated
                do! session.AsyncSaveChanges()
                return updated
            with _ ->
                return set
        | Some set ->
            return set
    }

    let getMailHeaders charId = async {
        use session = store.OpenAsyncSession()

        let! headers =
            query {
                for h in session.Query<MailHeader>() do
                where (h.RecipientId = charId)
                sortByDescending h.SentDate
            } |> AsyncQuery.asIList

        if headers.Count = 0 then
            let! updated = baseClient.GetMailHeaders(charId)
            updated |> Seq.iter session.Store
            do! session.AsyncSaveChanges()
            return query {
                for u in updated do 
                sortByDescending u.SentDate 
            } |> Seq.cache
        else
            let mostRecent =
                headers |> Seq.map (fun h -> h.QueryTime) |> Seq.max
            if mostRecent - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(30.) then
                // it's been at least 30 minutes since our last check, let's check now.
                let! updated = baseClient.GetMailHeaders(charId)
                headers |> Seq.iter session.Advanced.Evict
                updated |> Seq.iter session.Store
                do! session.AsyncSaveChanges()
                let merged =
                    headers
                    |> Seq.filter (fun h -> h.CachedUntil < DateTimeOffset.UtcNow)
                    |> Seq.append updated
                    |> Seq.distinctBy (fun h -> h.Id, h.RecipientId)
                return query {
                    for h in merged do
                    sortByDescending h.SentDate
                } |> Seq.cache
            else
                return upcast headers
    }

    let getMailBodies charId messageIds = async {
        use session = store.OpenAsyncSession()

        let! bodies =
            query {
                for b in session.Query<MailBody>() do
                where (b.RecipientId = charId && 
                       LinqExtensions.In(b.Id, messageIds))
            } |> AsyncQuery.asIList

        if bodies.Count = 0 || bodies.Count < Seq.length messageIds then
            let! updated = baseClient.GetMailBodies(charId, messageIds)
            bodies |> Seq.iter session.Advanced.Evict
            updated |> Seq.iter session.Store
            do! session.AsyncSaveChanges()
            if bodies.Count = 0 then
                return updated
            else
                return
                    bodies
                    |> Seq.filter (fun b -> b.CachedUntil < DateTimeOffset.UtcNow)
                    |> Seq.append updated
                    |> Seq.distinctBy (fun b -> b.Id, b.RecipientId)
                    |> Seq.cache
        else
            return upcast bodies
    }

    interface EveLib.FSharp.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId
        member x.GetMailHeaders(charId) = getMailHeaders charId
        member x.GetMailBodies(charId, [<ParamArray>] messageIds) = 
            getMailBodies charId messageIds
    interface EveLib.Async.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.StartAsTask
        member x.GetMailHeaders(charId) = getMailHeaders charId |> Async.StartAsTask
        member x.GetMailBodies(charId, [<ParamArray>] messageIds) = 
            getMailBodies charId messageIds |> Async.StartAsTask
    interface EveLib.Sync.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.RunSynchronously
        member x.GetMailHeaders(charId) = getMailHeaders charId |> Async.RunSynchronously
        member x.GetMailBodies(charId, [<ParamArray>] messageIds) = 
            getMailBodies charId messageIds |> Async.RunSynchronously
