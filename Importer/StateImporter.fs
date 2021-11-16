module public Importer.StateImporter

open System
open System.IO
open Importer.Common
open Importer.AlphaDirect
open Importer.Tinkoff

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

let LoadStateByFormat (stream: Stream, format: StateReportFormat) : ImportResult =
    match format with
    | AlphaDirectMyPortfolio -> alphaDirectImport stream
    | TinkoffMyAssets -> tinkoffImport stream
    |> convertToExporterResult