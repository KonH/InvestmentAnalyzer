module private InvestmentAnalyzer.Importer.ActualAlphaDirectOperations

open System.Xml
open FsToolkit.ErrorHandling
open InvestmentAnalyzer.Importer.Utils
open InvestmentAnalyzer.Importer.XmlUtils
open InvestmentAnalyzer.Importer.Common

let getLastNode (nodes: XmlNodeList) =
    if nodes.Count > 0 then nodes.[nodes.Count - 1] else null

let loadDate (xml: XmlDocument) =
    "report_broker/date_end"
    |> xml.SelectSingleNode
    |> Option.ofObj
    |> Option.map (fun n -> n.InnerText)
    |> Option.map (tryParseDate "dd.MM.yyyy H:mm:ss")
    |> removeResultOption ["Failed to read date"]

let expandNodes (nodes: XmlNodeList) =
    let mutable result = []
    for node in nodes do
        result <- node :: result
    result |> List.rev

let convertToMoneyMove (node: XmlNode) =
    let pCode = node.Attributes.["p_code"].Value
    let volumeAttr = node.Attributes.["volume"]
    if isNull volumeAttr then
        (pCode, None) else
        match tryParseDecimal volumeAttr.Value with
        | Ok volume -> (pCode, Some volume)
        | _ -> (pCode, None)

let isValidMoneyMove move =
    let _, volume = move
    match volume with
    | Some _ -> true
    | _ -> false

let detectType operationType (comment: string) =
    match operationType with
    | "Перевод" ->
        match comment with
        | _ when comment.StartsWith("из") -> In
        | _ when comment.StartsWith("Списание") -> Out
        | _ -> Ignored
    | _ -> Ignored
    |> (fun t -> t.ToString())

let loadNode (node: XmlNode) =
    result {
        let! settlementDate = (node.SelectSingleNode "settlement_date").InnerText |> tryParseDate "yyyy-MM-ddTHH:mm:ss"
        let operationType = (node.SelectSingleNode "oper_type").InnerText
        let comment = (node.SelectSingleNode "comment").InnerText
        let currency = (node.SelectSingleNode "p_code").InnerText
        let! volume = (node.SelectSingleNode "volume").InnerText |> tryParseDecimal
        let detectedType = detectType operationType comment
        return {
            Date = settlementDate
            Type = detectedType
            Currency = currency
            Volume = volume
        }
    }

let loadOperations (xml: XmlDocument) =
    "report_broker/money_moves/money_move"
    |> xml.SelectNodes
    |> expandNodes
    |> List.map loadNode
    |> unwrapSeq

let handleXmlContent (xml: XmlDocument) =
    result {
        let! date = xml |> loadDate
        let! operations = loadOperations xml
        return (date, operations)
    }

let importActualValidXml (xml: XmlDocument) =
    xml
    |> removeNamespace "MyBroker"
    |> handleXmlContent