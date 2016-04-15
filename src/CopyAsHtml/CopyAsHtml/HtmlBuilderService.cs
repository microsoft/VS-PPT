using System.ComponentModel.Composition;
using CopyAsHtml;
using EnvDTE;
using Microsoft.VisualStudio.Language.Intellisense.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Formatting.Implementation
{
    [Export(typeof(IHtmlBuilderService))]
    public sealed class HtmlBuilderService : IHtmlBuilderService
    {
        /// <summary>
        /// Returns an IClassificationType from its string name, e.g. "text"
        /// </summary>
        [Import]
        private IClassificationTypeRegistryService _classificationTypeRegistry { get; set; }

        /// <summary>
        /// Returns an IClassificationFormatMap for a given ITextView
        /// </summary>
        [Import]
        private IClassificationFormatMapService _classificationFormatMappingService { get; set; }

        [Import(AllowDefault = true)]
        private IWaitIndicator WaitIndicator { get; set; }

        /// <summary>
        /// Returns an IClassifier for a given text buffer
        /// </summary>
        [Import]
        private IClassifierAggregatorService _classifierAggregatorService { get; set; }

        [Import]
        private SVsServiceProvider _serviceProvider { get; set; }

        public string GenerateHtml(NormalizedSnapshotSpanCollection spans, IWpfTextView textView)
        {
            if (spans == null || spans.Count == 0)
            {
                return "";
            }

            // this will trigger loading of the package
            // so we can ensure ToolsOptionsPage gets created and
            // ToolsOptionsPage.Instance gets set
            var dte = (_DTE)_serviceProvider.GetService(typeof(_DTE));
            var props = dte.Properties[CopyAsHtmlPackage.CategoryName, CopyAsHtmlPackage.PageName];

            IClassificationFormatMap formatMap = _classificationFormatMappingService.GetClassificationFormatMap(textView);
            IClassificationType defaultClassificationType = _classificationTypeRegistry.GetClassificationType("text");
            HtmlMarkupProvider htmlMarkupProvider = new HtmlMarkupProvider(
                formatMap,
                defaultClassificationType,
                textView.Background);
            IClassifier classifier = _classifierAggregatorService.GetClassifier(textView.TextBuffer);

            var formattedStringBuilder = new FormattedStringBuilder(
                htmlMarkupProvider,
                classifier,
                defaultClassificationType,
                this.WaitIndicator);

            string result = formattedStringBuilder.AppendSnapshotSpans(spans);

            var classifierDispose = classifier as System.IDisposable;
            if (classifierDispose != null)
            {
                classifierDispose.Dispose();
            }

            return result;
        }
    }
}