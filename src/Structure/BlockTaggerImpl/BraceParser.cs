using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    internal abstract class BraceParser : IParser
    {
        public BraceParser()
        {
        }

        protected abstract BlockType FindType(CodeBlock parent, string statement);

        public Task<CodeBlock> ParseAsync(ITextSnapshot snapshot, CancellationToken token)
        {
            CodeBlock root = new CodeBlock(null, BlockType.Root, null, new SnapshotSpan(snapshot, 0, snapshot.Length), 0, 0);

            CodeBlock parent = root;

            Stack<CodeBlock> blockOpenings = new Stack<CodeBlock>();

            bool leadingWhitespace = true;
            int statementStart = 0;
            StringBuilder currentStatement = new StringBuilder();
            StringBuilder filteredStatement = new StringBuilder();

            SnapshotFilter filter = new SnapshotFilter(snapshot);
            while (filter.Next())
            {
                int position = filter.Position;
                char c = filter.Character;

                if (leadingWhitespace)
                {
                    leadingWhitespace = char.IsWhiteSpace(c);
                    statementStart = position;
                }

                if (!filter.InQuote)
                {
                    if (c == '{')
                    {
                        CodeBlock child = CreateCodeBlock(parent, currentStatement, filteredStatement, new SnapshotSpan(snapshot, position, 0), statementStart, blockOpenings.Count + 1);

                        blockOpenings.Push(child);

                        parent = child;
                    }
                    else if (c == '}')
                    {
                        if (blockOpenings.Count > 0)
                        {
                            CodeBlock child = blockOpenings.Pop();
                            child.SetSpan(new SnapshotSpan(snapshot, Span.FromBounds(child.Span.Start, position + 1)));

                            parent = child.Parent;
                        }
                    }
                }

                if (filter.EOS)
                {
                    currentStatement.Length = 0;
                    filteredStatement.Length = 0;
                    leadingWhitespace = true;
                }
                else
                {
                    AppendCharacter(currentStatement, c);
                    if (!filter.InQuote)
                        AppendCharacter(filteredStatement, c);
                }

                if (token.IsCancellationRequested)
                    return null;
            }

            while (blockOpenings.Count > 0)
            {
                CodeBlock child = blockOpenings.Pop();
                child.SetSpan(new SnapshotSpan(snapshot, Span.FromBounds(child.Span.Start, snapshot.Length)));
            }

            return Task.FromResult<CodeBlock>(root);
        }

        private CodeBlock CreateCodeBlock(CodeBlock parent, StringBuilder rawStatement, StringBuilder filteredStatement, SnapshotSpan span, int statementStart, int level)
        {
            //There could be up to one trailing space in rawStatement. Pre-emptively remove it.
            if ((rawStatement.Length > 0) && (rawStatement[rawStatement.Length - 1] == ' '))
                rawStatement.Length = rawStatement.Length - 1;

            BlockType type = this.FindType(parent, filteredStatement.ToString());
            CodeBlock child = new CodeBlock(parent, type, rawStatement.ToString(),
                                            span, statementStart, level);

            return child;
        }

        private class SnapshotFilter : QuoteFilter
        {
            private bool _eos;
            private int _braceDepth;
            private Stack<int> _nestedBraceDepth = new Stack<int>();

            public SnapshotFilter(ITextSnapshot snapshot)
                : base(snapshot)
            {
            }

            public new bool Next()
            {
                if (!base.Next())
                    return false;

                _eos = false;
                if (!base.InQuote)
                {
                    char c = base.Character;

                    if (c == ';')
                    {
                        //Whether or not a ; counts as an end of statement depends on context.
                        //      foo();                          <--This does
                        //      for (int i = 0; (i < 10); ++i)  <-- These don't
                        //          bar(delegate{
                        //                  baz();              <-- this does
                        //
                        // Basically, it is an end of statement unless it is contained in an open parenthesis and an open brace
                        // hasn't been encountered since the open paranthesis.
                        _eos = (_nestedBraceDepth.Count == 0) || (_nestedBraceDepth.Peek() < _braceDepth);
                    }
                    else if (c == '(')
                    {
                        _nestedBraceDepth.Push(_braceDepth);
                    }
                    else if (c == ')')
                    {
                        if (_nestedBraceDepth.Count > 0)
                            _nestedBraceDepth.Pop();
                    }
                    else if (c == '{')
                    {
                        ++(_braceDepth);
                        _eos = true;
                    }
                    else if (c == '}')
                    {
                        --(_braceDepth);
                        _eos = true;
                    }
                }

                return true;
            }

            public bool EOS { get { return _eos; } }
        }

        private class QuoteFilter : BaseFilter
        {
            private char _quote = ' ';
            private bool _escape;

            public QuoteFilter(ITextSnapshot snapshot)
                : base(snapshot)
            {
            }

            public bool Next()
            {
                if (++(this.position) < this.snapshot.Length)
                {
                    bool wasEscaped = _escape;
                    _escape = false;

                    char opener = base.Character;
                    if (_quote == ' ')
                    {
                        if (opener == '#')
                        {
                            ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                            this.position = line.End;
                        }
                        else if ((opener == '\'') || (opener == '\"'))
                            _quote = opener;
                        else if (opener == '@')
                        {
                            char next = this.PeekNextChar();
                            if (next == '\"')
                            {
                                _quote = '@';
                                this.position += 1;
                            }
                        }
                        else if (opener == '/')
                        {
                            char next = this.PeekNextChar();
                            if (next == '/')
                            {
                                ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                                this.position = line.End;
                            }
                            else if (next == '*')
                            {
                                this.position += 2;

                                while (this.position < this.snapshot.Length)
                                {
                                    if ((this.snapshot[this.position] == '*') && (this.PeekNextChar() == '/'))
                                    {
                                        this.position += 2;
                                        break;
                                    }

                                    ++(this.position);
                                }
                            }
                        }
                    }
                    else if ((_quote != '@') && (opener == '\\') && !wasEscaped)
                    {
                        _escape = true;
                    }
                    else if (((opener == _quote) || ((opener == '\"') && (_quote == '@'))) && !wasEscaped)
                    {
                        _quote = ' ';
                    }
                    else if ((_quote == '\"') || (_quote == '\''))
                    {
                        ITextSnapshotLine line = this.snapshot.GetLineFromPosition(this.position);
                        if (line.End == this.position)
                        {
                            //End simple quotes at the end of the line.
                            _quote = ' ';
                        }
                    }

                    return (this.position < this.snapshot.Length);
                }

                return false;
            }

            public bool InQuote { get { return (_quote != ' '); } }
        }

        //Return true if statement contains an '=' not contained in (
        public static bool ContainsEquals(string statement)
        {
            int parenthesisDepth = 0;
            for (int i = 0; (i < statement.Length); ++i)
            {
                char c = statement[i];
                if ((c == '=') && (parenthesisDepth == 0))
                    return true;
                else if (c == '(')
                    ++parenthesisDepth;
                else if (c == ')')
                    --parenthesisDepth;
            }

            return false;
        }

        private static void AppendCharacter(StringBuilder statement, char c)
        {
            if (char.IsWhiteSpace(c))
            {
                //Only append whitespace if the previous character isn't a space.
                if ((statement.Length > 0) && (statement[statement.Length - 1] != ' '))
                    statement.Append(' ');  //And substitute a space for any whitespace.
            }
            else
            {
                statement.Append(c);
            }
        }

        public static bool ContainsWord(string text, string p)
        {
            int index = text.IndexOf(p, StringComparison.Ordinal);
            return (index >= 0) &&
                   ((index == 0) || !char.IsLetterOrDigit(text[index - 1])) &&
                   ((index + p.Length == text.Length) || !char.IsLetterOrDigit(text[index + p.Length]));
        }
    }
}
