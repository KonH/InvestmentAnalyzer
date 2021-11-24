module private InvestmentAnalyzer.Importer.TinkoffOperations

open System
open System.Globalization
open System.Linq
open System.IO
open ClosedXML.Excel
open InvestmentAnalyzer.Importer.Common

type OperationColumns = {
    Date: string
    Operation: string
    Income: string
    Expense: string
}

let toLetterOnlyString (input: string) =
    new string((input.Where Char.IsLetter).ToArray())

let lookupColumnLetter (row: IXLRow) (text: string) =
    let targetText = toLetterOnlyString text
    let targetRow = row.Cells(fun c -> toLetterOnlyString (c.GetString()) = targetText).First();
    targetRow.Address.ColumnLetter

let collectCurrencyHeaders (workbook: IXLWorkbook) currencies startCurrencyRow endCurrencyRow =
    currencies
    |> List.map (fun currency ->
    let headers = workbook.Search currency |> Seq.filter (fun cell ->
            (cell.Address.RowNumber > startCurrencyRow) &&
            (cell.Address.RowNumber < endCurrencyRow))
    match Seq.length headers with
    | length when length > 1 -> Some (Seq.last headers)
    | _ -> None)
    |> List.filter (fun o -> o.IsSome)
    |> List.map (fun o -> o.Value)
    |> Array.ofList

let convertType operation =
    match operation with
    | "Пополнение счета" -> In
    | "Вывод средств" -> Out
    | _ -> Ignored

let handleOperationRow (columns: OperationColumns) (currency: string) (row: IXLRow) =
    let dateCell = row.Cell(columns.Date)
    if String.IsNullOrEmpty(dateCell.GetString()) then
        None
    else
        let operation = row.Cell(columns.Operation).GetString().Trim()
        let operationType = convertType operation
        let income = operationType = In
        let sumLetter = if income then columns.Income else columns.Expense
        let sumStr = row.Cell(sumLetter).GetString()
        let sum = Double.Parse(sumStr.Replace(',', '.'), CultureInfo.InvariantCulture)
        let date = DateTime.ParseExact(dateCell.GetString(), "dd.MM.yyyy", null)
        let volume = if income then sum else -sum
        Some {
            Date = date
            Type = operationType
            Currency = currency
            Volume = volume
        }

let detectColumns labelRow =
    let columnLookup = lookupColumnLetter labelRow
    {
        Date = columnLookup "Дата исполнения"
        Operation = columnLookup "Операция"
        Income = columnLookup "Сумма зачисления"
        Expense = columnLookup "Сумма списания"
    }

let handleCurrencySection (workbook: IXLWorkbook) (header: IXLCell) (nextHeader: IXLCell) =
    let startRow = header.Address.RowNumber + 2
    let endRow = nextHeader.Address.RowNumber - 1
    let rows = workbook.FindRows(fun r ->
        (r.RowNumber() >= startRow) && (r.RowNumber() <= endRow))
    let currency = header.Value.ToString()
    let labelRow = workbook.FindRows(fun r -> r.RowNumber() = header.Address.RowNumber + 1).First()
    let columns = detectColumns labelRow
    rows
    |> Seq.map (handleOperationRow columns currency)
    |> Seq.filter (fun e -> e.IsSome)
    |> Seq.map (fun e -> e.Value)
    |> Seq.toList

let loadDate (workbook: IXLWorkbook) =
    let titleNode = workbook.Search("Отчет о сделках и операциях за период").First()
    let parts = titleNode.GetString().Split()
    let lastPart = parts.[parts.Length - 1]
    DateTime.ParseExact(lastPart, "dd.MM.yyyy", null)

let tinkoffOperationsImport (stream: Stream) =
    let workbook = new XLWorkbook(stream)
    let operationsHeader = workbook.Search("2. Операции с денежными средствами").Single()
    let nextCatHeader = workbook.Search("3.1 Движение по ценным бумагам инвестора").Single()
    let currencies = ["RUB"; "USD"; "EUR"]
    let startCurrencyRow = operationsHeader.Address.RowNumber
    let endCurrencyRow = nextCatHeader.Address.RowNumber
    let currencyHeaders = collectCurrencyHeaders workbook currencies startCurrencyRow endCurrencyRow
    let mutable operations = []
    for i in 0.. currencyHeaders.Length - 1 do
        let header = currencyHeaders.[i]
        let nextHeader = if i < currencyHeaders.Length - 1 then currencyHeaders.[i + 1] else nextCatHeader
        operations <- operations @ (handleCurrencySection workbook header nextHeader)
        ()
    let date = loadDate workbook
    Ok (date, operations)
