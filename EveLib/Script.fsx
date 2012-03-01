// This file is a script that can be executed with the F# Interactive.  
// It can be used to explore and test the library project.
// Note that script files will not be part of the project build.

#r "System.Xml"
#r "System.Xml.Linq"
#load "Extensions.fs"
#load "ClientUtils.fs"
#load "Models.fs"
#load "Interfaces.fs"
#load "EveClient.fs"
open Mueller.EveLib
open Mueller.EveLib.FSharp

let key = { Id = 0; VCode = ""; AccessMask = 0 }
let client = EveClient.CreateSync key

let charList = client.GetCharacters()
let sigur = charList.Characters |> Seq.last
let serverStatus = client.GetServerStatus()

let wallet = client.Character.GetAccountBalance(sigur.CharId)

let kills = client.Map.GetRecentKills()

let lookup = client.Eve.GetItemIds("Sigur Yassavi")