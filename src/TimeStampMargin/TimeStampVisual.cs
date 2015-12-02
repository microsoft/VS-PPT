using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Microsoft.VisualStudio.PowerTools.TimeStampMargin
{
    /// <summary>
    /// The drawing visual for each line in the debugger output window.
    /// </summary>
    internal class TimeStampVisual : UIElement
    {
        public static readonly NumberSubstitution InvariantNumberSubstitution = new NumberSubstitution();

        private double _horizontalOffset;
        private double _verticalOffset = double.MinValue;
        private FormattedText _text;

        /// <summary>
        /// The identifier for a particular <see cref="ITextViewLine"/>
        /// </summary>
        public object LineTag { get; private set; }

        /// <summary>
        /// The time stamp to draw in the debugger output window.
        /// </summary>
        public DateTime TimeStamp { get; private set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TimeStampVisual()
        {
            this.SnapsToDevicePixels = true;
        }

        /// <summary>
        /// Draw the text for the time stamp on the line.
        /// </summary>
        /// <param name="drawingContext">The context to draw the text on.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawText(_text, new Point(_horizontalOffset, _verticalOffset));
        }

        /// <summary>
        /// Store <paramref name="timeStamp"/> and updates the text for the visual.
        /// </summary>
        /// <param name="timeStamp">Time of the event.</param>
        /// <param name="line">The line that this time stamp corresponds to.</param>
        /// <param name="view">The <see cref="IWpfTextView"/> to whom the <paramref name="line"/> belongs.</param>
        /// <param name="formatting">Properties for the time stamp text.</param>
        /// <param name="marginWidth">Used to calculate the horizontal offset for <see cref="OnRender(DrawingContext)"/>.</param>
        /// <param name="verticalOffset">Used to calculate the vertical offset for <see cref="OnRender(DrawingContext)"/>.</param>
        /// <param name="showHours">Option to show hours on the time stamp.</param>
        /// <param name="showMilliseconds">Option to show milliseconds on the time stamp.</param>
        internal void UpdateVisual(DateTime timeStamp, ITextViewLine line, IWpfTextView view, TextRunProperties formatting, double marginWidth, double verticalOffset,
                                   bool showHours, bool showMilliseconds)
        {
            this.LineTag = line.IdentityTag;

            if (timeStamp != this.TimeStamp)
            {
                this.TimeStamp = timeStamp;
                string text = GetFormattedTime(timeStamp, showHours, showMilliseconds);
                TextFormattingMode textFormattingMode = view.FormattedLineSource.UseDisplayMode ? TextFormattingMode.Display : TextFormattingMode.Ideal;
                _text = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                          formatting.Typeface, formatting.FontRenderingEmSize, formatting.ForegroundBrush,
                                          InvariantNumberSubstitution, textFormattingMode);

                _horizontalOffset = Math.Round(marginWidth - _text.Width);
                this.InvalidateVisual(); // force redraw
            }

            double newVerticalOffset = line.TextTop - view.ViewportTop + verticalOffset;
            if (newVerticalOffset != _verticalOffset)
            {
                _verticalOffset = newVerticalOffset;
                this.InvalidateVisual(); // force redraw
            }
        }

        /// <summary>
        /// Format a time stamp for display purposes.
        /// </summary>
        /// <remarks>
        ///     Examples:
        ///         13:11:11.123 (showHours + showMilliseconds)
        ///         11:11.123 (!showHours + showMilliseconds)
        ///         11:11     (!showHours + !showMilliseconds)
        /// </remarks>  
        /// <param name="timeStamp">Time stamp to format.</param>
        /// <param name="showHours">Option to show hours on the time stamp.</param>
        /// <param name="showMilliseconds">Option to show milliseconds on the time stamp.</param>
        /// <returns>A formatted time stamp as a <see cref="string"/>.</returns>
        private string GetFormattedTime(DateTime timeStamp, bool showHours, bool showMilliseconds)
        {
            string template = "{1:D2}:{2:D2}";
            if (showHours)
            {
                template = "{0,2}:" + template;
            }

            if (showMilliseconds)
            {
                template = template + ".{3:D3}";
            }

            return string.Format(CultureInfo.InvariantCulture, template, timeStamp.Hour, timeStamp.Minute, timeStamp.Second, timeStamp.Millisecond);
        }
    }
}
