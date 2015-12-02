using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TelemetryForPPT;

namespace Microsoft.VisualStudio.Text.Formatting.Implementation
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class CommandFilterFactory : IVsTextViewCreationListener
    {
        [Import]
        private IHtmlBuilderService htmlBuilderService { get; set; }

        [Import]
        private IRtfBuilderService rtfBuilderService { get; set; }

        [Import]
        private IEditorOperationsFactoryService editorOperationsFactoryService { get; set; }

        [Import]
        private IEditorPrimitivesFactoryService editorPrimitivesFactoryService { get; set; }

        [Import]
        private ITextUndoHistoryRegistry undoHistoryRegistry { get; set; }

        [Import]
        private IVsEditorAdaptersFactoryService editorAdaptersFactoryService { get; set; }

        [Import]
        private IEditorOptionsFactoryService editorOptionsFactoryService { get; set; }

        private ITelemetrySession _telemetrySession;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView textView = editorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            ITextUndoHistory undoHistory = undoHistoryRegistry.RegisterHistory(textView.TextBuffer);
            IEditorOperations editorOperations = editorOperationsFactoryService.GetEditorOperations(textView);
            IEditorOptions editorOptions = editorOptionsFactoryService.GetOptions(textView);
            IViewPrimitives viewPrimitives = editorPrimitivesFactoryService.GetViewPrimitives(textView);

            if (_telemetrySession == null)
            {
                _telemetrySession = TelemetrySessionForPPT.Create(this.GetType().Assembly);
            }

            // Object will survive since it adds itself to the view's command filter chain
            new CommandFilter(
                textViewAdapter,
                textView,
                htmlBuilderService,
                rtfBuilderService,
                editorOperations,
                undoHistory,
                editorOptions,
                viewPrimitives,
                _telemetrySession);
        }
    }
}