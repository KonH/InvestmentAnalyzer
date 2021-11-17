module private InvestmentAnalyzer.Importer.Utils

open System
open System.Globalization
open FsToolkit.ErrorHandling
open Microsoft.FSharp.Reflection

let getOptionString str =
    match str with
    | _ when String.IsNullOrEmpty(str) -> None
    | _ -> Some str

let removeResultOption error option =
    match option with
    | Some result -> result
    | None _ -> Error error

let optionToResult error option =
    match option with
    | Some result -> Ok result
    | None _ -> Error error

let tryParseDouble (str: string) =
    match Double.TryParse(str) with
    | true, v -> Ok v
    | _ -> Error [$"Failed to parse double from '{str}'"]

let reduceResult (result: Result<Result<'a, 'b>, 'b>) : Result<'a, 'b> =
    match result with
    | Ok ok -> ok
    | Error e -> Error e

let unwrapSeq seq =
    let result = List.ofSeq seq |> List.sequenceResultA
    match result with
    | Ok entries -> Ok entries
    | Error errors -> errors |> List.concat |> Error

let tryParseDate (format: string) (str: string) =
    match DateTime.TryParseExact(str, format, CultureInfo.InvariantCulture, DateTimeStyles.None) with
    | true, dt -> Ok dt
    | _ -> Error [$"Failed to parse date from '{str}' with format '{format}'"]

let getLastPartSeparatedBy (separator: string) (str: string) =
    let parts = str.Split(separator)
    if parts.Length > 0 then Ok parts.[parts.Length - 1] else Error [$"Not enough parts in string '{str}'"]

let zipTuple (r: Result<'a, 'b> * Result<'c, 'b>) =
    let left, right = r
    Result.zip left right

let unionFromString<'a> (s:string) =
    match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
    |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
    |_ -> None