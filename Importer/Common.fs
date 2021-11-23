module InvestmentAnalyzer.Importer.Common

open System

type StateEntry = {
    ISIN: string
    Name: string
    Currency: string
    Count: float
    TotalPrice: float
    PricePerUnit: float
}

type OperationType =
    | In
    | Out
    | Ignored

type OperationEntry = {
    Date: DateTime
    Type: OperationType
    Currency: string
    Volume: float
}