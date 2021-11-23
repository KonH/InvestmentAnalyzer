module InvestmentAnalyzer.Importer.Common

type StateEntry = {
    ISIN: string
    Name: string
    Currency: string
    Count: float
    TotalPrice: float
    PricePerUnit: float
}

// TODO: make data layout
type OperationEntry = {
    None: float
}