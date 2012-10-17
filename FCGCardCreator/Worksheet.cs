using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Worksheet(string title, uint rows, uint cols)
        {
            this.title = title;
            this.rows = rows;
            this.cols = cols;
            data = new Cell[rows,cols];
        }

        public Cell this[uint x, uint y]
        {
            get { return data[x,y]; }
            set { data[x, y] = value; }
        }

        public string GetString(uint x, uint y)
        {
            var val = this[x, y].Value;
            return (val == null) ? "" : val.ToString();
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
