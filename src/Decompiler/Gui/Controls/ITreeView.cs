﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContextMenu = System.Windows.Forms.ContextMenu;

namespace Decompiler.Gui.Controls
{
    public interface ITreeView
    {
        event EventHandler AfterSelect;

        bool Focused { get; }
        ITreeNode SelectedNode { get; set; }
        bool ShowNodeToolTips { get; set; }
        bool ShowRootLines { get; set; }

        ITreeNodeCollection Nodes { get; }
        ContextMenu ContextMenu { get; set; }

        ITreeNode CreateNode();
        ITreeNode CreateNode(string text);
    }

    public interface ITreeNodeCollection : IList<ITreeNode>
    {
        ITreeNode Add(string text);
        void AddRange(IEnumerable<ITreeNode> nodes);
    }

    public interface ITreeNode
    {
        ITreeNodeCollection Nodes { get; }
        string ImageName { get; set; }
        object Tag { get; set; }
        string Text { get; set; }
        string ToolTipText { get; set; }

        void Expand();
    }
}