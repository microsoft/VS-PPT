using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.PowerTools.MatchMargin
{
    /// <summary>
    /// Export a <see cref="IWpfTextViewMarginProvider"/>, which returns an instance of the margin for the editor
    /// to use.
    /// </summary>
    [Export(typeof(IWpfTextViewMarginProvider))]
    [MarginContainer(PredefinedMarginNames.VerticalScrollBar)]
    [Name(MatchMargin.Name)]
    [Order(After = PredefinedMarginNames.OverviewChangeTracking, Before = PredefinedMarginNames.OverviewMark)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [DeferCreation(OptionName = MatchMarginEnabledOption.OptionName)]
    internal sealed class MatchMarginFactory : IWpfTextViewMarginProvider
    {
#pragma warning disable 649
        [Import]
        internal IEditorFormatMapService EditorFormatMapService = null;

        [Export]
        [Name("MatchMarginAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Outlining, Before = PredefinedAdornmentLayers.Selection)]
        internal AdornmentLayerDefinition matchLayerDefinition;
#pragma warning restore 649

        /// <summary>
        /// Create an instance of the MatchMargin in the specified <see cref="IWpfTextViewHost"/>.
        /// </summary>
        /// <param name="textViewHost">The <see cref="IWpfTextViewHost"/> in which the MatchMargin will be displayed.</param>
        /// <returns>The newly created MatchMargin.</returns>
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            var containerMarginAsVerticalScrollBar = containerMargin as IVerticalScrollBar;
            if (containerMarginAsVerticalScrollBar != null)
            {
                return new MatchMargin(textViewHost, containerMarginAsVerticalScrollBar, this);
            }
            else
                return null;
        }
    }
}
