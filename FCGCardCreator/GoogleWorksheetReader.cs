using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace FCGCardCreator
{
    class GoogleWorksheetReader
    {
        public static Worksheet Read(WorksheetEntry entry, SpreadsheetsService service)
        {
            Worksheet sheet = new Worksheet(entry.Title.Text, entry.Rows, entry.Cols);

            CellQuery cq = new CellQuery(entry.CellFeedLink);
            CellFeed feed = service.Query(cq);

            foreach (CellEntry cellentry in feed.Entries)
            {
                Cell cell = new Cell();
                double output;
                if (Double.TryParse(cellentry.Cell.Value, out output))
                {
                    cell.Type = DataType.Number;
                    cell.Value = output;
                }
                else
                {
                    cell.Type = DataType.String;
                    cell.Value = cellentry.Cell.Value;
                }
                sheet[cellentry.Cell.Row - 1, cellentry.Cell.Column - 1] = cell;
            }
            return sheet;
        }
    }
}
