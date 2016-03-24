using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.OverviewMargin;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.PowerTools.StructureVisualizer
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(StructureMargin.Name)]
    [Order(After = PredefinedMarginNames.OverviewError)]
    [Order(Before = PredefinedMarginNames.OverviewSourceImage)]
    [MarginContainer(PredefinedMarginNames.VerticalScrollBar)]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class StructureMarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactoryService { get; set; }

        [Import]
        internal IEditorFormatMapService EditorFormatMapService { get; set; }

        /// <summary>
        /// Create an instance of the StructureMargin in the specified <see cref="IWpfTextViewHost"/>.
        /// </summary>
        /// <param name="textViewHost">The <see cref="IWpfTextViewHost"/> in which the StructureMargin will be displayed.</param>
        /// <param name="containerMargin">The scrollBar used to translate between buffer positions and y-coordinates.</param>
        /// <returns>The newly created StructureMargin.</returns>
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            var scrolBar = containerMargin as IVerticalScrollBar;
            if (scrolBar != null)
            {
                //Create the caret margin, passing it a newly instantiated text structure navigator for the view.
                return new StructureMargin(textViewHost, scrolBar, this);
            }
            else
                return null;
        }
    }

    [Export(typeof(IOverviewTipManagerProvider))]
    [Name(StructureMargin.Name)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class StructureTipFactory : IOverviewTipManagerProvider
    {
        public IOverviewTipManager GetOverviewTipManager(IWpfTextViewHost host)
        {
            return host.GetTextViewMargin(StructureMargin.Name) as IOverviewTipManager;
        }
    }
}
