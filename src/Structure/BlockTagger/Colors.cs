using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PowerToolsEx.BlockTagger
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureClassColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureClassColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureClass";

        public BlockStructureClassColorFormat()
        {
            this.DisplayName = Strings.BlockStructureClassColor;
            this.ForegroundColor = Color.FromArgb(0xFF, 0x7C, 0xCA, 0xDD);
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureConditionalColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureConditionalColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureConditional";

        public BlockStructureConditionalColorFormat()
        {
            this.DisplayName = Strings.BlockStructureConditionalColor;
            this.ForegroundColor = Color.FromArgb(0xFF, 0x7C, 0xCC, 0x87);
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureLoopColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureLoopColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureLoop";

        public BlockStructureLoopColorFormat()
        {
            this.DisplayName = Strings.BlockStructureLoopColor;
            this.ForegroundColor = Color.FromArgb(0xFF, 0xA3, 0x7E, 0xBC);
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureMethodColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureMethodColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureMethod";

        public BlockStructureMethodColorFormat()
        {
            this.DisplayName = Strings.BlockStructureMethodColor;
            this.ForegroundColor = Color.FromArgb(0xFF, 0x87, 0x9F, 0xFF);
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureUnknownColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureUnknownColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureUnknown";

        public BlockStructureUnknownColorFormat()
        {
            this.DisplayName = Strings.BlockStructureUnknownColor;
            this.ForegroundColor = Color.FromArgb(0xFF, 0xB6, 0xB6, 0xB6);
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureToolTipClassColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureToolTipClassColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureToolTipClass";

        public BlockStructureToolTipClassColorFormat()
        {
            this.DisplayName = Strings.BlockStructureToolTipClassColor;
            this.ForegroundColor = Colors.Black;
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureToolTipConditionalColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureToolTipConditionalColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureToolTipConditional";

        public BlockStructureToolTipConditionalColorFormat()
        {
            this.DisplayName = Strings.BlockStructureToolTipConditionalColor;
            this.ForegroundColor = Colors.Green;
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureToolTipLoopColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureToolTipLoopColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureToolTipLoop";

        public BlockStructureToolTipLoopColorFormat()
        {
            this.DisplayName = Strings.BlockStructureToolTipLoopColor;
            this.ForegroundColor = Colors.Purple;
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureToolTipMethodColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureToolTipMethodColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureToolTipMethod";

        public BlockStructureToolTipMethodColorFormat()
        {
            this.DisplayName = Strings.BlockStructureToolTipMethodColor;
            this.ForegroundColor = Colors.Blue;
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(BlockStructureToolTipUnknownColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class BlockStructureToolTipUnknownColorFormat : EditorFormatDefinition
    {
        public const string Name = "BlockStructureToolTipUnknown";

        public BlockStructureToolTipUnknownColorFormat()
        {
            this.DisplayName = Strings.BlockStructureToolTipUnknownColor;
            this.ForegroundColor = Colors.Gray;
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(ScrollBarMethodHighlightColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class ScrollBarMethodHighlightColorFormat : EditorFormatDefinition
    {
        public const string Name = "ScrollBarMethodHighlight";

        public ScrollBarMethodHighlightColorFormat()
        {
            this.DisplayName = Strings.ScrollBarMethodHighlightColor;
            this.ForegroundColor = Color.FromArgb(0xFF, 0x87, 0x9F, 0xFF);
            this.BackgroundCustomizable = false;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(MethodSeparatorColorFormat.Name)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class MethodSeparatorColorFormat : EditorFormatDefinition
    {
        public const string Name = "MethodSeparator";

        public MethodSeparatorColorFormat()
        {
            this.DisplayName = Strings.MethodSeparatorColor;
            this.ForegroundColor = Color.FromArgb(0xFF, 0xB6, 0xB6, 0xB6);
            this.BackgroundCustomizable = false;
        }
    }
}
