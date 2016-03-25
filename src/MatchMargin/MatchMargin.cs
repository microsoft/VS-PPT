using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows;

namespace Microsoft.VisualStudio.PowerTools.MatchMargin
{
    /// <summary>
    /// Implementation of an IWpfTextViewMargin that highlights the location of the words matching the word under the caret.
    /// </summary>
    internal class MatchMargin : IWpfTextViewMargin
    {
        /// <summary>
        /// Name of this margin.
        /// </summary>
        public const string Name = "MatchMargin";

        #region Private Members
        private MatchMarginElement _matchMarginElement;
        private bool _isDisposed;
        #endregion

        /// <summary>
        /// Constructor for the MatchMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        public MatchMargin(IWpfTextViewHost textViewHost, IVerticalScrollBar scrollBar, MatchMarginFactory factory)
        {
            // Validate
            if (textViewHost == null)
                throw new ArgumentNullException("textViewHost");

            _matchMarginElement = new MatchMarginElement(textViewHost.TextView, factory, scrollBar);
        }

        #region IWpfTextViewMargin Members
        /// <summary>
        /// The FrameworkElement that renders the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return _matchMarginElement;
            }
        }
        #endregion

        #region ITextViewMargin Members
        /// <summary>
        /// For a horizontal margin, this is the height of the margin (since the width will be determined by the ITextView. For a vertical margin, this is the width of the margin (since the height will be determined by the ITextView.
        /// </summary>
        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return _matchMarginElement.ActualWidth;
            }
        }

        /// <summary>
        /// The visible property, true if the margin is visible, false otherwise.
        /// </summary>
        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return _matchMarginElement.Enabled;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Compare(marginName, MatchMargin.Name, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
        }

        /// <summary>
        /// In our dipose, stop listening for events.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _matchMarginElement.Dispose();
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
        #endregion

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(Name);
        }
    }
}
