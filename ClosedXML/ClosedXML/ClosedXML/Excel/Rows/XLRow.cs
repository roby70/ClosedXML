﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ClosedXML.Excel
{
    internal class XLRow: XLRangeBase, IXLRow
    {
        public XLRow(Int32 row, XLRowParameters xlRowParameters)
        {
            SetRowNumber(row);
            Worksheet = xlRowParameters.Worksheet;

            this.IsReference = xlRowParameters.IsReference;
            if (IsReference)
            {
                Worksheet.RangeShiftedRows += new RangeShiftedRowsDelegate(Worksheet_RangeShiftedRows);
            }
            else
            {
                this.style = new XLStyle(this, xlRowParameters.DefaultStyle);
                this.height = xlRowParameters.Worksheet.DefaultRowHeight;
            }
        }

        void Worksheet_RangeShiftedRows(XLRange range, int rowsShifted)
        {
            if (range.FirstAddressInSheet.RowNumber <= this.RowNumber())
                SetRowNumber(this.RowNumber() + rowsShifted);
        }

        void RowsCollection_RowShifted(int startingRow, int rowsShifted)
        {
            if (startingRow <= this.RowNumber())
                SetRowNumber(this.RowNumber() + rowsShifted);
        }

        private void SetRowNumber(Int32 row)
        {
            FirstAddressInSheet = new XLAddress(row, 1);
            LastAddressInSheet = new XLAddress(row, XLWorksheet.MaxNumberOfColumns);
        }


        public Boolean IsReference { get; private set; }

        #region IXLRow Members

        private Double height;
        public Double Height 
        {
            get
            {
                if (IsReference)
                {
                    return Worksheet.Internals.RowsCollection[this.RowNumber()].Height;
                }
                else
                {
                    return height;
                }
            }
            set
            {
                if (IsReference)
                {
                    Worksheet.Internals.RowsCollection[this.RowNumber()].Height = value;
                }
                else
                {
                    height = value;
                }
            }
        }

        public void Delete()
        {
            var rowNumber = this.RowNumber();
            this.AsRange().Delete(XLShiftDeletedCells.ShiftCellsUp);
            Worksheet.Internals.RowsCollection.Remove(rowNumber);
        }

        public Int32 RowNumber()
        {
            return this.FirstAddressInSheet.RowNumber;
        }

        public void InsertRowsBelow(Int32 numberOfRows)
        {
            var rowNum = this.RowNumber();
            this.Worksheet.Internals.RowsCollection.ShiftRowsDown(rowNum + 1, numberOfRows);
            XLRange range = (XLRange)this.Worksheet.Row(rowNum).AsRange();
            range.InsertRowsBelow(numberOfRows, true);
        }

        public void InsertRowsAbove(Int32 numberOfRows)
        {
            var rowNum = this.RowNumber();
            this.Worksheet.Internals.RowsCollection.ShiftRowsDown(rowNum, numberOfRows);
            // We can't use this.AsRange() because we've shifted the rows
            // and we want to use the old rowNum.
            XLRange range = (XLRange)this.Worksheet.Row(rowNum).AsRange(); 
            range.InsertRowsAbove(numberOfRows, true);
        }

        public void Clear()
        {
            var range = this.AsRange();
            range.Clear();
            this.Style = Worksheet.Style;
        }

        #endregion


        #region IXLStylized Members

        private IXLStyle style;
        public override IXLStyle Style
        {
            get
            {
                if (IsReference)
                    return Worksheet.Internals.RowsCollection[this.RowNumber()].Style;
                else
                    return style;
            }
            set
            {
                if (IsReference)
                {
                    Worksheet.Internals.RowsCollection[this.RowNumber()].Style = value;
                }
                else
                {
                    style = new XLStyle(this, value);

                    var row = this.RowNumber();
                    foreach (var c in Worksheet.Internals.CellsCollection.Values.Where(c => c.Address.RowNumber == row))
                    {
                        c.Style = value;
                    }

                    var maxColumn = 0;
                    if (Worksheet.Internals.ColumnsCollection.Count > 0)
                        maxColumn = Worksheet.Internals.ColumnsCollection.Keys.Max();


                    for (var co = 1; co <= maxColumn; co++)
                    {
                        Worksheet.Cell(row, co).Style = value;
                    }
                }
            }
        }

        public override IEnumerable<IXLStyle> Styles
        {
            get
            {
                UpdatingStyle = true;

                yield return Style;

                var row = this.RowNumber();

                foreach (var c in Worksheet.Internals.CellsCollection.Values.Where(c => c.Address.RowNumber == row))
                {
                    yield return c.Style;
                }
                
                var maxColumn = 0;
                if (Worksheet.Internals.ColumnsCollection.Count > 0)
                    maxColumn = Worksheet.Internals.ColumnsCollection.Keys.Max();

                for (var co = 1; co <= maxColumn; co++)
                {
                    yield return Worksheet.Cell(row, co).Style;
                }

                UpdatingStyle = false;
            }
        }

        public override Boolean UpdatingStyle { get; set; }

        public override IXLRange AsRange()
        {
            return Range(1, 1, 1, XLWorksheet.MaxNumberOfColumns);
        }

        #endregion
    }
}
