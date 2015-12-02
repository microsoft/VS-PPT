using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.PowerToolsEx.BlockTagger;
using Microsoft.VisualStudio.TelemetryForPPT;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.PowerTools.StructureVisualizer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    /// <summary>
    /// Helper class to handle the rendering of the structure margin.
    /// </summary>
    internal class StructureMarginElement : FrameworkElement
    {
        private readonly IWpfTextView _textView;
        private readonly IVerticalScrollBar _scrollBar;

        private ITagAggregator<IBlockTag> _tagger;
        private BlockColoring _coloring;
        private const double marginWidth = 25.0;
        private const double lineWidth = 2.0;
        private const double gapWidth = 1.0;
        private IEditorFormatMap _formatMap;
        private StructureMarginFactory _factory;
        private bool _enabled;
        private bool _previewEnabled;

        private static readonly TimeSpan s_tipReportingDelay = TimeSpan.FromSeconds(3.0);
        private static readonly TimeSpan s_tipClearingDelay = TimeSpan.FromSeconds(1.0);
        private static bool s_logged = false;
        private static string s_lastKey = null;
        private static DateTime s_lastTimeChanged = DateTime.MaxValue;

        private static TimeSpan s_elapsedTime = TimeSpan.Zero;
        private static ITelemetrySession s_telemetrySession = null;

        /// <summary>
        /// Constructor for the StructureMarginElement.
        /// </summary>
        /// <param name="textView">ITextView to which this StructureMargenElement will be attacheded.</param>
        /// <param name="verticalScrollbar">Vertical scrollbar of the ITextViewHost that contains <paramref name="textView"/>.</param>
        /// <param name="tagFactory">MEF tag factory.</param>
        public StructureMarginElement(IWpfTextView textView, IVerticalScrollBar verticalScrollbar, StructureMarginFactory factory)
        {
            _textView = textView;
            _scrollBar = verticalScrollbar;
            _factory = factory;

            _formatMap = factory.EditorFormatMapService.GetEditorFormatMap(textView);

            this.IsHitTestVisible = false;
            this.SnapsToDevicePixels = true;
            this.Width = marginWidth;

            textView.Options.OptionChanged += this.OnOptionChanged;

            this.IsVisibleChanged += this.OnIsVisibleChanged;

            this.OnOptionChanged(null, null);
        }

        public void Dispose()
        {
            this.IsVisibleChanged -= this.OnIsVisibleChanged;
            _textView.Options.OptionChanged -= this.OnOptionChanged;
        }

        public bool UpdateTip(IVerticalScrollBar margin, MouseEventArgs e, ToolTip tip)
        {
            if ((!_textView.IsClosed) && (_tagger != null) && _previewEnabled)
            {
                Point pt = e.GetPosition(this);
                if ((pt.X >= 0.0) && (pt.X <= this.Width))
                {
                    SnapshotPoint position = _scrollBar.GetBufferPositionOfYCoordinate(pt.Y);

                    IBlockTag deepestTag = null;
                    var tags = _tagger.GetTags(new SnapshotSpan(position, 0));
                    foreach (var tagSpan in tags)
                    {
                        if (tagSpan.Tag.Type != BlockType.Unknown)
                        {
                            if ((deepestTag == null) || (tagSpan.Tag.Level > deepestTag.Level))
                                deepestTag = tagSpan.Tag;
                        }
                    }

                    if (deepestTag != null)
                    {
                        if (tip.IsOpen)
                        {
                            var existingContext = tip.Content as FrameworkElement;
                            if ((existingContext != null) && (existingContext.Tag == deepestTag))
                            {
                                // No changes from the last time we opened the tip.
                                return true;
                            }
                        }

                        FrameworkElement context = deepestTag.Context(_coloring,
                                                                        _textView.FormattedLineSource.DefaultTextProperties);

                        context.Tag = deepestTag;

                        //The width of the view is in zoomed coordinates so factor the zoom factor into the tip window width computation.
                        double zoom = _textView.ZoomLevel / 100.0;
                        tip.MinWidth = tip.MaxWidth = Math.Floor(Math.Max(50.0, _textView.ViewportWidth * zoom * 0.5));
                        tip.MinHeight = tip.MaxHeight = context.Height + 12.0;

                        tip.Content = context;
                        tip.IsOpen = true;

                        StructureMarginElement.LogTipOpened("VS/PPT-Structure/MarginTipOpened", context);

                        return true;
                    }
                }
            }

            return false;
        }

        public static void LogTipOpened(string key, FrameworkElement context)
        {
            // Both users of the context tip will get spammed a lot (we recreate the tip whenever the context changes) so we want to log a record
            // when any tip is displayed more or less continuously for 3 seconds. So we accumulate the time the tip is opened until we reach 3 seconds
            // (and then we log things) & reset the elapsed time counter whenever the tip has been closed for more than one second.
            context.IsVisibleChanged += LogTipClosed;

            DateTime now = DateTime.UtcNow;
            if ((now - StructureMarginElement.s_lastTimeChanged) > StructureMarginElement.s_tipClearingDelay)
            {
                StructureMarginElement.s_elapsedTime = TimeSpan.Zero;
                StructureMarginElement.s_logged = false;
            }

            StructureMarginElement.s_lastTimeChanged = now;
            StructureMarginElement.s_lastKey = key;
        }

        private static void LogTipClosed(object sender, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement context = sender as FrameworkElement;
            if ((context != null) && !context.IsVisible)
            {
                context.IsVisibleChanged -= LogTipClosed;

                DateTime now = DateTime.UtcNow;
                StructureMarginElement.s_elapsedTime += (now - StructureMarginElement.s_lastTimeChanged);
                StructureMarginElement.s_lastTimeChanged = now;

                if ((!StructureMarginElement.s_logged) && (StructureMarginElement.s_elapsedTime > StructureMarginElement.s_tipReportingDelay))
                {
                    if (StructureMarginElement.s_telemetrySession == null)
                    {
                        StructureMarginElement.s_telemetrySession = TelemetrySessionForPPT.Create(typeof(StructureMarginElement).Assembly);
                    }

                    StructureMarginElement.s_telemetrySession.PostEvent(StructureMarginElement.s_lastKey);

                    StructureMarginElement.s_elapsedTime = TimeSpan.Zero;
                    StructureMarginElement.s_logged = true;
                }
            }
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                //Hook up to the various events we need to keep the caret margin current.
                _scrollBar.Map.MappingChanged += OnMappingChanged;

                _coloring = new BlockColoring(_formatMap, 1.0);
                _coloring.Changed += this.OnColoringChanged;

                _tagger = _factory.TagAggregatorFactoryService.CreateTagAggregator<IBlockTag>(_textView);
                _tagger.BatchedTagsChanged += OnTagsChanged;

                //Force the margin to be re-rendered since things might have changed while the margin was hidden.
                this.InvalidateVisual();
            }
            else
            {
                _scrollBar.Map.MappingChanged -= OnMappingChanged;

                _coloring.Changed -= this.OnColoringChanged;
                _coloring.Dispose();
                _coloring = null;

                _tagger.BatchedTagsChanged -= OnTagsChanged;
                _tagger.Dispose();
                _tagger = null;
            }
        }

        private void OnColoringChanged(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        private void OnOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            bool mapMode = _textView.Options.GetOptionValue(DefaultTextViewHostOptions.ShowEnhancedScrollBarOptionId);
            _enabled = mapMode && _textView.Options.GetOptionValue(StructureMarginEnabledOption.OptionKey);
            _previewEnabled = mapMode && _textView.Options.GetOptionValue(DefaultTextViewHostOptions.ShowPreviewOptionId) && _textView.Options.GetOptionValue(StructurePreviewEnabledOption.OptionKey);

            this.Visibility = _enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTagsChanged(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        private void OnMappingChanged(object sender, EventArgs e)
        {
            //Force the visual to invalidate since the we'll need to redraw all the structure markings
            this.InvalidateVisual();
        }

        /// <summary>
        /// Override for the FrameworkElement's OnRender. When called, redraw
        /// all of the markers 
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if ((!_textView.IsClosed) && (_tagger != null))
            {
                var tags = _tagger.GetTags(new SnapshotSpan(_textView.TextSnapshot, 0, _textView.TextSnapshot.Length));

                foreach (var tag in tags)
                {
                    this.DrawBlock(drawingContext, tag, true);
                }

                foreach (var tag in tags)
                {
                    this.DrawBlock(drawingContext, tag, false);
                }
            }
        }

        private void DrawBlock(DrawingContext drawingContext, IMappingTagSpan<IBlockTag> tag, bool methodHighlights)
        {
            if (methodHighlights
                ? (tag.Tag.Type == BlockType.Method)
                : ((tag.Tag.Type != BlockType.Namespace) && (tag.Tag.Type != BlockType.Root)))
            {
                int level = 0;
                for (var p = tag.Tag.Parent; (p != null); p = p.Parent)
                {
                    if ((p.Type != BlockType.Namespace) && (p.Type != BlockType.Root))
                        ++level;
                }

                NormalizedSnapshotSpanCollection spans = tag.Span.GetSpans(_textView.TextSnapshot);
                foreach (var span in spans)
                {
                    double x = (double)(level) * (lineWidth + gapWidth) + 3.0;

                    if (x < this.ActualWidth)
                    {
                        double yTop = _scrollBar.GetYCoordinateOfBufferPosition(span.Start);
                        double yBottom = _scrollBar.GetYCoordinateOfBufferPosition(span.End);

                        if (yBottom > yTop + 2.0)
                        {
                            if (methodHighlights)
                            {
                                drawingContext.PushClip(new RectangleGeometry(new Rect(x - 1.0, 0.0, (this.ActualWidth - x), this.ActualHeight)));
                                drawingContext.DrawEllipse(_coloring.MethodSeparatorAndHighlightColoring.ToolTipBrush, null, new Point(x - 1.0, (yTop + yBottom) * 0.5),
                                                           (this.ActualWidth + 1.0 - x), (yBottom - yTop) * 0.5);
                                drawingContext.Pop();
                            }
                            else
                            {
                                drawingContext.DrawLine(_coloring.GetLinePen(tag.Tag),
                                                        new Point(x, yTop),
                                                        new Point(x, yBottom));
                            }
                        }
                    }
                }
            }
        }
    }
}
