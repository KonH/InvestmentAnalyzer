module private InvestmentAnalyzer.Importer.TinkoffState

open System
open System.Text.RegularExpressions
open FsToolkit.ErrorHandling
open InvestmentAnalyzer.Importer.Common
open InvestmentAnalyzer.Importer.Utils
open InvestmentAnalyzer.Importer.PrdUtils

type AssetType =
    | Bond
    | Other

let isValidLine str =
    let isInvalid = String.IsNullOrEmpty(str) || str.Contains(" из ") || str.Contains(" из\t")
    not isInvalid

let extractSection (lines: string list) (firstHeader: string) (lastHeader: string) headerLength =
    lines
    |> Seq.skipWhile (fun str -> str.Contains(firstHeader) = false)
    |> Seq.skip (1 + headerLength)
    |> Seq.filter isValidLine
    |> Seq.takeWhile (fun str -> str.Contains(lastHeader) = false)
    |> List.ofSeq

let compactLines (lines: string[] list) =
    let mutable result = []
    let mutable shouldAppendPrevLine = false
    for i in 0..lines.Length - 1 do
        let currentLine = lines.[i]
        match not shouldAppendPrevLine && currentLine.Length <= 3 with
        | true ->
            shouldAppendPrevLine <- true
        | false ->
            let lineToAdd = if shouldAppendPrevLine then Array.concat [lines.[i - 1]; currentLine] else currentLine
            result <- lineToAdd :: result
            shouldAppendPrevLine <- false
    result |> List.rev

let loadAssetLines lines =
    extractSection lines "2. Ценные бумаги" "3. Производные финансовые инструменты" 4
    |> List.map (fun str -> str.Split('\t'))
    |> compactLines

let reorderByIsin (lines: string list) =
    let isinRegex = Regex("([A-Z]{2})((?![A-Z]{10}\b)[A-Z0-9]{10})")
    let fullLine = String.Join('\t', lines)
    let matches = isinRegex.Matches(fullLine)
    let mutable lastIndex = 0
    seq { 0..(matches.Count - 1) }
    |> Seq.map (fun i -> matches.[i])
    |> Seq.map (fun m ->
        let startIndex = lastIndex
        let endIndex = m.Index + m.Length
        let length = endIndex - startIndex
        lastIndex <- endIndex
        let line = fullLine.Substring(startIndex, length).Trim()
        line)
    |> Array.ofSeq

let loadInfoLines lines =
    extractSection lines "4. Справочник ценных бумаг" "5. Справочник производных финансовых инструментов" 0
    |> reorderByIsin

let detectAssetType (assetLine: string array) =
    if assetLine.Length = 8 then AssetType.Bond else AssetType.Other

let getIndexes assetType =
    match assetType with
    | Bond -> (2, 3)
    | Other -> (0, 1)

let createEntryByValues name isin currency =
    {
        ISIN = isin;
        Name = name;
        Currency = currency
        Count = 0.0;
        TotalPrice = 0.0;
        PricePerUnit = 0.0;
    }

let createEntry currency (infoParts: string array) =
    if infoParts.Length >= 2
    then
        let name = infoParts.[infoParts.Length - 2]
        let isin = infoParts.[infoParts.Length - 1]
        Ok (createEntryByValues name isin currency)
    else
        Error [$"Failed to get info from infoParts: {String.Join(';', infoParts)}"]

let addCount str entry =
    tryParseDouble str
    |> Result.map (fun v -> { entry with Count = v })

let addPricePerUnit str entry =
    tryParseDouble str
    |> Result.map (fun v -> { entry with PricePerUnit = v })

let addTotalPrice str entry =
    tryParseDouble str
    |> Result.map (fun v -> { entry with TotalPrice = v })

let lookupInfoBaseIndexCustom (infoLines: string array) (code: string) =
    let customIndex = array.FindIndex(infoLines, (fun s -> s.Contains(code)))
    if customIndex >= 0 then Ok(customIndex) else Error [$"Failed to find asset by code '{code}'"]

let lookupInfoLineBaseIndex (infoLines: string array) (code: string) =
    let simpleIndex = array.FindIndex(infoLines, (fun s -> s.StartsWith(code)))
    if simpleIndex >= 0 then Ok(simpleIndex) else lookupInfoBaseIndexCustom infoLines code

let extractInfoSubstring (infoLine: string) (code: string) =
    let infoCodeIndex = infoLine.IndexOf(code, StringComparison.InvariantCulture)
    infoLine.Substring(infoCodeIndex)

let handleInfoLineByIndex (infoLines: string array) assetType index =
    let finalIndex = if assetType = AssetType.Bond then (index + 1) else index
    infoLines.[finalIndex]

let lookupInfoLine (infoLines: string array) assetType code =
   lookupInfoLineBaseIndex infoLines code
   |> Result.map (handleInfoLineByIndex infoLines assetType)
   |> Result.map (fun s -> s.Split('\t'))

let processAsset infoLines assetLine =
    let assetType = detectAssetType assetLine
    let codeIndex, countIndex = getIndexes assetType
    let code = assetLine.[codeIndex]
    let countStr = assetLine.[countIndex]
    let pricePerUnitParts = assetLine.[assetLine.Length - 2].Split()
    let totalPriceParts = assetLine.[assetLine.Length - 1].Split()
    let currency = pricePerUnitParts.[1]
    lookupInfoLine infoLines assetType code
    |> Result.bind (createEntry currency)
    |> Result.bind (addCount countStr)
    |> Result.bind (addPricePerUnit pricePerUnitParts.[0])
    |> Result.bind (addTotalPrice totalPriceParts.[0])

let processAssets assetLines infoLines =
    assetLines
    |> List.map (processAsset infoLines)
    |> unwrapSeq

let readDate (lines: string list) =
    let line = lines |> List.find (fun str -> str.StartsWith("Справка об активах Инвестора на брокерском счете на"))
    line.Split(' ')
    |> (fun parts -> parts.[parts.Length - 1])
    |> tryParseDate "dd.MM.yyyy"

let tinkoffStateImport stream =
    let lines = stream |> loadDocument |> readAllLines
    let assetLines = loadAssetLines lines
    let infoLines = loadInfoLines lines
    let date = readDate lines
    let assets = processAssets assetLines infoLines
    Result.zip date assets
