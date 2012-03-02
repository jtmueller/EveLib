﻿namespace EveLib

open System
open System.Xml
open System.Xml.Linq
open EveLib
open EveLib.FSharp
open ClientUtils

type internal CharacterQueries(apiValues: (string * string) list) =

    let getAccountBalance charId = async {
        let values = ("characterID", string charId) :: apiValues
        let! response = getResponse "/char/AccountBalance.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return {
            CharacterId = charId
            Type = WalletType.Personal
            Accounts =
                rowset.Rows
                |> Seq.map (fun r -> { AccountId = xval r?accountID
                                       AccountKey = xval r?accountKey
                                       Balance = xval r?balance })
                |> Seq.cache
            QueryTime = response.QueryTime;
            CachedUntil = response.CachedUntil
        }
    }

    interface EveLib.FSharp.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId
    interface EveLib.Async.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.StartAsTask
    interface EveLib.Sync.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.RunSynchronously