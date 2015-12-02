using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    public class CodeBlock : IBlockTag
    {
        private SnapshotSpan _span;
        private readonly CodeBlock _parent;
        private readonly IList<CodeBlock> _children = new List<CodeBlock>();
        private readonly string _statement;
        private readonly BlockType _type;
        private readonly int _level;
        private readonly int _statementStart;

        public CodeBlock(CodeBlock parent, BlockType type, string statement, SnapshotSpan span, int statementStart, int level)
        {
            _parent = parent;
            if (parent != null)
            {
                parent._children.Add(this);
            }

            _statement = statement;
            _type = type;

            _span = span;
            _statementStart = statementStart;
            _level = level;
        }

        public void SetSpan(SnapshotSpan span)
        {
            _span = span;
        }

        public CodeBlock Parent
        {
            get { return _parent; }
        }

        public IList<CodeBlock> Children
        {
            get { return _children; }
        }

        public SnapshotSpan Span
        {
            get { return _span; }
        }

        public string Statement
        {
            get { return _statement; }
        }

        public BlockType Type
        {
            get { return _type; }
        }

        IBlockTag IBlockTag.Parent
        {
            get
            {
                return _parent;
            }
        }

        public int Level
        {
            get { return _level; }
        }

        public SnapshotPoint StatementStart
        {
            get { return new SnapshotPoint(this.Span.Snapshot, _statementStart); }
        }

        public FrameworkElement Context(BlockColoring coloring, TextRunProperties properties)
        {
            CodeBlock context = this;
            Stack<CodeBlock> stack = new Stack<CodeBlock>();
            while (true)
            {
                if (context._type == BlockType.Root)
                    break;
                if (context._type != BlockType.Unknown)
                {
                    stack.Push(context);
                }

                context = context._parent;
                if (context._type == BlockType.Namespace)
                    break;
            }

            int indent = 0;
            StringBuilder b = new StringBuilder();
            while (true)
            {
                context = stack.Pop();
                b.Append(context._statement);

                indent += 2;
                if (stack.Count != 0)
                {
                    b.Append('\r');
                    b.Append(' ', indent);
                }
                else
                {
                    break;
                }
            }
            return new TextBlob(FormatStatements(b.ToString(), coloring, properties));
        }

        private static FormattedText FormatStatements(string tipText, BlockColoring coloring, TextRunProperties properties)
        {
            FormattedText formattedText = new FormattedText(tipText,
                                           CultureInfo.InvariantCulture,
                                           FlowDirection.LeftToRight,
                                           properties.Typeface,
                                           properties.FontRenderingEmSize,
                                           properties.ForegroundBrush);

            if (coloring != null)
            {
                string[] loopKeywords = new string[] { "for", "while", "do", "foreach", "For", "While", "Do", "Loop", "Until", "End While" };
                string[] ifKeywords = new string[] { "if", "else", "switch", "If", "Else", "ElseIf", "End If" };
                string[] methodKeywords = new string[] { "private", "public", "protected", "internal", "sealed", "static",
                                                     "new", "override",
                                                     "int", "double", "void", "bool",
                                                     "Sub", "Function", "Module", "Class", "Property", "Get", "Set",
                                                     "Private", "Public",
                                                     "End Sub", "End Function", "End Module", "End Class", "End Property", "End Get", "End Set"};

                SetColors(coloring.GetToolTipBrush(BlockType.Loop), loopKeywords, tipText, formattedText);
                SetColors(coloring.GetToolTipBrush(BlockType.Conditional), ifKeywords, tipText, formattedText);
                SetColors(coloring.GetToolTipBrush(BlockType.Method), methodKeywords, tipText, formattedText);
            }
            return formattedText;
        }

        private static void SetColors(Brush brush, string[] keywords, string tipText, FormattedText formattedText)
        {
            foreach (string keyword in keywords)
            {
                int index = -1;
                while (true)
                {
                    index = ContainsWord(tipText, keyword, index + 1);
                    if (index == -1)
                        break;

                    formattedText.SetForegroundBrush(brush, index, keyword.Length);
                }
            }
        }

        private static int ContainsWord(string text, string p, int index)
        {
            index = text.IndexOf(p, index);
            if (index == -1)
                return -1;
            else if (((index == 0) || (!char.IsLetterOrDigit(text[index - 1]))) &&
                      ((index + p.Length == text.Length) || (!char.IsLetterOrDigit(text[index + p.Length]))))
            {
                return index;
            }
            else
                return ContainsWord(text, p, index + 1);
        }

        public class TextBlob : FrameworkElement
        {
            private FormattedText _text;
            public TextBlob(FormattedText text)
            {
                _text = text;

                this.Width = text.Width;
                this.Height = text.Height;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                drawingContext.DrawText(_text, new Point(0.0, 0.0));
            }
        }
    }
}
