using System;
using System.Linq;
using CopyAsHtml;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TelemetryForPPT;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Text.Formatting.Implementation
{
    /// <summary>
    /// VS Command Filter to intercept the Edit.Copy command
    /// </summary>
    internal sealed class CommandFilter : IOleCommandTarget
    {
        private readonly IOleCommandTarget _nextCommandTargetInChain;
        private readonly IHtmlBuilderService _htmlBuilderService;
        private readonly IRtfBuilderService _rtfBuilderService;
        private readonly IEditorOperations _editorOperations;
        private readonly IViewPrimitives _viewPrimitives;
        private readonly ITextUndoHistory _undoHistory;
        private readonly IWpfTextView _textView;
        private readonly IEditorOptions _editorOptions;
        private readonly ITelemetrySession _telemetrySession;

        // Length above which generation of RTF and HTML will be skipped since the generated data
        // will likely be too large for clipboard
        private const int MAX_CONTENT_LENGTH = 1000000;

        private static readonly Guid s_GUID_VSStd2K = new Guid("{1496A755-94DE-11D0-8C3F-00C04FC2AAE2}");
        private static readonly Guid s_guidIWpfTextViewHost = new Guid("8C40265E-9FDB-4f54-A0FD-EBB72B7D0476");

        public CommandFilter(
            IVsTextView textView,
            IWpfTextView wpfTextView,
            IHtmlBuilderService htmlBuilderService,
            IRtfBuilderService rtfBuilderService,
            IEditorOperations editorOperations,
            ITextUndoHistory undoHistory,
            IEditorOptions editorOptions,
            IViewPrimitives viewPrimitives,
            ITelemetrySession telemetrySession)
        {
            _htmlBuilderService = htmlBuilderService;
            _rtfBuilderService = rtfBuilderService;
            _undoHistory = undoHistory;
            _textView = wpfTextView;
            _editorOperations = editorOperations;
            _editorOptions = editorOptions;
            _viewPrimitives = viewPrimitives;
            _telemetrySession = telemetrySession;

            textView.AddCommandFilter(this, out _nextCommandTargetInChain);
        }

        private IWpfTextView GetWpfTextView(IVsTextView textView)
        {
            IVsUserData userData = (IVsUserData)textView;
            object result;
            userData.GetData(s_guidIWpfTextViewHost, out result);
            var textViewHost = (IWpfTextViewHost)result;
            return textViewHost.TextView;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            bool isCopyHtmlMarkup = false;
            if (pguidCmdGroup == GuidList.CommandSetGuid && nCmdID == 0x100) // Copy Html Markup
            {
                isCopyHtmlMarkup = true;
            }

            // If the command is copy or cut, handle it here, otherwise route through the rest of the
            // command handler chain
            var isCopy = IsCopyCommand(pguidCmdGroup, nCmdID);
            var isCut = IsCutCommand(pguidCmdGroup, nCmdID);
            var isLineCut = nCmdID == (uint)VSConstants.VSStd2KCmdID.CUTLINE;

            // respect the "Apply Cut or Copy commands to blank lines when there is no selection" option
            if (_textView.Selection.IsEmpty)
            {
                if (isCut && !_editorOperations.CanCut)
                {
                    return VSConstants.S_OK;
                }

                if (isCopy || isCopyHtmlMarkup)
                {
                    if (IsPointOnBlankViewLine() && !_editorOptions.GetOptionValue(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId))
                    {
                        // all conditions are met: no selection, blank line, option off - bail now
                        return VSConstants.S_OK;
                    }
                }
            }

            if (isCopy || isCopyHtmlMarkup || isCut)
            {
                string html = null;
                string rtf = null;
                string text = null;
                NormalizedSnapshotSpanCollection applicabilitySpans = null;
                bool singleLineOperation;
                bool isBoxCopy = _textView.Selection.Mode == TextSelectionMode.Box;

                int tabSize = _editorOptions.GetOptionValue<int>(DefaultOptions.TabSizeOptionId);

                this.GenerateClipboardData(
                    isLineCut,
                    tabSize,
                    out html,
                    out rtf,
                    out text,
                    out applicabilitySpans,
                    out singleLineOperation);

                if (isCut)
                {
                    this.DeleteSpan(applicabilitySpans);
                }

                if (isCopyHtmlMarkup)
                {
                    text = html;
                    html = null;
                    rtf = null;
                    singleLineOperation = false;
                    isBoxCopy = false;
                }

                ClipboardSupport.SetClipboardData(html, rtf, text, singleLineOperation, isBoxCopy);

                _telemetrySession.PostEvent("VS/PPT-CopyAsHTML/Invoked", "VS.PPT-CopyAsHTML.Invoked.IsCopyHtmlMarkup", isCopyHtmlMarkup, "VS.PPT-CopyAsHTML.Invoked.IsCopy", isCopy,
                    "VS.PPT-CopyAsHTML.Invoked.IsCut", isCut, "VS.PPT-CopyAsHTML.Invoked.IsLineCut", isLineCut, "VS.PPT-CopyAsHTML.Invoked.IsBoxCopy", isBoxCopy);

                return VSConstants.S_OK;
            }

            return _nextCommandTargetInChain.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private bool IsPointOnBlankViewLine()
        {
            DisplayTextPoint displayTextPoint = _viewPrimitives.Caret;
            DisplayTextPoint firstTextColumn = displayTextPoint.GetFirstNonWhiteSpaceCharacterOnViewLine();
            return firstTextColumn.CurrentPosition == displayTextPoint.EndOfViewLine;
        }

        private void DeleteSpan(NormalizedSnapshotSpanCollection applicabilitySpans)
        {
            using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction("HTML Cut"))
            {
                _editorOperations.AddBeforeTextBufferChangePrimitive();

                bool successfulEdit = true;

                using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
                {
                    foreach (SnapshotSpan span in applicabilitySpans)
                    {
                        successfulEdit &= edit.Delete(span);
                    }

                    if (successfulEdit)
                    {
                        edit.Apply();
                    }
                }

                _editorOperations.AddAfterTextBufferChangePrimitive();

                if (successfulEdit)
                {
                    undoTransaction.Complete();
                }
            }
        }

        private void GenerateClipboardData(
            bool isLineCut,
            int tabSize,
            out string html,
            out string rtf,
            out string text,
            out NormalizedSnapshotSpanCollection applicabilitySpans,
            out bool singleLineOperation)
        {
            HtmlMarkupProvider.TabSize = tabSize;

            singleLineOperation = _textView.Selection.IsEmpty;

            if (singleLineOperation)
            {
                // Copy or cut current line if selection is empty
                applicabilitySpans = new NormalizedSnapshotSpanCollection(_textView.Caret.ContainingTextViewLine.ExtentIncludingLineBreak);
            }
            else if (isLineCut)
            {
                applicabilitySpans = GetFullLines();
            }
            else
            {
                applicabilitySpans = _textView.Selection.SelectedSpans;
            }

            // Check to make sure a huge range of text is not selected; if so, bail on rtf and html
            bool largeTextRange = !singleLineOperation && applicabilitySpans.Sum(s => s.Length) > MAX_CONTENT_LENGTH;

            if (!largeTextRange)
            {
                html = _htmlBuilderService.GenerateHtml(applicabilitySpans, _textView);
                rtf = _rtfBuilderService.GenerateRtf(applicabilitySpans, _textView);
            }
            else
            {
                // Text is too large, bail on RTF and HTML
                html = rtf = null;
            }

            text = string.Join(_textView.Options.GetNewLineCharacter(), applicabilitySpans.Select(span => span.GetText()));
        }

        private NormalizedSnapshotSpanCollection GetFullLines()
        {
            var selection = _textView.Selection;
            var start = selection.Start;
            var end = selection.End;
            var snapshot = start.Position.Snapshot;
            var startLine = _textView.GetTextViewLineContainingBufferPosition(start.Position);
            var endLine = _textView.GetTextViewLineContainingBufferPosition(end.Position);
            var span = new SnapshotSpan(startLine.Start, endLine.EndIncludingLineBreak - startLine.Start);
            var spanCollection = new NormalizedSnapshotSpanCollection(span);
            return spanCollection;
        }

        private bool IsCopyCommand(Guid pguidCmdGroup, uint nCmdID)
        {
            return pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97
                && nCmdID == (uint)VSConstants.VSStd97CmdID.Copy;
        }

        private bool IsCutCommand(Guid pguidCmdGroup, uint nCmdID)
        {
            bool isCut =
                pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97
                && nCmdID == (uint)VSConstants.VSStd97CmdID.Cut; // 16

            bool isCUT = pguidCmdGroup == s_GUID_VSStd2K
                && (nCmdID == (uint)VSConstants.VSStd2KCmdID.CUT // 58
                    || nCmdID == (uint)VSConstants.VSStd2KCmdID.CUTLINE); // 61

            return isCut || isCUT;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == GuidList.CommandSetGuid && cCmds == 1) // Copy Html Markup
            {
                if (prgCmds[0].cmdID == 0x100)
                {
                    Guid standard97 = VSConstants.GUID_VSStandardCommandSet97;
                    OLECMD[] oleCmds = new OLECMD[1]
                    {
                        new OLECMD()
                        {
                            cmdID = (uint)VSConstants.VSStd97CmdID.Copy,
                            cmdf = prgCmds[0].cmdf
                        }
                    };

                    // call into the standard handler for Copy to see if copy is available
                    _nextCommandTargetInChain.QueryStatus(ref standard97, 1, oleCmds, IntPtr.Zero);
                    prgCmds[0].cmdf = oleCmds[0].cmdf;
                    return VSConstants.S_OK;
                }
            }

            var result = _nextCommandTargetInChain.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            return result;
        }
    }
}
