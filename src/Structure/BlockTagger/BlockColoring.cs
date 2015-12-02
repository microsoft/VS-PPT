using System;
using System.Diagnostics;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.PowerToolsEx.BlockTagger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class BlockColoring : IDisposable
    {
        private IEditorFormatMap _formatMap;
        private double _width;

        public Coloring ClassColoring { get; private set; }
        public Coloring ConditionalColoring { get; private set; }
        public Coloring LoopColoring { get; private set; }
        public Coloring MethodColoring { get; private set; }
        public Coloring MethodSeparatorAndHighlightColoring { get; private set; }
        public Coloring UnknownColoring { get; private set; }

        public BlockColoring(IEditorFormatMap formatMap, double width)
        {
            _formatMap = formatMap;
            _width = width;

            formatMap.FormatMappingChanged += this.OnFormatMappingChanged;
            this.OnFormatMappingChanged(null, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            if (_formatMap != null)
            {
                _formatMap.FormatMappingChanged -= this.OnFormatMappingChanged;
                _formatMap = null;
            }
        }

        private void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            this.ClassColoring = new Coloring(_formatMap, BlockStructureClassColorFormat.Name, BlockStructureToolTipClassColorFormat.Name, _width);
            this.ConditionalColoring = new Coloring(_formatMap, BlockStructureConditionalColorFormat.Name, BlockStructureToolTipConditionalColorFormat.Name, _width);
            this.LoopColoring = new Coloring(_formatMap, BlockStructureLoopColorFormat.Name, BlockStructureToolTipLoopColorFormat.Name, _width);
            this.MethodColoring = new Coloring(_formatMap, BlockStructureMethodColorFormat.Name, BlockStructureToolTipMethodColorFormat.Name, _width);
            this.MethodSeparatorAndHighlightColoring = new Coloring(_formatMap, MethodSeparatorColorFormat.Name, ScrollBarMethodHighlightColorFormat.Name, _width, 0.25);
            this.UnknownColoring = new Coloring(_formatMap, BlockStructureUnknownColorFormat.Name, BlockStructureToolTipUnknownColorFormat.Name, _width);

            EventHandler changed = this.Changed;
            if (changed != null)
            {
                changed(this, new EventArgs());
            }
        }

        public Pen GetToolTipPen(IBlockTag tag)
        {
            return this.GetColoring(tag.Type).ToolTipPen;
        }
        public Pen GetToolTipPen(BlockType type)
        {
            return this.GetColoring(type).ToolTipPen;
        }

        public Pen GetLinePen(IBlockTag tag)
        {
            return this.GetColoring(tag.Type).LinePen;
        }
        public Pen GetLinePen(BlockType type)
        {
            return this.GetColoring(type).LinePen;
        }

        public Brush GetLineBrush(IBlockTag tag)
        {
            return this.GetColoring(tag.Type).LineBrush;
        }
        public Brush GetLineBrush(BlockType type)
        {
            return this.GetColoring(type).LineBrush;
        }

        public Brush GetToolTipBrush(IBlockTag tag)
        {
            return this.GetColoring(tag.Type).ToolTipBrush;
        }
        public Brush GetToolTipBrush(BlockType type)
        {
            return this.GetColoring(type).ToolTipBrush;
        }

        public event EventHandler Changed;

        private Coloring GetColoring(BlockType type)
        {
            switch (type)
            {
                case BlockType.Loop:
                    return this.LoopColoring;
                case BlockType.Conditional:
                    return this.ConditionalColoring;
                case BlockType.Class:
                    return this.ClassColoring;
                case BlockType.Method:
                    return this.MethodColoring;
                default:
                    return this.UnknownColoring;
            }
        }

        public struct Coloring
        {
            public readonly Brush LineBrush;
            public readonly Brush ToolTipBrush;
            public readonly Pen LinePen;
            public readonly Pen ToolTipPen;

            public Coloring(IEditorFormatMap map, string lineName, string toolTipName, double width, double opacity = 1.0)
            {
                var brush = Coloring.GetBrush(map, toolTipName, EditorFormatDefinition.ForegroundBrushId);
                if (brush != null)
                {
                    brush = brush.Clone();
                    brush.Opacity = opacity;
                    brush.Freeze();
                }

                this.ToolTipBrush = brush;
                this.LineBrush = Coloring.GetBrush(map, lineName, EditorFormatDefinition.ForegroundBrushId);

                if (this.LineBrush != null)
                {
                    this.LinePen = new Pen(this.LineBrush, width);
                    this.LinePen.Freeze();
                }
                else
                {
                    this.LinePen = null;
                }

                if (this.ToolTipBrush != null)
                {
                    this.ToolTipPen = new Pen(this.ToolTipBrush, width);
                    this.ToolTipPen.Freeze();
                }
                else
                {
                    this.ToolTipPen = null;
                }
            }

            private static Brush GetBrush(IEditorFormatMap map, string name, string resource = EditorFormatDefinition.BackgroundBrushId)
            {
                var formatProperties = map.GetProperties(name);
                if (formatProperties != null && formatProperties.Contains(resource))
                {
                    var brushValue = formatProperties[resource] as Brush;
                    if (brushValue != null)
                    {
                        Debug.Assert(brushValue.IsFrozen);

                        return brushValue;
                    }
                }

                return null;
            }
        }
    }
}
