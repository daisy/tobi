/***************************************************************************

Copyright (c) Microsoft Corporation 2012-2013.

This code is licensed using the Microsoft Public License (Ms-PL).  The text of the license can be found here:

http://www.microsoft.com/resources/sharedsource/licensingbasics/publiclicense.mspx

Published at http://OpenXmlDeveloper.org
Resource Center and Documentation: http://openxmldeveloper.org/wiki/w/wiki/powertools-for-open-xml.aspx

Developer: Eric White
Blog: http://www.ericwhite.com
Twitter: @EricWhiteDev
Email: eric@ericwhite.com

Version: 2.6.00
 * Enhancements to support HtmlConverter.cs

***************************************************************************/

/*
Here is modification of a WmlDocument:
    public static WmlDocument SimplifyMarkup(WmlDocument doc, SimplifyMarkupSettings settings)
    {
        using (OpenXmlMemoryStreamDocument streamDoc = new OpenXmlMemoryStreamDocument(doc))
        {
            using (WordprocessingDocument document = streamDoc.GetWordprocessingDocument())
            {
                SimplifyMarkup(document, settings);
            }
            return streamDoc.GetModifiedWmlDocument();
        }
    }

Here is read-only of a WmlDocument:

    public static string GetBackgroundColor(WmlDocument doc)
    {
        using (OpenXmlMemoryStreamDocument streamDoc = new OpenXmlMemoryStreamDocument(doc))
        using (WordprocessingDocument document = streamDoc.GetWordprocessingDocument())
        {
            XDocument mainDocument = document.MainDocumentPart.GetXDocument();
            XElement backgroundElement = mainDocument.Descendants(W.background).FirstOrDefault();
            return (backgroundElement == null) ? string.Empty : backgroundElement.Attribute(W.color).Value;
        }
    }

Here is creating a new WmlDocument:

    private OpenXmlPowerToolsDocument CreateSplitDocument(WordprocessingDocument source, List<XElement> contents, string newFileName)
    {
        using (OpenXmlMemoryStreamDocument streamDoc = OpenXmlMemoryStreamDocument.CreateWordprocessingDocument())
        {
            using (WordprocessingDocument document = streamDoc.GetWordprocessingDocument())
            {
                DocumentBuilder.FixRanges(source.MainDocumentPart.GetXDocument(), contents);
                PowerToolsExtensions.SetContent(document, contents);
            }
            OpenXmlPowerToolsDocument newDoc = streamDoc.GetModifiedDocument();
            newDoc.FileName = newFileName;
            return newDoc;
        }
    }
*/

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.IO.Packaging;
using DocumentFormat.OpenXml.Packaging;

namespace OpenXmlPowerTools
{
    public class PowerToolsDocumentException : Exception
    {
        public PowerToolsDocumentException(string message) : base(message) { }
    }
    public class PowerToolsInvalidDataException : Exception
    {
        public PowerToolsInvalidDataException(string message) : base(message) { }
    }

    public class OpenXmlPowerToolsDocument
    {
        public string FileName { get; set; }
        public byte[] DocumentByteArray { get; set; }

        public static OpenXmlPowerToolsDocument FromFileName(string fileName)
        {
            byte[] bytes = File.ReadAllBytes(fileName);
            Type type;
            try
            {
                type = GetDocumentType(bytes);
            }
            catch (FileFormatException)
            {
                throw new PowerToolsDocumentException("Not an Open XML document.");
            }
            if (type == typeof(WordprocessingDocument))
                return new WmlDocument(fileName, bytes);
            if (type == typeof(SpreadsheetDocument))
                return new SmlDocument(fileName, bytes);
            if (type == typeof(PresentationDocument))
                return new PmlDocument(fileName, bytes);
            if (type == typeof(Package))
            {
                OpenXmlPowerToolsDocument pkg = new OpenXmlPowerToolsDocument(bytes);
                pkg.FileName = fileName;
                return pkg;
            }
            throw new PowerToolsDocumentException("Not an Open XML document.");
        }

