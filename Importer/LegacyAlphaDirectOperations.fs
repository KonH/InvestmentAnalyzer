module private InvestmentAnalyzer.Importer.LegacyAlphaDirectOperations

open System.Xml
open FsToolkit.ErrorHandling
open InvestmentAnalyzer.Importer.Utils
open InvestmentAnalyzer.Importer.XmlUtils
open InvestmentAnalyzer.Importer.Common

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

let loadNode (node: XmlNode) =
    let attrs = getAttributes node
    let lastUpdate = attrs.["last_update"].Value
    match tryParseDate "yyyy-MM-ddTHH:mm:ss" lastUpdate with
    | Ok lastUpdateDate ->
        let operationTypeNode = node.SelectSingleNode "oper_type"
        let operationType = operationTypeNode.Attributes.["oper_type"].Value
        let comment = (operationTypeNode.SelectSingleNode "comment").Attributes.["comment"].Value
        let moneyMoves =
            "oper_type/comment/money_volume_begin1_Collection/money_volume_begin1/p_code_Collection/p_code/p_code"
            |> node.SelectNodes
            |> expandNodes
            |> List.map convertToMoneyMove
            |> List.filter isValidMoneyMove
        let moneyMove = moneyMoves.[0]
        let currency, volumeOpt = moneyMove
        let volume = Option.get volumeOpt
        let detectedType = detectType operationType comment
        Ok {
            Date = lastUpdateDate
            Type = detectedType
            Currency = currency
            Volume = volume
        }
    | Error e -> Error e


let loadOperations (xml: XmlDocument) =
    "Report/Trades2/Report/Tablix1/settlement_date_Collection/settlement_date/rn_Collection/rn"
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

let importLegacyValidXml (xml: XmlDocument) =
    xml
    |> removeNamespace "MyBroker"
    |> handleXmlContent