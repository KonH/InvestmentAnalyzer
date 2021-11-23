module public InvestmentAnalyzer.Importer.OperationImporter

open System
open System.IO
open InvestmentAnalyzer.Importer.Common
open InvestmentAnalyzer.Importer.Utils
open InvestmentAnalyzer.Importer.AlphaDirectOperations
open InvestmentAnalyzer.Importer.TinkoffOperations

type OperationReportFormat =
    | AlphaDirectMoneyMove
    | TinkoffMoneyMove

type ImportResult = {
    Success: bool
    Errors: string[]
    Date: DateTime
    Operations: OperationEntry[]
}

let private ok date entries =
    { Success = true; Errors = Array.ofList []; Date = date; Operations = Array.ofList entries }

let private error errors =
    { Success = false; Errors = Array.ofList errors; Date = DateTime.MinValue; Operations = Array.ofList [] }

let private convertToExporterResult (result: Result<DateTime * OperationEntry list, string list>) =
    match result with
    | Ok (date, entries) -> ok date entries
    | Error e -> error e

let private exceptionToResult func =
    try
       func()
    with e ->
       Error [e.ToString()]

let LoadOperationsByFormat (stream: Stream, formatStr: string) : ImportResult =
    let format = unionFromString<OperationReportFormat> formatStr
    match format with
    | Some AlphaDirectMoneyMove -> exceptionToResult (fun () -> alphaDirectOperationsImport stream)
    | Some TinkoffMoneyMove -> exceptionToResult (fun () -> tinkoffOperationsImport stream)
    | None -> Error [$"Unknown operations format '{formatStr}'"]
    |> convertToExporterResult