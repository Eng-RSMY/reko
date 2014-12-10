﻿using System;
using Decompiler.Gui.Windows.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Decompiler.UiPrototype.WinForms
{
    /// <summary>
    /// Renders IL code in colorized text format.
    /// </summary>
    public partial class ProcedureView : Form
    {
        public ProcedureView()
        {
            InitializeComponent();

            this.editorView1.Styles.Add("keyword", new EditorStyle
            {
                Foreground = new SolidBrush(Color.Blue),
            });
            this.editorView1.Styles.Add("fn", new EditorStyle
            {
                Foreground = new SolidBrush(Color.FromArgb(0x00,0x80,0x80)),
                Cursor = Cursors.Hand,
            });
            this.editorView1.Styles.Add("cmt", new EditorStyle
            {
                Foreground = new SolidBrush(Color.FromArgb(0x00, 0x80, 0x00)),
            });
            this.editorView1.Navigate += editorView1_Navigate;
        }

        public TextView EditorView { get { return editorView1; } }

        public class EditorModel : TextViewModel
        {
            public event EventHandler ModelChanged;

            private TextSpan[][] lines;

            public EditorModel(params TextSpan[][] spans)
            {
                this.lines = spans;
            }

            public int LineCount { get { return lines.Length; } }

            public IEnumerable<TextSpan> GetLineSpans(int index)
            { 
                return lines[index];
            }

            public void CacheHint(int index, int count)
            {
            }
        }

        public TextViewModel Model(params TextSpan[][] lines)
        {
            return new EditorModel(lines);
        }

        public TextSpan[] Line(params TextSpan[] spans)
        {
            return spans;
        }

        public TextSpan Span(string text)
        {
            return new EditorSpan(text);
        }

        public TextSpan Link(string text)
        {
            return new EditorSpan(text)
            {
                Style = "fn",
                Tag = text,
            };
        }

        public TextSpan Span(string text, string style)
        {
            return new EditorSpan(text)
            {
                Style = style,
            };
        }

        public class EditorSpan : TextSpan
        {
            private string text;

            public EditorSpan(string text) { this.text = text; }

            public override string GetText()
            {
                return text;
            }
        }

        void editorView1_Navigate(object sender, EditorNavigationArgs e)
        {
            MessageBox.Show(this, "Hello! " + e.Destination);
        }
    }
}