        public static OpenXmlPowerToolsDocument FromDocument(OpenXmlPowerToolsDocument doc)
        {
            Type type = doc.GetDocumentType();
            if (type == typeof(WordprocessingDocument))
                return new WmlDocument(doc);
            if (type == typeof(SpreadsheetDocument))
                return new SmlDocument(doc);
            if (type == typeof(PresentationDocument))
                return new PmlDocument(doc);
            return null;    // This should not be possible from a valid OpenXmlPowerToolsDocument object
        }

        public OpenXmlPowerToolsDocument(OpenXmlPowerToolsDocument original)
        {
            DocumentByteArray = new byte[original.DocumentByteArray.Length];
            Array.Copy(original.DocumentByteArray, DocumentByteArray, original.DocumentByteArray.Length);
            FileName = original.FileName;
        }

        public OpenXmlPowerToolsDocument(string fileName)
        {
            this.FileName = fileName;
            DocumentByteArray = File.ReadAllBytes(fileName);
        }

        public OpenXmlPowerToolsDocument(byte[] byteArray)
        {
            DocumentByteArray = new byte[byteArray.Length];
            Array.Copy(byteArray, DocumentByteArray, byteArray.Length);
            this.FileName = null;
        }

        public OpenXmlPowerToolsDocument(string fileName, MemoryStream memStream)
        {
            FileName = fileName;
            DocumentByteArray = new byte[memStream.Length];
            Array.Copy(memStream.GetBuffer(), DocumentByteArray, memStream.Length);
        }

        public string GetName()
        {
            if (FileName == null)
                return "Unnamed Document";
            FileInfo file = new FileInfo(FileName);
            return file.Name;
        }

        public void SaveAs(string fileName)
        {
            File.WriteAllBytes(fileName, DocumentByteArray);
        }

        public void Save()
        {
            if (this.FileName == null)
                throw new InvalidOperationException("Attempting to Save a document that has no file name.  Use SaveAs instead.");
            File.WriteAllBytes(this.FileName, DocumentByteArray);
        }

        public void WriteByteArray(Stream stream)
        {
            stream.Write(DocumentByteArray, 0, DocumentByteArray.Length);
        }

        public Type GetDocumentType()
        {
            return GetDocumentType(DocumentByteArray);
        }

        private static Type GetDocumentType(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            using (Package package = Package.Open(stream, FileMode.Open))
            {
                PackageRelationship relationship = package.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument").FirstOrDefault();
                if (relationship != null)
                {
                    PackagePart part = package.GetPart(PackUriHelper.ResolvePartUri(relationship.SourceUri, relationship.TargetUri));
                    switch (part.ContentType)
                    {
                        case "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml":
                            return typeof(WordprocessingDocument);
                        case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml":
                            return typeof(SpreadsheetDocument);
                        case "application/vnd.openxmlformats-officedocument.presentationml.template.main+xml":
                        case "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml":
                            return typeof(PresentationDocument);
                    }
                    return typeof(Package);
                }
                return null;
            }
        }

        public static void SavePartAs(OpenXmlPart part, string filePath)
        {
            Stream partStream = part.GetStream(FileMode.Open, FileAccess.Read);
            byte[] partContent = new byte[partStream.Length];
            partStream.Read(partContent, 0, (int)partStream.Length);

            File.WriteAllBytes(filePath, partContent);
        }
    }

