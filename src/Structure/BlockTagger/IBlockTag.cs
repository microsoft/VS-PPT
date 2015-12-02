using System.Windows;
using System.Windows.Media.TextFormatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.PowerToolsEx.BlockTagger
{
    public interface IBlockTag : ITag
    {
        SnapshotSpan Span { get; }

        BlockType Type { get; }

        int Level { get; }

        SnapshotPoint StatementStart { get; }

        IBlockTag Parent { get; }

        FrameworkElement Context(BlockColoring coloring, TextRunProperties properties);
    }

    public enum BlockType
    {
        Root, Loop, Conditional, Method, Class, Namespace, Other, Unknown
    }
}
