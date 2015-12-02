using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.PowerToolsEx.BlockTagger;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.PowerTools.StructureVisualizer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    internal class StructureAdornmentManager : MouseProcessorBase
    {
        private IWpfTextView _view;
        private IAdornmentLayer _layer;
        private StructureAdornmentFactory _factory;
        private ITagAggregator<IBlockTag> _blockTagger;
        private HashSet<VisibleBlock> _visibleBlocks = new HashSet<VisibleBlock>();
        private List<GeometryAdornment> _methodSeparators = new List<GeometryAdornment>();
        private bool _enabled;
        private bool _showAdornments;
        private bool _showMethodSeparator;
        private bool _redrawAllQueued;
        private ToolTip _tipWindow;
        private BlockColoring _coloring;
        private IEditorFormatMap _formatMap;

        public static StructureAdornmentManager Create(IWpfTextView view, StructureAdornmentFactory factory)
        {
            return view.Properties.GetOrCreateSingletonProperty<StructureAdornmentManager>(delegate { return new StructureAdornmentManager(view, factory); });
        }

        private StructureAdornmentManager(IWpfTextView view, StructureAdornmentFactory factory)
        {
            _view = view;
            _factory = factory;
            _layer = view.GetAdornmentLayer("StructureAdornmentLayer");

            _formatMap = factory.EditorFormatMapService.GetEditorFormatMap(view);

            _view.VisualElement.IsVisibleChanged += this.OnVisibleChanged;
            _view.Closed += this.OnClosed;

            _view.Options.OptionChanged += this.OnOptionChanged;
            this.OnOptionChanged(null, null);
        }

        private void OnOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            bool oldShowAdorments = _showAdornments;
            bool oldShowMethodSeparator = _showMethodSeparator;

            _showAdornments = _view.Options.GetOptionValue(StructureAdornmentEnabledOption.OptionKey);
            _showMethodSeparator = _view.Options.GetOptionValue(MethodSeparatorEnabledOption.OptionKey);

            if ((!this.UpdateShowAdornments(true)) && ((oldShowAdorments != _showAdornments) || (oldShowMethodSeparator != _showMethodSeparator)))
            {
                this.RedrawAllAdornments();
            }
        }

        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.UpdateShowAdornments(true);
        }

        private bool UpdateShowAdornments(bool enabled)
        {
            enabled = enabled && (_view.VisualElement.IsVisible || _view.Roles.Contains("PRINTABLE")) && (_showAdornments || _showMethodSeparator);

            if (_enabled != enabled)
            {
                _enabled = enabled;

                if (enabled)
                {
                    _coloring = new BlockColoring(_formatMap, 1.0);
                    _coloring.Changed += this.OnColoringChanged;

                    _view.LayoutChanged += OnLayoutChanged;
                    _blockTagger = _factory.TagAggregatorFactoryService.CreateTagAggregator<IBlockTag>(_view);
                    _blockTagger.BatchedTagsChanged += OnTagsChanged;

                    this.RedrawAllAdornments();

                    return true;
                }
                else
                {
                    _view.LayoutChanged -= OnLayoutChanged;

                    _coloring.Changed -= this.OnColoringChanged;
                    _coloring.Dispose();
                    _coloring = null;

                    _blockTagger.BatchedTagsChanged -= OnTagsChanged;
                    _blockTagger.Dispose();
                    _blockTagger = null;

                    _visibleBlocks.Clear();
                    _methodSeparators.Clear();
                    _layer.RemoveAllAdornments();

                    this.CloseTip();
                }
            }

            return false;
        }

        private void OnColoringChanged(object sender, EventArgs e)
        {
            this.RedrawAllAdornments();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            this.UpdateShowAdornments(false);

            _view.Options.OptionChanged -= this.OnOptionChanged;
            _view.VisualElement.IsVisibleChanged -= this.OnVisibleChanged;
            _view.Closed -= this.OnClosed;
        }

        private void OnTagsChanged(object sender, EventArgs e)
        {
            this.RedrawAllAdornments();
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (_enabled)
            {
                double left = Math.Floor(e.NewViewState.ViewportLeft);
                double right = Math.Floor(e.NewViewState.ViewportRight);

                if (_showMethodSeparator)
                {
                    if ((Math.Floor(e.OldViewState.ViewportLeft) != left) ||
                        (Math.Floor(e.OldViewState.ViewportRight) != right))
                    {
                        foreach (var a in _methodSeparators)
                        {
                            Canvas.SetLeft(a, left);

                            a.SetGeometry(new RectangleGeometry(new Rect(0.0, 0.0, right - left, 1.0)));
                        }
                    }
                }

                if (e.NewOrReformattedSpans.Count > 0)
                {
                    this.RedrawAdornments(e.NewOrReformattedSpans, left, right);
                }
            }
        }

        private void RedrawAllAdornments()
        {
            if (!_redrawAllQueued)
            {
                _redrawAllQueued = true;

                _view.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                                                                                            {
                                                                                                if (_redrawAllQueued)
                                                                                                {
                                                                                                    _redrawAllQueued = false;
                                                                                                    this.RedrawAllAdornmentsImmeditately();
                                                                                                }
                                                                                            }));
            }
        }

        private void RedrawAllAdornmentsImmeditately()
        {
            if (_enabled && (_view.TextViewLines != null) && !_view.IsClosed)
            {
                //Remove the existing adornments.
                _visibleBlocks.Clear();
                _methodSeparators.Clear();
                _layer.RemoveAllAdornments();

                this.RedrawAdornments(new NormalizedSnapshotSpanCollection(_view.TextViewLines.FormattedSpan),
                                      Math.Floor(_view.ViewportLeft), Math.Floor(_view.ViewportRight));
            }
        }

        private void RedrawAdornments(NormalizedSnapshotSpanCollection newOrReformattedSpans, double left, double right)
        {
            if (left != right)
            {
                //Recreate the adornments for the visible text.
                //Get all the tags that could possibly be drawn in the view (because we set the extent of any adornment to be from the statement start
                //of a tag to its ultimate end to guarantee that they are redrawn appropriately when something in the statement changes).
                var tags = _blockTagger.GetTags(_view.TextViewLines.FormattedSpan);
                foreach (var tag in tags)
                {
                    this.CreateBlockAdornments(tag, newOrReformattedSpans, left, right);
                }
            }
        }

        private void CreateBlockAdornments(IMappingTagSpan<IBlockTag> tag, NormalizedSnapshotSpanCollection newOrReformattedSpans, double left, double right)
        {
            NormalizedSnapshotSpanCollection spans = tag.Span.GetSpans(_view.TextSnapshot);
            if (spans.Count > 0)
            {
                //Get the start of the tag's span (which could be out of the view or not even mappable to
                //the view's text snapshot).
                var statementStart = _view.BufferGraph.MapUpToSnapshot(tag.Tag.StatementStart, PointTrackingMode.Positive, PositionAffinity.Predecessor, _view.TextSnapshot);
                if (statementStart.HasValue)
                {
                    var end = _view.BufferGraph.MapUpToSnapshot(tag.Tag.Span.End, PointTrackingMode.Positive, PositionAffinity.Predecessor, _view.TextSnapshot);
                    if (end.HasValue)
                    {
                        //Get the full extent of the block tag so that its adornments will be destroyed if anything in the block changes.
                        SnapshotSpan extent = new SnapshotSpan(statementStart.Value, end.Value);

                        bool intersecting = newOrReformattedSpans.IntersectsWith(new NormalizedSnapshotSpanCollection(extent));
                        if (intersecting)
                        {
                            var start = _view.BufferGraph.MapUpToSnapshot(tag.Tag.Span.Start, PointTrackingMode.Positive, PositionAffinity.Predecessor, _view.TextSnapshot);
                            if (start.HasValue)
                            {
                                ITextSnapshotLine startLine = start.Value.GetContainingLine();

                                if (_showAdornments)
                                {
                                    double x = -1.0;
                                    foreach (var span in spans)
                                    {
                                        if (span.OverlapsWith(_view.TextViewLines.FormattedSpan))
                                        {
                                            ITextViewLine spanTop = _view.TextViewLines.GetTextViewLineContainingBufferPosition(span.Start);
                                            double yTop = (spanTop == null) ? _view.TextViewLines.FirstVisibleLine.Top : spanTop.Bottom;

                                            ITextViewLine spanBottom = _view.TextViewLines.GetTextViewLineContainingBufferPosition(span.End);
                                            double yBottom = (spanBottom == null) ? _view.TextViewLines.LastVisibleLine.Bottom : spanBottom.Top;

                                            if (yBottom > yTop)
                                            {
                                                if (x < 0.0)
                                                {
                                                    //We have a starting point ... but it may be the wrong one. We have three cases to consider:
                                                    //1)        if (foo) {
                                                    //          |                               //Line goes here
                                                    //
                                                    //2)        if (foo)
                                                    //              {
                                                    //              |                           //Line goes here
                                                    //
                                                    //3)        if (bar &&
                                                    //              foo) {
                                                    //          |                               //Line goes here
                                                    //
                                                    //
                                                    //In each of these cases, we need to find the position of the first non-whitespace character on the line
                                                    SnapshotPoint blockStart = FindFirstNonwhitespace(startLine);

                                                    //If the span start's on the first non-whitespace character of the line, then we have case 2
                                                    //(& we're done).
                                                    if (blockStart != start.Value)
                                                    {
                                                        //Case 1 or 3 ... see if the start & statement start are on the same line.
                                                        //Is the span start on the same line as the statement start (if so, we have case 1 & are done).
                                                        if (!startLine.Extent.Contains(statementStart.Value))
                                                        {
                                                            //Case 3.
                                                            blockStart = statementStart.Value;
                                                        }
                                                    }

                                                    //Get the x-coordinate of the adornment == middle of the character that starts the block.
                                                    ITextViewLine tagTop = _view.GetTextViewLineContainingBufferPosition(blockStart);
                                                    TextBounds bounds = tagTop.GetCharacterBounds(blockStart);
                                                    x = Math.Floor((bounds.Left + bounds.Right) * 0.5);   //Make sure this is only a pixel wide.
                                                }

                                                this.CreateBlockAdornment(tag.Tag, extent, x, yTop, yBottom);
                                            }
                                        }
                                    }
                                }

                                if (_showMethodSeparator && (tag.Tag.Type == BlockType.Method) && (startLine.End < end.Value))
                                {
                                    var point = _view.BufferGraph.MapUpToBuffer(end.Value, PointTrackingMode.Negative, PositionAffinity.Predecessor, _view.VisualSnapshot.TextBuffer);
                                    if (point.HasValue)
                                    {
                                        ITextViewLine spanBottom = _view.TextViewLines.GetTextViewLineContainingBufferPosition(end.Value);
                                        if (spanBottom != null)
                                        {
                                            GeometryAdornment adornment = new GeometryAdornment(_coloring.MethodSeparatorAndHighlightColoring.LineBrush,
                                                                                                new RectangleGeometry(new Rect(0.0, 0.0, right - left, 1.0)));

                                            Canvas.SetLeft(adornment, left);
                                            Canvas.SetTop(adornment, spanBottom.Bottom - 1.0);

                                            _methodSeparators.Add(adornment);
                                            _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, extent, adornment, adornment, OnMethodSeparatorRemoved);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public class GeometryAdornment : UIElement
        {
            #region Private Attributes
            private readonly DrawingVisual _child;
            internal readonly Brush _brush;
            internal Geometry _geometry;
            #endregion

            public GeometryAdornment(Brush brush, Geometry geometry)
            {
                _brush = brush;

                _child = new DrawingVisual();
                this.SetGeometry(geometry);
                this.AddVisualChild(_child);

                this.IsHitTestVisible = false;
            }

            public void SetGeometry(Geometry geometry)
            {
                _geometry = geometry;
                DrawingContext context = _child.RenderOpen();
                context.DrawGeometry(_brush, null, geometry);
                context.Close();
            }

            #region Member Overrides
            protected override Visual GetVisualChild(int index)
            {
                return _child;
            }

            protected override int VisualChildrenCount
            {
                get
                {
                    return 1;
                }
            }
            #endregion //Member Overrides
        }

        private static SnapshotPoint FindFirstNonwhitespace(ITextSnapshotLine line)
        {
            int i = line.Start;
            while (i < line.End)
            {
                char c = line.Snapshot[i];
                if ((c != ' ') && (c != '\t'))
                    break;
                ++i;
            }

            return new SnapshotPoint(line.Snapshot, i);
        }

        private void CreateBlockAdornment(IBlockTag tag, SnapshotSpan span, double x, double yTop, double yBottom)
        {
            LineGeometry line = new LineGeometry(new Point(x, yTop), new Point(x, yBottom));

            GeometryDrawing drawing = new GeometryDrawing(null, _coloring.GetLinePen(tag), line);
            drawing.Freeze();

            DrawingImage drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();

            Image image = new Image();
            image.Source = drawingImage;

            VisibleBlock block = new VisibleBlock(tag, x, yTop, yBottom);

            Canvas.SetLeft(image, x);
            Canvas.SetTop(image, yTop);
            _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, block, image, OnAdornmentRemoved);

            _visibleBlocks.Add(block);
        }

        private void OnMethodSeparatorRemoved(object tag, UIElement element)
        {
            _methodSeparators.Remove((GeometryAdornment)tag);
        }

        private void OnAdornmentRemoved(object tag, UIElement element)
        {
            _visibleBlocks.Remove((VisibleBlock)tag);
        }

        private struct VisibleBlock
        {
            public readonly IBlockTag tag;
            public readonly double x;
            public readonly double yTop;
            public readonly double yBottom;

            public VisibleBlock(IBlockTag tag, double x, double yTop, double yBottom)
            {
                this.tag = tag;
                this.x = x;
                this.yTop = yTop;
                this.yBottom = yBottom;
            }
        }

        public override void PreprocessMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            ITextViewLineCollection textLines = _view.TextViewLines;
            if ((textLines != null) && (_visibleBlocks.Count > 0))
            {
                ITextViewLine firstVisible = textLines.FirstVisibleLine;

                Point pt = e.GetPosition(_view.VisualElement);
                pt.X += _view.ViewportLeft;
                pt.Y += _view.ViewportTop;

                //Horrible hack for peek: prevent the tip from showing if the y coordinate is in the space below the bottom of a line's text
                //(which is where the peek adornment would be displayed).
                var line = textLines.GetTextViewLineContainingYCoordinate(pt.Y);
                if ((line != null) && (pt.Y <= line.TextBottom + 1.0))
                {
                    int screenTop = (firstVisible.VisibilityState == VisibilityState.FullyVisible)
                                    ? firstVisible.Start
                                    : firstVisible.EndIncludingLineBreak;

                    foreach (VisibleBlock block in _visibleBlocks)
                    {
                        if ((Math.Abs(pt.X - block.x) < 4.0) &&
                            (pt.Y >= block.yTop) && (pt.Y <= block.yBottom))
                        {
                            SnapshotPoint? statementStart = _view.BufferGraph.MapUpToSnapshot(block.tag.StatementStart, PointTrackingMode.Positive, PositionAffinity.Successor, _view.TextSnapshot);
                            if (statementStart.HasValue && (statementStart.Value < screenTop))
                            {
                                if (_tipWindow == null)
                                {
                                    _tipWindow = new ToolTip();

                                    _tipWindow.ClipToBounds = true;

                                    _tipWindow.Placement = PlacementMode.Top;
                                    _tipWindow.PlacementTarget = _view.VisualElement;
                                    _tipWindow.HorizontalAlignment = HorizontalAlignment.Left;
                                    _tipWindow.HorizontalContentAlignment = HorizontalAlignment.Left;

                                    _tipWindow.VerticalAlignment = VerticalAlignment.Top;
                                    _tipWindow.VerticalContentAlignment = VerticalAlignment.Top;
                                }

                                _tipWindow.PlacementRectangle = new Rect(block.x, 0.0, 0.0, 0.0);

                                if (_tipWindow.IsOpen)
                                {
                                    var existingContext = _tipWindow.Content as FrameworkElement;
                                    if ((existingContext != null) && (existingContext.Tag == block.tag))
                                    {
                                        // No changes from the last time we opened the tip.
                                        return;
                                    }
                                }

                                FrameworkElement context = block.tag.Context(_coloring,
                                                                             _view.FormattedLineSource.DefaultTextProperties);
                                context.Tag = block.tag;

                                //The width of the view is in zoomed coordinates so factor the zoom factor into the tip window width computation.
                                double zoom = _view.ZoomLevel / 100.0;
                                _tipWindow.MaxWidth = Math.Max(100.0, _view.ViewportWidth * zoom * 0.5);
                                _tipWindow.MinHeight = _tipWindow.MaxHeight = context.Height + 12.0;

                                var rd = _formatMap.GetProperties("TextView Background");
                                if (rd.Contains(EditorFormatDefinition.BackgroundBrushId))
                                {
                                    _tipWindow.Background = rd[EditorFormatDefinition.BackgroundBrushId] as Brush;
                                }

                                _tipWindow.Content = context;
                                _tipWindow.IsOpen = true;

                                StructureMarginElement.LogTipOpened("VS/PPT-Structure/AdornmentTipOpened", context);

                                return;
                            }
                        }
                    }
                }
            }

            this.CloseTip();
        }

        public override void PreprocessMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            this.CloseTip();
        }

        private void CloseTip()
        {
            if (_tipWindow != null)
            {
                _tipWindow.IsOpen = false;
                _tipWindow = null;
            }
        }
    }
}