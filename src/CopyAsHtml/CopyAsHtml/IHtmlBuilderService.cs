using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Formatting
{
    /// <summary>
    /// Generates HTML-formatted text from a collection of snapshot spans.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part and should be imported using the following attribute:
    /// [Import(typeof(IHtmlBuilderService))] 
    /// </remarks>
    public interface IHtmlBuilderService
    {
        /// <summary>
        /// Gets an HTML string containing the formatted text of corresponding to the provided
        /// <paramref name="spans"/>.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> that contains the snapshot spans.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing HTML markup that represents formatted selection
        /// from the text view.
        /// </returns>
        string GenerateHtml(NormalizedSnapshotSpanCollection spans, IWpfTextView textView);
    }
}
