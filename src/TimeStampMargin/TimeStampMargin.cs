using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Microsoft.VisualStudio.PowerTools.TimeStampMargin
{
    /// <summary>
    /// Defines the option to enable the time stamp margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(TimeStampMarginEnabled.StaticName)]
    public sealed class TimeStampMarginEnabled : ViewOptionDefinition<bool>
    {
        public const string StaticName = "TimeStampMarginEnabled";
        public static readonly EditorOptionKey<bool> StaticKey = new EditorOptionKey<bool>(TimeStampMarginEnabled.StaticName);

        public override bool Default { get { return true; } }

        public override EditorOptionKey<bool> Key { get { return TimeStampMarginEnabled.StaticKey; } }
    }

    /// <summary>
    /// Defines the option to show hours in the time stamp.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(TimeStampMarginShowHours.StaticName)]
    public sealed class TimeStampMarginShowHours : ViewOptionDefinition<bool>
    {
        public const string StaticName = "TimeStampMarginShowHours";
        public static readonly EditorOptionKey<bool> StaticKey = new EditorOptionKey<bool>(TimeStampMarginShowHours.StaticName);

        public override bool Default { get { return false; } }

        public override EditorOptionKey<bool> Key { get { return TimeStampMarginShowHours.StaticKey; } }
    }

    /// <summary>
    /// Defines the option to show milliseconds in the time stamp.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(TimeStampMarginShowMilliseconds.StaticName)]
    public sealed class TimeStampMarginShowMilliseconds : ViewOptionDefinition<bool>
    {
        public const string StaticName = "TimeStampMarginShowMilliseconds";
        public static readonly EditorOptionKey<bool> StaticKey = new EditorOptionKey<bool>(TimeStampMarginShowMilliseconds.StaticName);

        public override bool Default { get { return true; } }

        public override EditorOptionKey<bool> Key { get { return TimeStampMarginShowMilliseconds.StaticKey; } }
    }

    /// <summary>
    /// A class detailing the margin's visual definition including both size and content.
    /// </summary>
    internal sealed class TimeStampMargin : Canvas, IWpfTextViewMargin
    {
        public const string MarginName = "TimeStampMargin";
        public const string LineNumberClassificationTypeKey = "line number";
        public static readonly XmlLanguage EnUsLanguage = XmlLanguage.GetLanguage("en-US");

        private readonly IWpfTextView _textView;
        private bool _isDisposed;
        private bool _showMilliseconds;
        private bool _showHours;
        private readonly MarginFactory _factory;
        private readonly IClassificationType _lineNumberClassification;
        private readonly IClassificationFormatMap _formatMap;
        private TextRunProperties _formatting;
        private double _oldViewportTop = double.MinValue;
        private Canvas _translatedCanvas = new Canvas();
        private List<DateTime> _lineTimeStamps = new List<DateTime>();

        /// <summary>
        /// Creates a <see cref="TimeStampMargin"/> for a given <see cref="IWpfTextView"/>.
        /// </summary>
        public TimeStampMargin(IWpfTextView textView, MarginFactory factory)
        {
            _textView = textView;
            _factory = factory;

            this.IsHitTestVisible = false;
            this.ClipToBounds = true;
            this.Children.Add(_translatedCanvas);

            _lineNumberClassification = _factory.ClassificationTypeRegistryService.GetClassificationType(LineNumberClassificationTypeKey);
            _formatMap = _factory.ClassificationFormatMappingService.GetClassificationFormatMap(_textView);

            // Enable the fixed text hinting mode to ensure that WPF never considers
            // the text as being animated. If it does, it will use a lower resolution to render
            // the text during animation (scrolling for instance) which makes the text look fuzzy
            // and then sharp once the animation stops
            TextOptions.SetTextHintingMode(this, TextHintingMode.Fixed);

            _textView.TextBuffer.Changed += OnTextBufferChanged;

            ITextSnapshot snapshot = _textView.TextBuffer.CurrentSnapshot;
            for (int i = snapshot.LineCount - 2; (i >= 0); --i)     //Do not add the time stamp for the last line of the buffer (the one without a line break)
            {
                _lineTimeStamps.Add(DateTime.Now);
            }

            base.IsVisibleChanged += delegate (object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    // Raised whenever the text in a text view changes.
                    _textView.LayoutChanged += OnLayoutChanged;

                    // Raised whenever the options for a text view changes.
                    _textView.Options.OptionChanged += OnOptionChanged;

                    // Raised whenever the classification format changes.
                    _formatMap.ClassificationFormatMappingChanged += OnClassificationFormatChanged;

                    //Fonts might have changed while we were hidden.
                    this.SetFontFromClassification();
                }
                else
                {
                    _textView.LayoutChanged -= OnLayoutChanged;
                    _textView.Options.OptionChanged -= OnOptionChanged;
                    _formatMap.ClassificationFormatMappingChanged -= OnClassificationFormatChanged;
                }
            };
        }

        /// <summary>
        /// Handle changes in Tools > Options for TimeStampMargin
        /// </summary>
        private void OnOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            if ((e.OptionId == TimeStampMarginShowMilliseconds.StaticName) ||
                (e.OptionId == TimeStampMarginShowHours.StaticName))
            {
                this.SetFontFromClassification();
            }
        }

        /// <summary>
        /// Handle changes to the text buffer (new lines, for example).
        /// </summary>
        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            foreach (var change in e.Changes)
            {
                if (change.LineCountDelta > 0)
                {
                    List<DateTime> newTimeStamps = new List<DateTime>();
                    for (int i = 0; (i < change.LineCountDelta); ++i)
                    {
                        newTimeStamps.Add(DateTime.Now);
                    }

                    _lineTimeStamps.InsertRange(e.Before.GetLineFromPosition(change.OldPosition).LineNumber, newTimeStamps);
                }
                else if (change.LineCountDelta < 0)
                {
                    _lineTimeStamps.RemoveRange(e.Before.GetLineFromPosition(change.OldPosition).LineNumber, -change.LineCountDelta);
                }
            }
        }

        /// <summary>
        /// Handle changes to <see cref="ITextView"/> layout (like when the underlying <see cref="ITextBuffer"/> changes).
        /// </summary>
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (_oldViewportTop != _textView.ViewportTop)
            {
                _oldViewportTop = _textView.ViewportTop;
                _translatedCanvas.RenderTransform = new TranslateTransform(offsetX: 0.0, offsetY: -_textView.ViewportTop);
            }

            // Update the lines on the canvas with timestamps if they don't have them
            this.UpdateLineNumbers();
        }


        /// <summary>
        /// Handle changes from classification changes (such as a font color change).
        /// </summary>
        private void OnClassificationFormatChanged(object sender, EventArgs e)
        {
            this.SetFontFromClassification();
        }

        /// <summary>
        /// Update the time stamps for the text view lines that are visible. 
        /// Forces a draw for those lines.
        /// </summary>
        private void UpdateLineNumbers()
        {
            HashSet<object> visibleLines = new HashSet<object>();
            foreach (var line in _textView.TextViewLines)
            {
                if (line.IsFirstTextViewLineForSnapshotLine)
                {
                    visibleLines.Add(line.IdentityTag);
                }
            }

            //Go backwards through the list so we remove no-longer-visible children back-to-front.
            Dictionary<object, TimeStampVisual> visibleTags = new Dictionary<object, TimeStampVisual>();
            List<int> childrenToRemove = new List<int>();

            for (int i = _translatedCanvas.Children.Count - 1; (i >= 0); --i)
            {
                TimeStampVisual visual = _translatedCanvas.Children[i] as TimeStampVisual;
                if (visual != null)
                {
                    if (visibleLines.Contains(visual.LineTag))
                    {
                        visibleTags.Add(visual.LineTag, visual);
                    }
                    else
                    {
                        childrenToRemove.Add(i);
                    }
                }
                else
                {
                    Debug.Fail("Wrong type of child");
                }
            }

            List<TimeStampVisual> newVisuals = new List<TimeStampVisual>();
            foreach (var line in _textView.TextViewLines)
            {
                if (line.IsFirstTextViewLineForSnapshotLine)
                {
                    int lineNumber = line.Start.GetContainingLine().LineNumber;
                    if (lineNumber < _lineTimeStamps.Count)             // No time stamp for the last line in the file.
                    {
                        var timeStamp = _lineTimeStamps[lineNumber];
                        TimeStampVisual visual;

                        if (!visibleTags.TryGetValue(line.IdentityTag, out visual))
                        {
                            int i = childrenToRemove.Count - 1;

                            if (i < 0)
                            {
                                visual = new TimeStampVisual();
                                newVisuals.Add(visual);
                            }
                            else
                            {
                                visual = _translatedCanvas.Children[childrenToRemove[i]] as TimeStampVisual;
                                Debug.Assert(visual != null);

                                // Don't remove child from canvas.
                                childrenToRemove.RemoveAt(i);
                            }
                        }

                        // Draw visual on text view.
                        visual.UpdateVisual(timeStamp, line, _textView, _formatting, base.MinWidth, _oldViewportTop, _showHours, _showMilliseconds);
                    }
                }
            }

            // Remove all the remaining unused children.
            foreach (int i in childrenToRemove)
            {
                _translatedCanvas.Children.RemoveAt(i);
            }

            foreach (var visual in newVisuals)
            {
                _translatedCanvas.Children.Add(visual);
            }
        }

        /// <summary>
        /// Updates properties from a change to the format map. Also forces a redraw of all the time stamped lines.
        /// </summary>
        private void SetFontFromClassification()
        {
            var font = _formatMap.GetTextProperties(_lineNumberClassification);
            _showHours = _textView.Options.GetOptionValue(TimeStampMarginShowHours.StaticKey);
            _showMilliseconds = _textView.Options.GetOptionValue(TimeStampMarginShowMilliseconds.StaticKey);

            // In the line number margin, we always enforce a 100% opacity.
            // This is to prevent mixing of the line number's background color
            // with its parent's background when the background brush of the
            // line number classified item is not opaque.
            Brush backgroundBrush = font.BackgroundBrush;
            if (backgroundBrush.Opacity != 1.0)
            {
                backgroundBrush = backgroundBrush.Clone();
                backgroundBrush.Opacity = 1.0;
                backgroundBrush.Freeze();

                font = font.SetBackgroundBrush(backgroundBrush);
            }

            base.Background = backgroundBrush;
            _formatting = font;
            this.SetClearTypeHint(font);
            this.DetermineMarginWidth();

            // Reformat all the lines
            _translatedCanvas.Children.Clear();
            this.UpdateLineNumbers();
        }

        /// <summary>
        /// Use clear type if we are in an en-us environment and the <see cref="TextFormattingRunProperties"/>' 
        /// font is under the Consolas family.
        /// </summary>
        /// <param name="textProperties">The properties that contain the font information.</param>
        private void SetClearTypeHint(TextFormattingRunProperties textProperties)
        {
            string familyName;
            if (!textProperties.TypefaceEmpty &&
                textProperties.Typeface.FontFamily.FamilyNames.TryGetValue(EnUsLanguage, out familyName) &&
                string.Compare(familyName, "Consolas", StringComparison.OrdinalIgnoreCase) == 0)
            {
                TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            }
            else
            {
                TextOptions.SetTextRenderingMode(this, TextRenderingMode.Auto);
            }
        }

        /// <summary>
        /// Determine the width of the margin, using the number of visible digits (e.g. 5) to construct
        /// a model string (e.g. "88888").
        /// </summary>
        private void DetermineMarginWidth()
        {
            string template = _showMilliseconds ? "XX:XX.XXX" : "XX:XX";
            if (_showHours)
            {
                template = "XX:" + template;
            }

            TextFormattingMode textFormattingMode = _textView.FormattedLineSource.UseDisplayMode ? TextFormattingMode.Display : TextFormattingMode.Ideal;
            FormattedText formattedText = new FormattedText(template, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                                            _formatting.Typeface, _formatting.FontRenderingEmSize, Brushes.Black,
                                                            TimeStampVisual.InvariantNumberSubstitution, textFormattingMode);

            base.MinWidth = formattedText.Width;
        }

        /// <summary>
        /// Throw an <see cref="ObjectDisposedException"/> if we have already been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(MarginName);
            }
        }

        #region IWpfTextViewMargin Members

        /// <summary>
        /// The <see cref="System.Windows.FrameworkElement"/> that implements the visual representation
        /// of the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            // Since this margin implements Canvas, this is the object which renders
            // the margin.
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        #endregion

        #region ITextViewMargin Members

        /// <summary>
        /// The rendered size of the margin in pixels.
        /// </summary>
        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return base.MinWidth;
            }
        }

        /// <summary>
        /// Determine if the margin is enabled or not.
        /// </summary>
        /// <remarks>
        /// The margin is enabled whenever the option "TimeStampMarginEnabled" is true.
        /// </remarks>
        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return _textView.Options.GetOptionValue(TimeStampMarginEnabled.StaticKey);
            }
        }

        /// <summary>
        /// Returns an instance of the margin if this is the margin that has been requested.
        /// </summary>
        /// <param name="marginName">The name of the margin requested</param>
        /// <returns>An instance of TimeStampMargin or null</returns>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return (marginName == MarginName) ? this as IWpfTextViewMargin : null;
        }

        /// <summary>
        /// Unsubscribe from events, ask that we do not be finalized from the garbage collector.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _textView.TextBuffer.Changed -= OnTextBufferChanged;
                _isDisposed = true;
            }
        }
        #endregion
    }
}
