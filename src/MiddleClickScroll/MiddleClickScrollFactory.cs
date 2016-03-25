
namespace Microsoft.PowerTools.MiddleClickScroll
{
    using System.ComponentModel.Composition;
    using VisualStudio.TelemetryForPPT;
    using VisualStudio.Text.Editor;
    using VisualStudio.Utilities;

    [Export(typeof(IMouseProcessorProvider))]
    [Name("MiddleClickScroll")]
    [Order(Before = "UrlClickMouseProcessor")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class MiddleClickScrollFactory : IMouseProcessorProvider
    {
#pragma warning disable 649
        [Export]
        [Name("MiddleClickScrollLayer")]
        [Order(Before = PredefinedAdornmentLayers.Selection)]
        internal AdornmentLayerDefinition viewLayerDefinition;
#pragma warning restore 649

        private ITelemetrySession _telemetrySession;

        public IMouseProcessor GetAssociatedProcessor(IWpfTextView textView)
        {
            if (_telemetrySession == null)
            {
                _telemetrySession = TelemetrySessionForPPT.Create(this.GetType().Assembly);
            }

            return MiddleClickScroll.Create(textView, _telemetrySession);
        }
    }
}
