module private InvestmentAnalyzer.Importer.AlphaDirectState

open System.Xml
open FsToolkit.ErrorHandling
open InvestmentAnalyzer.Importer.Common
open InvestmentAnalyzer.Importer.Utils
open InvestmentAnalyzer.Importer.XmlUtils

type AlphaDirectRawEntry = {
    activeNameStr: Option<string>
    isinStr: Option<string>
    currencyStr: Option<string>
    countStr: Option<string>
    totalPriceStr: Option<string>
}

let loadDetailNodes (xml: XmlDocument) =
    "Report/Financial_results/Report/Tablix1/code_curr_Collection/code_curr/Details_Collection/Details"
    |> xml.SelectNodes
    |> Seq.cast<XmlNode>

let readRawNodeValues node =
    let attributes = node |> Option.ofObj |> Option.map getAttributes
    {
         activeNameStr = attributes |> Option.bind (getAttributeValue "p_name")
         isinStr = attributes |> Option.bind (getAttributeValue "ISIN2")
         currencyStr = attributes |> Option.bind (getAttributeValue "code_curr")
         countStr = attributes |> Option.bind (getAttributeValue "forword_rest2")
         totalPriceStr = attributes |> Option.bind (getAttributeValue "CostOpenPosEnd2")
    }

let isNodeRequired (entry: AlphaDirectRawEntry) =
    // Is nodes without ISIN present, it is usually currencies, don't track them for now
    entry.isinStr.IsSome &&
    // In some cases count is not present, skip such nodes
    entry.countStr.IsSome

let addIsin (value: Option<string>) (entry: StateEntry) : Result<StateEntry, string list> =
    value
    |> optionToResult ["Failed to read ISIN"]
    |> Result.map (fun v -> { entry with ISIN = v })

let addName (value: Option<string>) (entry: StateEntry) : Result<StateEntry, string list> =
    value
    |> optionToResult ["Failed to read Name"]
    |> Result.map (fun v -> { entry with Name = v })

let addCount (value: Option<string>) (entry: StateEntry) : Result<StateEntry, string list> =
    value
    |> optionToResult ["Failed to read Count"]
    |> Result.map tryParseDouble
    |> reduceResult
    |> Result.map (fun v -> { entry with Count = v })

let addPricePerUnit (entry: StateEntry) : StateEntry =
    { entry with PricePerUnit = entry.TotalPrice / float entry.Count }

let addTotalPrice (value: Option<string>) (entry: StateEntry) : Result<StateEntry, string list> =
    value
    |> optionToResult ["Failed to read TotalPrice"]
    |> Result.map tryParseDouble
    |> reduceResult
    |> Result.map (fun v -> { entry with TotalPrice = v })

let addCurrency (value: Option<string>) (entry: StateEntry) : Result<StateEntry, string list> =
    value
    |> optionToResult ["Failed to read Currency"]
    |> Result.map (fun v -> { entry with Currency = v })

let importDetailNode (entry: AlphaDirectRawEntry) =
    {
        ISIN = ""
        Name = ""
        Currency = ""
        Count = 0.0
        TotalPrice = 0.0
        PricePerUnit = 0.0
    }
    |> addIsin entry.isinStr
    |> Result.bind (addName entry.activeNameStr)
    |> Result.bind (addCurrency entry.currencyStr)
    |> Result.bind (addCount entry.countStr)
    |> Result.bind (addTotalPrice entry.totalPriceStr)
    |> Result.map addPricePerUnit

let importDetailNodes nodes =
    nodes
    |> Seq.map readRawNodeValues
    |> Seq.filter isNodeRequired
    |> Seq.map importDetailNode
    |> unwrapSeq

let loadDate (xml: XmlDocument) =
    "Report/Financial_results/Report/Tablix1"
    |> xml.SelectSingleNode
    |> Option.ofObj
    |> Option.map getAttributes
    |> Option.bind (getAttributeValue "Textbox45")
    |> Option.map (getLastPartSeparatedBy " ")
    |> removeResultOption ["Failed to load date"]
    |> Result.bind (tryParseDate "dd.MM.yyyy")

let handleXmlContent (xml: XmlDocument) =
    let date = xml |> loadDate
    let entries = xml |> loadDetailNodes |> importDetailNodes
    Result.zip date entries

let importValidXml (xml: XmlDocument) =
    xml
    |> removeNamespace "MyPortfolio"
    |> handleXmlContent

let alphaDirectStateImport stream =
    stream
    |> loadXml
    |> Result.bind importValidXml