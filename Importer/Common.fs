module InvestmentAnalyzer.Importer.Common

open System

type StateEntry = {
    ISIN: string
    Name: string
    Currency: string
    Count: decimal
    TotalPrice: decimal
    PricePerUnit: decimal
}

type OperationType =
    | In
    | Out
    | Ignored

type OperationEntry = {
    Date: DateTime
    Type: string
    Currency: string
    Volume: decimal
}