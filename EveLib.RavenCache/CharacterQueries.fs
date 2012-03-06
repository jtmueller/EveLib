namespace EveLib.RavenCache

open System
open Raven.Client
open Raven.Client.Linq
open EveLib

type internal CharacterQueries(baseClient: FSharp.ICharQueries, store: IDocumentStore) =
    
    let getAccountBalance charId = async {
        use session = store.OpenAsyncSession()

        let! wallets =
            query {
                for set in session.Query<WalletSet>() do
                where (set.Type = WalletType.Personal && set.Id = charId)
                select set
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

    interface EveLib.FSharp.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId
    interface EveLib.Async.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.StartAsTask
    interface EveLib.Sync.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.RunSynchronously
