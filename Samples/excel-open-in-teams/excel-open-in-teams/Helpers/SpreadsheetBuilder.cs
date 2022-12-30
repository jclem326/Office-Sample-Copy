﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;
using We = DocumentFormat.OpenXml.Office2013.WebExtension;
using Wetp = DocumentFormat.OpenXml.Office2013.WebExtentionPane;
using excel_open_in_teams.Models;

namespace excel_open_in_teams.Helpers
{
    public class SpreadsheetBuilder
    {

        /// <summary>
        /// Creates a new spreadsheet containing a sheet with the specified name.
        /// Inserts the product data as a table.
        /// </summary>
        /// <param name="name">The name of the sheet to add to the workbook.</param>
        /// <param name="productData">The product data values to add to the spreadsheet.</param>
        /// <returns>A base64 byte array representing the entire spreadsheet.</returns>
        public byte[] CreateSpreadsheet(string name, IEnumerable<Product> productData)
        {
            var stream = new MemoryStream();
            // By default, AutoSave = true, Editable = true, and Type = xlsx.
            var spreadsheetDocument =
                SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);

            // Add a WorkbookPart to the document.
            var workbookpart = spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();

            // Add a WorksheetPart to the WorkbookPart.
            var worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            // Add Sheets to the Workbook.
            var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            // Append a new worksheet and associate it with the workbook.
            var sheet = new Sheet()
            { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = name };
            sheets.Append(sheet);
            workbookpart.Workbook.Save();

            // Get the sheetData table for the worksheet and insert the table header and product data
            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            InsertHeader(sheetData);
            InsertData(2, 1, sheetData, productData);

            //Embed the script lab add-in
            EmbedAddin(spreadsheetDocument);

            //save and close
            workbookpart.Workbook.Save();
            spreadsheetDocument.Close();

            //Convert stream to base64 and return it
            var retVal = stream.ToArray();
            return retVal;
        }


        /// <summary>
        /// Inserts the header row for the product data table at A1 position
        /// </summary>
        /// <param name="sheetData">Reference to the sheetData section to insert the row.</param>
        private void InsertHeader(SheetData sheetData)
        {
            InsertCellValue(sheetData, 1, "A1", "Product", CellValues.String);
            InsertCellValue(sheetData, 1, "B1", "Qtr1", CellValues.String);
            InsertCellValue(sheetData, 1, "C1", "Qtr2", CellValues.String);
            InsertCellValue(sheetData, 1, "D1", "Qtr3", CellValues.String);
            InsertCellValue(sheetData, 1, "E1", "Qtr4", CellValues.String);
        }

        /// <summary>
        /// Constructs a cell name from row and column numbers into an Excel string format like "AB21"
        /// </summary>
        /// <param name="row">row number value</param>
        /// <param name="col">column number value</param>
        /// <returns>Excel string form of the cell name</returns>
        private string ToCellName(uint row, uint col)
        {
            //Column is alpha based composed of big letter and small leter like AC or BN
            //Special case is first set of A..Z in which case big letter will be a space.
            char bigLetter;
            uint ordinal = col / 26; //26 is Alphabet size
            if (ordinal == 0) bigLetter = ' ';
            else bigLetter = (char)(ordinal + 'A');

            char smallLetter;
            ordinal = (col % 26) - 1; //26 is Alphabet size
            smallLetter = (char)(ordinal + 'A');

            string answer = bigLetter.ToString() + smallLetter.ToString() + row.ToString();
            return answer.Trim();
        }

        /// <summary>
        /// Insert the data rows into a table at specified row,col position.
        /// </summary>
        /// <param name="row">The row index to start at</param>
        /// <param name="col">The column index to start at</param>
        /// <param name="sheetData">The sheetData to insert into</param>
        /// <param name="productData">The product values to insert</param>
        private void InsertData(uint row, uint col, SheetData sheetData, IEnumerable<Product> productData)
        {
            foreach (Product p in productData)
            {
                InsertCellValue(sheetData, row, ToCellName(row, col), p.Name, CellValues.String);
                InsertCellValue(sheetData, row, ToCellName(row, col + 1), p.Qtr1.ToString(), CellValues.Number);
                InsertCellValue(sheetData, row, ToCellName(row, col + 2), p.Qtr2.ToString(), CellValues.Number);
                InsertCellValue(sheetData, row, ToCellName(row, col + 3), p.Qtr3.ToString(), CellValues.Number);
                InsertCellValue(sheetData, row, ToCellName(row, col + 4), p.Qtr4.ToString(), CellValues.Number);
                row++;
            }
        }

