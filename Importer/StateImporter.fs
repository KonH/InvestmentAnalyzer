module public InvestmentAnalyzer.Importer.StateImporter

open System
open System.IO
open InvestmentAnalyzer.Importer.Common
open InvestmentAnalyzer.Importer.Utils
open InvestmentAnalyzer.Importer.AlphaDirect
open InvestmentAnalyzer.Importer.Tinkoff

type StateReportFormat =
    | AlphaDirectMyPortfolio
    | TinkoffMyAssets

type ImportResult = {
    Success: bool
    Errors: string[]
    Date: DateTime
    Entries: StateEntry[]
}

let private ok date entries =
    { Success = true; Errors = Array.ofList []; Date = date; Entries = Array.ofList entries }

let private error errors =
    { Success = false; Errors = Array.ofList errors; Date = DateTime.MinValue; Entries = Array.ofList [] }

let private convertToExporterResult (result: Result<DateTime * StateEntry list, string list>) =
    match result with
    | Ok (date, entries) -> ok date entries
    | Error e -> error e

let private exceptionToResult func =
    try
       func()
    with e ->
       Error [e.ToString()]

let LoadStateByFormat (stream: Stream, formatStr: string) : ImportResult =
    let format = unionFromString<StateReportFormat> formatStr
    match format with
    | Some AlphaDirectMyPortfolio -> exceptionToResult (fun () -> alphaDirectImport stream)
    | Some TinkoffMyAssets -> exceptionToResult (fun () -> tinkoffImport stream)
    | None -> Error [$"Unknown state format '{formatStr}'"]
    |> convertToExporterResult