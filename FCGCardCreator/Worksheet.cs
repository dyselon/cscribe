using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace FCGCardCreator
{
    enum DataType { String, Number };
    struct Cell
    {
        public DataType Type;
        public object Value;
    }

    class Worksheet
    {
        private string title;
        private uint rows;
        private uint cols;
        private Cell[,] data;

        public Worksheet(WorksheetEntry entry, SpreadsheetsService service)
        {
            this.title = entry.Title.Text;
            this.rows = entry.Rows;
            this.cols = entry.Cols;
            data = new Cell[rows,cols];

            CellQuery cq = new CellQuery(entry.CellFeedLink);
            CellFeed feed = service.Query(cq);

            foreach (CellEntry cellentry in feed.Entries)
            {
                Cell cell = new Cell();
                double output;
                if(Double.TryParse(cellentry.Cell.Value, out output))
                {
                    cell.Type = DataType.Number;
                    cell.Value = output;
                }
                else
                {
                    cell.Type = DataType.String;
                    cell.Value = cellentry.Cell.Value;
                }
                data[cellentry.Cell.Row - 1, cellentry.Cell.Column - 1] = cell;
            }
        }

        public Cell this[uint x, uint y]
        {
            get
            {
                return data[x,y];
            }
        }

        public string GetString(uint x, uint y)
        {
            return this[x, y].Value as string;
        }

        public int? GetInt(uint x, uint y)
        {
            if (this[x, y].Type == DataType.Number)
            {
                return Convert.ToInt32(this[x, y].Value);
            }
            else return null;
        }

        public string Title { get { return title; } }
        public uint Rows { get { return rows; } }
        public uint Cols { get { return cols; } }
    }
}
