using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TelemetryForPPT;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;

namespace NoahRichards.AlignAssignments
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class VsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        private IVsEditorAdaptersFactoryService _adaptersFactory = null;


        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var wpfTextView = _adaptersFactory.GetWpfTextView(textViewAdapter);
            if (wpfTextView == null)
            {
                Debug.Fail("Unable to get IWpfTextView from text view adapter");
                return;
            }

            ITelemetrySession telemetrySession = TelemetrySessionForPPT.Create(this.GetType().Assembly);

            CommandFilter filter = new CommandFilter(wpfTextView, shouldBeDisabled: false, telemetrySession: telemetrySession);

            IOleCommandTarget next;
            if (ErrorHandler.Succeeded(textViewAdapter.AddCommandFilter(filter, out next)))
                filter.Next = next;
        }
    }

    internal class CommandFilter : IOleCommandTarget
    {
        private readonly IWpfTextView _view;
        private readonly bool _shouldBeDisabled;
        private readonly ITelemetrySession _telemetrySession;

        public CommandFilter(IWpfTextView view, bool shouldBeDisabled, ITelemetrySession telemetrySession)
        {
            _shouldBeDisabled = shouldBeDisabled;
            _view = view;
            _telemetrySession = telemetrySession;
        }

        internal IOleCommandTarget Next { get; set; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == GuidList.guidAlignAssignmentsCmdSet &&
                nCmdID == PkgCmdIDList.cmdidAlignAssignments)
            {
                if (_shouldBeDisabled)
                    return VSConstants.E_FAIL;

                AlignAssignments();
                return VSConstants.S_OK;
            }

            if (Next != null)
            {
                return Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            else
            {
                return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == GuidList.guidAlignAssignmentsCmdSet &&
                prgCmds[0].cmdID == PkgCmdIDList.cmdidAlignAssignments)
            {
                if (_shouldBeDisabled)
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_INVISIBLE);
                else if (AssignmentsToAlign)
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                else
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED);

                return VSConstants.S_OK;
            }
            if (Next != null)
            {
                return Next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }
            else
            {
                return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
            }
        }

        private void AlignAssignments()
        {
            // Find all lines above and below with = signs
            ITextSnapshot snapshot = _view.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            _telemetrySession.PostEvent("VS/PPT-AlignAssignments/CommandInvoked");

            int currentLineNumber = snapshot.GetLineNumberFromPosition(_view.Caret.Position.BufferPosition);

            Dictionary<int, ColumnAndOffset> lineNumberToEqualsColumn = new Dictionary<int, ColumnAndOffset>();

            // Start with the current line
            ColumnAndOffset columnAndOffset = GetColumnNumberOfFirstEquals(snapshot.GetLineFromLineNumber(currentLineNumber));
            if (columnAndOffset.Column == -1)
                return;

            lineNumberToEqualsColumn[currentLineNumber] = columnAndOffset;

            int lineNumber = currentLineNumber;
            int minLineNumber = 0;
            int maxLineNumber = snapshot.LineCount - 1;

            // If the selection spans multiple lines, only attempt to fix the lines in the selection
            if (!_view.Selection.IsEmpty)
            {
                var selectionStartLine = _view.Selection.Start.Position.GetContainingLine();
                if (_view.Selection.End.Position > selectionStartLine.End)
                {
                    minLineNumber = selectionStartLine.LineNumber;

                    // Get the line number containing the last included portion of the SnapshotSpan.  Remember
                    // that End is not a part of the SnapshotSpan.  It's safe to subtract one here because
                    // the predicate !IsEmpty ensures the length is at least 1
                    maxLineNumber = snapshot.GetLineNumberFromPosition(_view.Selection.End.Position - 1);
                }
            }

            // Moving backwards
            for (lineNumber = currentLineNumber - 1; lineNumber >= minLineNumber; lineNumber--)
            {
                columnAndOffset = GetColumnNumberOfFirstEquals(snapshot.GetLineFromLineNumber(lineNumber));
                if (columnAndOffset.Column == -1)
                    break;

                lineNumberToEqualsColumn[lineNumber] = columnAndOffset;
            }

            // Moving forwards
            for (lineNumber = currentLineNumber + 1; lineNumber <= maxLineNumber; lineNumber++)
            {
                columnAndOffset = GetColumnNumberOfFirstEquals(snapshot.GetLineFromLineNumber(lineNumber));
                if (columnAndOffset.Column == -1)
                    break;

                lineNumberToEqualsColumn[lineNumber] = columnAndOffset;
            }

            // Perform the actual edit
            if (lineNumberToEqualsColumn.Count > 1)
            {
                int columnToIndentTo = lineNumberToEqualsColumn.Values.Max(c => c.Column);

                using (var edit = snapshot.TextBuffer.CreateEdit())
                {
                    foreach (var pair in lineNumberToEqualsColumn.Where(p => p.Value.Column < columnToIndentTo))
                    {
                        ITextSnapshotLine line = snapshot.GetLineFromLineNumber(pair.Key);
                        string spaces = new string(' ', columnToIndentTo - pair.Value.Column);

                        if (!edit.Insert(line.Start.Position + pair.Value.Offset, spaces))
                            return;
                    }

                    edit.Apply();
                }
            }
        }

        private ColumnAndOffset GetColumnNumberOfFirstEquals(ITextSnapshotLine line)
        {
            ITextSnapshot snapshot = line.Snapshot;
            int tabSize = _view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);

            int column = 0;
            int nonWhiteSpaceCount = 0;
            for (int i = line.Start.Position; i < line.End.Position; i++)
            {
                char ch = snapshot[i];
                if (ch == '=')
                    return new ColumnAndOffset()
                    {
                        Column = column,
                        Offset = (i - line.Start.Position) - nonWhiteSpaceCount
                    };

                // For the sake of associating characters with the '=', include only characters
                // that should associate with the =.  Namely, make sure we leave out things that may
                // belong to identifiers and common punctuation that doesn't associate (like parentheses)
                if (!CharAssociatesWithEquals(ch))
                    nonWhiteSpaceCount = 0;
                else
                    nonWhiteSpaceCount++;

                if (ch == '\t')
                    column += tabSize - (column % tabSize);
                else
                    column++;

                // Also, check to see if this is a surrogate pair.  If so, skip the next character by incrementing
                // the loop counter and increment the nonWhiteSpaceCount without incrementing the column
                // count.
                if (char.IsHighSurrogate(ch) &&
                    i < line.End.Position - 1 && char.IsLowSurrogate(snapshot[i + 1]))
                {
                    nonWhiteSpaceCount++;
                    i++;
                }
            }

            return new ColumnAndOffset() { Column = -1, Offset = -1 };
        }

        private struct ColumnAndOffset
        {
            public int Column;
            public int Offset;
        }

        static private bool CharAssociatesWithEquals(char ch)
        {
            HashSet<char> charsThatAssociateWithEquals = new HashSet<char>() { '+', '-', '.', '<', '>', '/', ':', '\\', '*', '&', '^', '%', '$', '#', '@', '!', '~' };
            return charsThatAssociateWithEquals.Contains(ch);
        }

        private bool AssignmentsToAlign
        {
            get
            {
                return _view.Caret.Position.BufferPosition.GetContainingLine().GetText().Contains("=");
            }
        }
    }
}