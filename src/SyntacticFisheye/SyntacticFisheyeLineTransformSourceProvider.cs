using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.SyntacticFisheye
{
    /// <summary>
    /// This class implements a connector that produces the SyntacticFisheye LineTransformSourceProvider.
    /// </summary>
    [Export(typeof(ILineTransformSourceProvider))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
    [TextViewRole(PredefinedTextViewRoles.PreviewTextView)]
    [TextViewRole("PRINTABLE")]
    internal sealed class SyntacticFisheyeLineTransformSourceProvider : ILineTransformSourceProvider
    {
        public ILineTransformSource Create(IWpfTextView textView)
        {
            if (textView.Roles.Contains(DifferenceViewerRoles.LeftViewTextViewRole) || textView.Roles.Contains(DifferenceViewerRoles.RightViewTextViewRole) || textView.Roles.Contains("VSMERGEDEFAULT" /*MergeViewerRoles.VSMergeDefaultRole from TFS*/))
            {
                //Don't use the fisheye for diff views since it will cause them to become misaligned.
                return null;
            }

            return SyntacticFisheyeLineTransformSource.Create(textView);
        }
    }
}