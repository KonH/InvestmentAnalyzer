module private Importer.XmlUtils

open System
open System.IO
open System.Xml
open Importer.Utils

let loadXml (stream: Stream) =
    let doc = XmlDocument()
    try
        doc.Load(stream)
        Ok doc
    with
        | e -> [e.ToString()] |> Error

// Xmlns makes harder to use Xpath on that document
let removeNamespace (ns: string) (xml: XmlDocument) =
    let sanitizedXml = XmlDocument()
    xml.OuterXml.Replace($"xmlns=\"{ns}\"", String.Empty) |> sanitizedXml.LoadXml
    sanitizedXml

let getAttributes (node: XmlNode) =
    node.Attributes

let getAttribute (attributeName: string) (attributes: XmlAttributeCollection) =
    attributes.[attributeName]

let getAttributeValue (attributeName: string) (attributes: XmlAttributeCollection) =
    attributes
    |> getAttribute attributeName
    |> fun attr -> attr.Value
    |> getOptionString