module private InvestmentAnalyzer.Importer.AlphaDirectOperations

open System.Xml
open FsToolkit.ErrorHandling
open InvestmentAnalyzer.Importer.XmlUtils
open InvestmentAnalyzer.Importer.ActualAlphaDirectOperations
open InvestmentAnalyzer.Importer.LegacyAlphaDirectOperations

let importValidXml (xml: XmlDocument) =
    let actualResult = xml |> importActualValidXml
    match actualResult with
    | Ok r -> Ok r
    | Error _ -> xml |> importLegacyValidXml

let alphaDirectOperationsImport stream =
    stream
    |> loadXml
    |> Result.bind importValidXml