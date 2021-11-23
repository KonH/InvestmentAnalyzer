module private InvestmentAnalyzer.Importer.AlphaDirectOperations

open System.Xml
open FsToolkit.ErrorHandling
open InvestmentAnalyzer.Importer.Utils
open InvestmentAnalyzer.Importer.XmlUtils

let getLastNode (nodes: XmlNodeList) =
    if nodes.Count > 0 then nodes.[nodes.Count - 1] else null

let loadDate (xml: XmlDocument) =
    "Report/Trades2/Report/Tablix1/settlement_date_Collection/settlement_date"
    |> xml.SelectNodes
    |> getLastNode
    |> Option.ofObj
    |> Option.map getAttributes
    |> Option.bind (getAttributeValue "settlement_date")
    |> Option.map (tryParseDate "yyyy-MM-ddTHH:mm:ss")
    |> removeResultOption ["Failed to read date"]

let handleXmlContent (xml: XmlDocument) =
    let date = xml |> loadDate
    let entries = Ok [] // TODO: parse operations
    Result.zip date entries

let importValidXml (xml: XmlDocument) =
    xml
    |> removeNamespace "MyBroker"
    |> handleXmlContent

let alphaDirectOperationsImport stream =
    stream
    |> loadXml
    |> Result.bind importValidXml