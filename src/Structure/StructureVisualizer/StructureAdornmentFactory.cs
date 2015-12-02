using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.PowerTools.StructureVisualizer
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [Export(typeof(IMouseProcessorProvider))]
    [Name("StructureAdornment")]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    [TextViewRole("PRINTABLE")]
    internal sealed class StructureAdornmentFactory : IWpfTextViewCreationListener, IMouseProcessorProvider
    {
#pragma warning disable 649
        [Export]
        [Name("StructureAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = "Inter Line Adornment")]
        internal AdornmentLayerDefinition viewLayerDefinition;

        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactoryService { get; set; }

        [Import]
        internal IEditorFormatMapService EditorFormatMapService { get; set; }
#pragma warning restore 649

        public void TextViewCreated(IWpfTextView textView)
        {
            StructureAdornmentManager.Create(textView, this);
        }

        public IMouseProcessor GetAssociatedProcessor(IWpfTextView textView)
        {
            return StructureAdornmentManager.Create(textView, this);
        }
    }
}