    public partial class WmlDocument : OpenXmlPowerToolsDocument
    {
        public WmlDocument(OpenXmlPowerToolsDocument original)
            : base(original)
        {
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(string fileName)
            : base(fileName)
        {
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(string fileName, byte[] byteArray)
            : base(byteArray)
        {
            FileName = fileName;
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(string fileName, MemoryStream memStream)
            : base(fileName, memStream)
        {
        }
    }

    public partial class SmlDocument : OpenXmlPowerToolsDocument
    {
        public SmlDocument(OpenXmlPowerToolsDocument original)
            : base(original)
        {
            if (GetDocumentType() != typeof(SpreadsheetDocument))
                throw new PowerToolsDocumentException("Not a Spreadsheet document.");
        }

        public SmlDocument(string fileName)
            : base(fileName)
        {
            if (GetDocumentType() != typeof(SpreadsheetDocument))
                throw new PowerToolsDocumentException("Not a Spreadsheet document.");
        }

        public SmlDocument(string fileName, byte[] byteArray)
            : base(byteArray)
        {
            FileName = fileName;
            if (GetDocumentType() != typeof(SpreadsheetDocument))
                throw new PowerToolsDocumentException("Not a Spreadsheet document.");
        }

        public SmlDocument(string fileName, MemoryStream memStream)
            : base(fileName, memStream)
        {
        }
    }

    public partial class PmlDocument : OpenXmlPowerToolsDocument
    {
        public PmlDocument(OpenXmlPowerToolsDocument original)
            : base(original)
        {
            if (GetDocumentType() != typeof(PresentationDocument))
                throw new PowerToolsDocumentException("Not a Presentation document.");
        }

        public PmlDocument(string fileName)
            : base(fileName)
        {
            if (GetDocumentType() != typeof(PresentationDocument))
                throw new PowerToolsDocumentException("Not a Presentation document.");
        }

        public PmlDocument(string fileName, byte[] byteArray)
            : base(byteArray)
        {
            FileName = fileName;
            if (GetDocumentType() != typeof(PresentationDocument))
                throw new PowerToolsDocumentException("Not a Presentation document.");
        }

        public PmlDocument(string fileName, MemoryStream memStream)
            : base(fileName, memStream)
        {
        }
    }

    public class OpenXmlMemoryStreamDocument : IDisposable
    {
        private OpenXmlPowerToolsDocument Document;
        private MemoryStream DocMemoryStream;
        private Package DocPackage;

        public OpenXmlMemoryStreamDocument(OpenXmlPowerToolsDocument doc)
        {
            Document = doc;
            DocMemoryStream = new MemoryStream();
            doc.WriteByteArray(DocMemoryStream);
            try
            {
                DocPackage = Package.Open(DocMemoryStream, FileMode.Open);
            }
            catch (Exception e)
            {
                throw new PowerToolsDocumentException(e.Message);
            }
        }

        internal OpenXmlMemoryStreamDocument(MemoryStream stream)
        {
            DocMemoryStream = stream;
            try
            {
                DocPackage = Package.Open(DocMemoryStream, FileMode.Open);
            }
            catch (Exception e)
            {
                throw new PowerToolsDocumentException(e.Message);
            }
        }

        public static OpenXmlMemoryStreamDocument CreateWordprocessingDocument()
        {
            MemoryStream stream = new MemoryStream();
            using (WordprocessingDocument doc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                doc.AddMainDocumentPart();
                doc.MainDocumentPart.PutXDocument(new XDocument(
                    new XElement(W.document,
                        new XAttribute(XNamespace.Xmlns + "w", W.w),
                        new XAttribute(XNamespace.Xmlns + "r", R.r),
                        new XElement(W.body))));
                doc.Close();
                return new OpenXmlMemoryStreamDocument(stream);
            }
        }
        public static OpenXmlMemoryStreamDocument CreateSpreadsheetDocument()
        {
            MemoryStream stream = new MemoryStream();
            using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
            {
                doc.AddWorkbookPart();
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XNamespace relationshipsns = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                doc.WorkbookPart.PutXDocument(new XDocument(
                    new XElement(ns + "workbook",
                        new XAttribute("xmlns", ns),
                        new XAttribute(XNamespace.Xmlns + "r", relationshipsns),
                        new XElement(ns + "sheets"))));
                doc.Close();
                return new OpenXmlMemoryStreamDocument(stream);
            }
        }
        public static OpenXmlMemoryStreamDocument CreatePresentationDocument()
        {
            MemoryStream stream = new MemoryStream();
            using (PresentationDocument doc = PresentationDocument.Create(stream, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
            {
                doc.AddPresentationPart();
                XNamespace ns = "http://schemas.openxmlformats.org/presentationml/2006/main";
                XNamespace relationshipsns = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                XNamespace drawingns = "http://schemas.openxmlformats.org/drawingml/2006/main";
                doc.PresentationPart.PutXDocument(new XDocument(
                    new XElement(ns + "presentation",
                        new XAttribute(XNamespace.Xmlns + "a", drawingns),
                        new XAttribute(XNamespace.Xmlns + "r", relationshipsns),
                        new XAttribute(XNamespace.Xmlns + "p", ns),
                        new XElement(ns + "sldMasterIdLst"),
                        new XElement(ns + "sldIdLst"),
                        new XElement(ns + "notesSz", new XAttribute("cx", "6858000"), new XAttribute("cy", "9144000")))));
                doc.Close();
                return new OpenXmlMemoryStreamDocument(stream);
            }
        }

        public static OpenXmlMemoryStreamDocument CreatePackage()
        {
            MemoryStream stream = new MemoryStream();
            Package package = Package.Open(stream, FileMode.Create);
            package.Close();
            return new OpenXmlMemoryStreamDocument(stream);
        }

        public Package GetPackage()
        {
            return DocPackage;
        }

        public WordprocessingDocument GetWordprocessingDocument()
        {
            try
            {
                if (GetDocumentType() != typeof(WordprocessingDocument))
                    throw new PowerToolsDocumentException("Not a Wordprocessing document.");
                return WordprocessingDocument.Open(DocPackage);
            }
            catch (Exception e)
            {
                throw new PowerToolsDocumentException(e.Message);
            }
        }
        public SpreadsheetDocument GetSpreadsheetDocument()
        {
            try
            {
                if (GetDocumentType() != typeof(SpreadsheetDocument))
                    throw new PowerToolsDocumentException("Not a Spreadsheet document.");
                return SpreadsheetDocument.Open(DocPackage);
            }
            catch (Exception e)
            {
                throw new PowerToolsDocumentException(e.Message);
            }
        }

        public PresentationDocument GetPresentationDocument()
        {
            try
            {
                if (GetDocumentType() != typeof(PresentationDocument))
                    throw new PowerToolsDocumentException("Not a Presentation document.");
                return PresentationDocument.Open(DocPackage);
            }
            catch (Exception e)
            {
                throw new PowerToolsDocumentException(e.Message);
            }
        }

        public Type GetDocumentType()
        {
            PackageRelationship relationship = DocPackage.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument").FirstOrDefault();
            if (relationship == null)
                throw new PowerToolsDocumentException("Not an Open XML Document.");
            PackagePart part = DocPackage.GetPart(PackUriHelper.ResolvePartUri(relationship.SourceUri, relationship.TargetUri));
            switch (part.ContentType)
            {
                case "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml":
                    return typeof(WordprocessingDocument);
                case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml":
                    return typeof(SpreadsheetDocument);
                case "application/vnd.openxmlformats-officedocument.presentationml.template.main+xml":
                case "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml":
                    return typeof(PresentationDocument);
            }
            return null;
        }

        public OpenXmlPowerToolsDocument GetModifiedDocument()
        {
            DocPackage.Close();
            DocPackage = null;
            return new OpenXmlPowerToolsDocument((Document == null) ? null : Document.FileName, DocMemoryStream);
        }

        public WmlDocument GetModifiedWmlDocument()
        {
            DocPackage.Close();
            DocPackage = null;
            return new WmlDocument((Document == null) ? null : Document.FileName, DocMemoryStream);
        }

        public SmlDocument GetModifiedSmlDocument()
        {
            DocPackage.Close();
            DocPackage = null;
            return new SmlDocument((Document == null) ? null : Document.FileName, DocMemoryStream);
        }

        public PmlDocument GetModifiedPmlDocument()
        {
            DocPackage.Close();
            DocPackage = null;
            return new PmlDocument((Document == null) ? null : Document.FileName, DocMemoryStream);
        }

        public void Close()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~OpenXmlMemoryStreamDocument()
        {
            Dispose(false);
        }

        private void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                if (DocPackage != null)
                {
                    DocPackage.Close();
                }
                if (DocMemoryStream != null)
                {
                    DocMemoryStream.Dispose();
                }
            }
            if (DocPackage == null && DocMemoryStream == null)
                return;
            DocPackage = null;
            DocMemoryStream = null;
            GC.SuppressFinalize(this);
        }
    }
}
