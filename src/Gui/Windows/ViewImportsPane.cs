﻿#region License
/* 
 * Copyright (C) 1999-2017 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using Reko.Core;
using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Reko.Gui.Windows
{
    public class ViewImportsPane : IWindowPane
    {
        private ViewImportsControl control;
        private Comparer comparer;
        private Program program;
        private IServiceProvider services;

        public ViewImportsPane(Program program)
        {
            this.program = program;
        }

        public IWindowFrame Frame { get; set; }

        public void Close()
        {
            if (control != null)
                control.Dispose();
            control = null;
        }

        public Control CreateControl()
        {
            this.control = new ViewImportsControl();
            this.comparer = new Comparer();

            this.control.Load += Control_Load;
            this.control.Imports.MouseMove += Imports_MouseMove;
            this.control.Imports.ColumnClick += Imports_ColumnClick;
            this.control.Imports.ListViewItemSorter = comparer;
            this.control.Imports.MouseClick += Imports_MouseClick;
            return control;
        }

        public void SetSite(IServiceProvider sp)
        {
            this.services = sp;
        }

        private void Control_Load(object sender, EventArgs e)
        {
            this.uiPrefsSvc = services.RequireService<IUiPreferencesService>();
            var items = program.ImportReferences.Values
                .Select(CreateListItem)
                .ToArray();
            control.Imports.Items.Clear();
            control.Imports.Items.AddRange(items);
        }

        private ListViewItem CreateListItem(ImportReference imp)
        {
            var item = new ListViewItem();
            item.Tag = imp;
            item.Text = imp.ReferenceAddress.ToString();
            item.UseItemStyleForSubItems = false;
            item.SubItems[0].Tag = imp.ReferenceAddress;
            item.SubItems.Add(imp.ModuleName);
            var ord = imp as OrdinalImportReference;
            if (ord != null)
            {
                item.SubItems.Add(ord.Ordinal.ToString());
            }
            else
            {
                var nam = imp as NamedImportReference;
                if (nam != null)
                {
                    item.SubItems.Add(nam.ImportName);
                }
            }
            item.SubItems[0].ForeColor = uiPrefsSvc.Styles["link"].Foreground.Color;
            return item;
        }

        ListViewItem.ListViewSubItem mSelected;
        private IUiPreferencesService uiPrefsSvc;

        private void Imports_MouseMove(object sender, MouseEventArgs e)
        {
            var info = control.Imports.HitTest(e.Location);
            if (info.SubItem == mSelected) return;
            if (mSelected != null)
                mSelected.Font = control.Imports.Font;
            mSelected = null;
            this.control.Imports.Cursor = Cursors.Default;
            if (info.SubItem != null && info.Item.SubItems[0] == info.SubItem)
            {
                info.SubItem.Font = new Font(info.SubItem.Font, FontStyle.Underline);
                this.control.Imports.Cursor = Cursors.Hand;
                mSelected = info.SubItem;
            }
        }

        private void Imports_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == comparer.SortColumn)
            {
                // Same column as before, invert the sort order.
                comparer.Order = comparer.Order == SortOrder.Ascending
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                comparer.SortColumn = e.Column;
                comparer.Order = SortOrder.Ascending;
            }
            control.Imports.Sort();
        }

        private void Imports_MouseClick(object sender, MouseEventArgs e)
        {
            var info = control.Imports.HitTest(e.X, e.Y);
            var row = info.Item.Index;
            if (info.SubItem != null)
            {
                var addr = info.SubItem.Tag as Address;
                services.RequireService<ILowLevelViewService>()
                    .ShowMemoryAtAddress(program, addr);
            }
        }

        private class Comparer : IComparer
        {
            public Comparer()
            {
                this.SortColumn = 0;
                this.Order = SortOrder.Ascending;
            }

            public int SortColumn { get; set; }
            public SortOrder Order { get; set; }

            public int Compare(object x, object y)
            {
                var itemX = (ListViewItem)x;
                var itemY = (ListViewItem)y;
                var importX = (ImportReference)itemX.Tag;
                var importY = (ImportReference)itemY.Tag;
                int cmp;
                switch (SortColumn)
                {
                case 0:
                    cmp = importX.ReferenceAddress.CompareTo(importY.ReferenceAddress);
                    break;
                case 1:
                    cmp = importX.ModuleName.CompareTo(importY.ModuleName);
                    break;
                case 2:
                    cmp = importX.CompareTo(importY);
                    break;
                default:
                    cmp = 0;
                    break;
                }
                if (Order == SortOrder.Descending)
                    cmp = -cmp;
                return cmp;
            }
        }
    }
}