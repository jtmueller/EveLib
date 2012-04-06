namespace EveLib

open System
open System.Diagnostics
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
            Id = charId
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

    let getMailHeaders charId = async {
        let values = ("characterID", string charId) :: apiValues
        let! response = getResponse "/char/MailMessages.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return
            rowset.Rows
            |> Seq.map (fun r ->
                {   Id = xval r?messageID
                    RecipientId = charId
                    SenderId = xval r?senderID
                    SentDate = (r?sentDate).Value + " +0"  // eve server time is GMT
                               |> DateTimeOffset.Parse
                    Title = xval r?title
                    ToCorpOrAllianceId = xopt r?toCorpOrAllianceID
                    ToCharacterIds = (r?toCharacterIDs).Value.Split([| ','; ' ' |], StringSplitOptions.RemoveEmptyEntries)
                                     |> Seq.map int
                    ToListIds = (r?toListID).Value.Split([| ','; ' ' |], StringSplitOptions.RemoveEmptyEntries)
                                |> Seq.map int
                    QueryTime = response.QueryTime
                    CachedUntil = response.CachedUntil   })
            |> Seq.cache
    }

    interface EveLib.FSharp.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId
        member x.GetMailHeaders(charId) = getMailHeaders charId
    interface EveLib.Async.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.StartAsTask
        member x.GetMailHeaders(charId) = getMailHeaders charId |> Async.StartAsTask
    interface EveLib.Sync.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.RunSynchronously
        member x.GetMailHeaders(charId) = getMailHeaders charId |> Async.RunSynchronously
