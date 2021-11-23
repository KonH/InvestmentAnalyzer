module private InvestmentAnalyzer.Importer.PdfUtils

open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text
open iText.Kernel.Pdf
open iText.Kernel.Pdf.Canvas.Parser
open iText.Kernel.Pdf.Canvas.Parser.Listener

type CustomExtractStrategy() as self =
    inherit LocationTextExtractionStrategy()
    member this.getLocationalResultField() : FieldInfo =
       let t = typedefof<LocationTextExtractionStrategy>
       t.GetField("locationalResult", BindingFlags.Instance ||| BindingFlags.NonPublic)

    member this.startWithSpace (str: string) =
            (str.Length <> 0) && (str.[0] = ' ')

    member this.endsWithSpace (str: string) =
            (str.Length <> 0) && (str.[str.Length - 1] = ' ')

    member this.sameLine (chunk: TextChunk, lastChunk: TextChunk) =
            chunk.GetLocation().SameLine(lastChunk.GetLocation())

    member this.shouldAddTab (chunk: TextChunk, lastChunk: TextChunk) =
        let startWithSpace = this.startWithSpace(chunk.GetText())
        let endsWithSpace = this.endsWithSpace(lastChunk.GetText())
        this.IsChunkAtWordBoundary(chunk, lastChunk) && not startWithSpace && not endsWithSpace

    override u.GetResultantText() : string =
        // Hack to add additional separators
        base.GetResultantText() |> ignore // To trigger common actions
        let field = self.getLocationalResultField()
        let locationalResult = field.GetValue(self) :?> IEnumerable<TextChunk>
        let textChunks = List<TextChunk>(locationalResult)
        let mutable prevChunk : TextChunk = null
        let stringBuilder = StringBuilder()
        for chunk in textChunks do
            match prevChunk with
            | null -> ()
            |  _ when self.sameLine(chunk, prevChunk) ->
                if self.shouldAddTab(chunk, prevChunk) then stringBuilder.Append('\t') |> ignore else ()
            | _ -> stringBuilder.Append('\n') |> ignore
            stringBuilder.Append(chunk.GetText()) |> ignore
            prevChunk <- chunk
        stringBuilder.ToString()

let loadDocument (stream : Stream) =
    let reader = new PdfReader(stream)
    new PdfDocument(reader)

let readPageUsingCustomStrategy (page: PdfPage) =
    PdfTextExtractor.GetTextFromPage(page, CustomExtractStrategy())

let readAllLines (doc: PdfDocument) =
    seq { 1..doc.GetNumberOfPages() }
    |> Seq.map doc.GetPage
    |> Seq.map readPageUsingCustomStrategy
    |> Seq.map (fun s -> s.Split('\n'))
    |> Seq.concat
    |> List.ofSeq