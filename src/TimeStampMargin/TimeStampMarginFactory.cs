using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.PowerTools.TimeStampMargin
{
    #region TimeStampMargin Factory
    /// <summary>
    /// Export a <see cref="IWpfTextViewMarginProvider"/>, which returns an instance of the margin for the editor
    /// to use.
    /// </summary>
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(TimeStampMargin.MarginName)]
    [MarginContainer(PredefinedMarginNames.LeftSelection)]
    [Order(Before = PredefinedMarginNames.Spacer)]
    [ContentType("DebugOutput")]                        //Only for the debug output window. Use text instead of DebugOutput for general testing.
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [DeferCreation(OptionName = TimeStampMarginEnabled.StaticName)]     // Don't enable the margin if the TimeStampMarginEnabled option is set to false.
    internal sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        internal IClassificationFormatMapService ClassificationFormatMappingService { get; private set; }

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; private set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            return new TimeStampMargin(textViewHost.TextView, this);
        }
    }
    #endregion
}
