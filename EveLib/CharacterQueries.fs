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

    let getMailBodies charId (messageIds:int seq) = async {
        let values = ("characterID", string charId) :: ("ids", String.Join(",", messageIds)) :: apiValues
        let! response = getResponse "/char/MailBodies.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return
            rowset.Rows
            |> Seq.map (fun r -> { Id = xval r?messageID 
                                   RecipientId = charId
                                   Text = r.Element.Value 
                                   QueryTime = response.QueryTime 
                                   CachedUntil = response.CachedUntil })
            |> Seq.cache
    }

    let getCharSheet charId = async {
        let values =  ("characterID", string charId) :: apiValues
        let toAugment (el:XElement) =
            if isNull el then None
            else Some { AugmentName = xelv "augmentatorName" el; AugmentValue = xelv "augmentatorValue" el }
        let toRole row =
            { RoleId = xval row?roleID; RoleName = xval row?roleName }
        let findRowset name (el:XElement) =
            el.Elements(xn "rowset")
            |> Seq.tryFind (fun x -> x.Attribute(xn "name").Value = name)
            |> Option.map (fun x -> RowSet(x))

        let! response = getResponse "/char/CharacterSheet.xml.aspx" values
        let r = response.Result
        let enhancers = r.Element(xn "attributeEnhancers")
        let attributes = r.Element(xn "attributes")

        return {
            Id = xelv "characterID" r
            CharacterName = xelv "name" r
            DoB = (xelv "DoB" r) + " +0" |> DateTimeOffset.Parse
            Race = xelv "race" r
            Bloodline = xelv "bloodLine" r
            Ancestry = xelv "ancestry" r
            Gender = xelv "gender" r
            CorpName = xelv "corporationName" r
            CorpId = xelv "corporationID" r
            AllianceName = xelvo "allianceName" r
            AllianceId = xelvo "allianceID" r
            CloneName = xelv "cloneName" r
            CloneSkillPoints = xelv "cloneSkillPoints" r
            Balance = xelv "balance" r
            AttributeEnhancers = 
                { MemoryBonus = enhancers.Element(xn "memoryBonus") |> toAugment
                  PerceptionBonus = enhancers.Element(xn "perceptionBonus") |> toAugment
                  WillpowerBonus = enhancers.Element(xn "willpowerBonus") |> toAugment
                  IntelligenceBonus = enhancers.Element(xn "intelligenceBonus") |> toAugment
                  CharismaBonus = enhancers.Element(xn "charismaBonus") |> toAugment }
            Attributes = 
                { Intelligence = xelv "intelligence" attributes
                  Memory = xelv "memory" attributes
                  Charisma = xelv "charisma" attributes
                  Perception = xelv "perception" attributes
                  Willpower = xelv "willpower" attributes }
            Skills = 
                match findRowset "skills" r with
                | None -> Seq.empty
                | Some rs ->
                    rs.Rows
                    |> Seq.map (fun s -> { TypeId = xval s?typeID
                                           SkillPoints = xval s?skillpoints
                                           Level = xval s?level
                                           Published = (string s?published = "1") })
                    |> Seq.cache
            Certificates =
                match findRowset "certificates" r with
                | None -> Seq.empty
                | Some rs -> rs.Rows |> Seq.map (fun c -> int c?certificateID)
            CorpRoles =
                match findRowset "corporationRoles" r with
                | None -> Seq.empty
                | Some rs -> rs.Rows |> Seq.map toRole
            CorpRolesAtHQ =
                match findRowset "corporationRolesAtHQ" r with
                | None -> Seq.empty
                | Some rs -> rs.Rows |> Seq.map toRole
            CorpRolesAtBase =
                match findRowset "corporationRolesAtBase" r with
                | None -> Seq.empty
                | Some rs -> rs.Rows |> Seq.map toRole
            CorpRolesAtOther =
                match findRowset "corporationRoles" r with
                | None -> Seq.empty
                | Some rs -> rs.Rows |> Seq.map toRole
            CorpTitles =
                match findRowset "corporationTitles" r with
                | None -> Seq.empty
                | Some rs -> rs.Rows |> Seq.map (fun t -> { TitleId = xval t?titleID; TitleName = xval t?titleName })
            QueryTime = response.QueryTime
            CachedUntil = response.CachedUntil
        }
    }

    interface EveLib.FSharp.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId
        member x.GetMailHeaders(charId) = getMailHeaders charId
        member x.GetMailBodies(charId, msgIds) = getMailBodies charId msgIds
        member x.GetCharacterSheet(charId) = getCharSheet charId

    interface EveLib.Async.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.StartAsTask
        member x.GetMailHeaders(charId) = getMailHeaders charId |> Async.StartAsTask
        member x.GetMailBodies(charId, msgIds) = getMailBodies charId msgIds |> Async.StartAsTask
        member x.GetCharacterSheet(charId) = getCharSheet charId |> Async.StartAsTask