        /// <summary>
        /// Inserts a cell value into the specified location of the sheet.
        /// </summary>
        /// <param name="sheetData">The sheetData to insert into</param>
        /// <param name="rowIndex">The row index location to insert into</param>
        /// <param name="cellName">String name of the cell to insert into</param>
        /// <param name="value">The value to insert</param>
        /// <param name="type">The type of the value to insert</param>
        private void InsertCellValue(SheetData sheetData, uint rowIndex, string cellName, string value, CellValues type)
        {
            // Add a row to the cell table.
            Row row;
            row = new Row() { RowIndex = rowIndex };
            sheetData.Append(row);

            // In the new row, find the column location to insert a cell.  
            Cell refCell = null;
            foreach (Cell cell in row.Elements<Cell>())
            {
                if (string.Compare(cell.CellReference.Value, cellName, true) > 0)
                {
                    refCell = cell;
                    break;
                }
            }

            // Add the cell to the cell table at A1.
            Cell newCell = new Cell() { CellReference = cellName };
            row.InsertBefore(newCell, refCell);

            // Set the cell value to be a numeric value of 100.
            newCell.CellValue = new CellValue(value);
            newCell.DataType = new EnumValue<CellValues>(type);

        }


        /*
        * Except for code enclosed in "CUSTOM MODIFICATION", all code and comments below this point 
         * were generated by the Open XML SDK 2.5 Productivity Tool which you can get from here:
        * https://www.microsoft.com/en-us/download/details.aspx?id=30425
        */

        // Adds child parts and generates content of the specified part.
        private void CreateWebExTaskpanesPart(WebExTaskpanesPart part)
        {
            WebExtensionPart webExtensionPart1 = part.AddNewPart<WebExtensionPart>("rId1");
            GenerateWebExtensionPart1Content(webExtensionPart1);

            GeneratePartContent(part);
        }

        // Generates content of webExtensionPart1.
        private void GenerateWebExtensionPart1Content(WebExtensionPart webExtensionPart1)
        {
            We.WebExtension webExtension1 = new We.WebExtension() { Id = "{635BF0CD-42CC-4174-B8D2-6D375C9A759E}" };
            webExtension1.AddNamespaceDeclaration("we", "http://schemas.microsoft.com/office/webextensions/webextension/2010/11");
            We.WebExtensionStoreReference webExtensionStoreReference1 = new We.WebExtensionStoreReference() { Id = "wa104380862", Version = "1.1.0.0", Store = "en-US", StoreType = "OMEX" };
            We.WebExtensionReferenceList webExtensionReferenceList1 = new We.WebExtensionReferenceList();

            We.WebExtensionPropertyBag webExtensionPropertyBag1 = new We.WebExtensionPropertyBag();

            // Add the property that makes the taskpane visible.
            We.WebExtensionProperty webExtensionProperty1 = new We.WebExtensionProperty() { Name = "Office.AutoShowTaskpaneWithDocument", Value = "true" };
            webExtensionPropertyBag1.Append(webExtensionProperty1);

            // CUSTOM MODIFICATION BEGIN
            // Add the property that specifies the snippet to import.
            string snippetToImportValue = string.Format("{{\"type\":\"gist\",\"id\":\"{0}\"}}", "{72189570-AE11-4207-9DEE-C8BDE4B83188}");
            We.WebExtensionProperty webExtensionProperty2 = new We.WebExtensionProperty() { Name = "SnippetToImport", Value = snippetToImportValue };
            webExtensionPropertyBag1.Append(webExtensionProperty2);
            // CUSTOM MODIFICATION END

            We.WebExtensionBindingList webExtensionBindingList1 = new We.WebExtensionBindingList();

            We.Snapshot snapshot1 = new We.Snapshot();
            snapshot1.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

            webExtension1.Append(webExtensionStoreReference1);
            webExtension1.Append(webExtensionReferenceList1);
            webExtension1.Append(webExtensionPropertyBag1);
            webExtension1.Append(webExtensionBindingList1);
            webExtension1.Append(snapshot1);

            webExtensionPart1.WebExtension = webExtension1;
        }

        // Generates content of part.
        private void GeneratePartContent(WebExTaskpanesPart part)
        {
            Wetp.Taskpanes taskpanes1 = new Wetp.Taskpanes();
            taskpanes1.AddNamespaceDeclaration("wetp", "http://schemas.microsoft.com/office/webextensions/taskpanes/2010/11");

            Wetp.WebExtensionTaskpane webExtensionTaskpane1 = new Wetp.WebExtensionTaskpane() { DockState = "right", Visibility = true, Width = 350D, Row = (UInt32Value)4U };

            Wetp.WebExtensionPartReference webExtensionPartReference1 = new Wetp.WebExtensionPartReference() { Id = "rId1" };
            webExtensionPartReference1.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

            webExtensionTaskpane1.Append(webExtensionPartReference1);

            taskpanes1.Append(webExtensionTaskpane1);

            part.Taskpanes = taskpanes1;
        }

        // Embeds the add-in into a file of the specified type.
        private void EmbedAddin(SpreadsheetDocument spreadsheet)
        {
            spreadsheet.DeletePart(spreadsheet.WebExTaskpanesPart);
            var webExTaskpanesPart = spreadsheet.AddWebExTaskpanesPart();
            CreateWebExTaskpanesPart(webExTaskpanesPart);

        }
    }
}
