using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.OverviewMargin;

namespace Microsoft.VisualStudio.PowerTools.StructureVisualizer
{
    /// <summary>
    /// Implementation of a margin that show the structure of a code file.
    /// </summary>
    internal class StructureMargin : IWpfTextViewMargin, IOverviewTipManager
    {
        /// <summary>
        /// Name of this margin.
        /// </summary>
        public const string Name = "Structure";

        #region Private Members
        private StructureMarginElement _structureMarginElement;
        private bool _isDisposed;
        #endregion

        /// <summary>
        /// Constructor for the StructureMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        public StructureMargin(IWpfTextViewHost textViewHost, IVerticalScrollBar scrollBar, StructureMarginFactory factory)
        {
            // Validate
            if (textViewHost == null)
                throw new ArgumentNullException("textViewHost");

            _structureMarginElement = new StructureMarginElement(textViewHost.TextView, scrollBar, factory);
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
                return _structureMarginElement;
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
                return _structureMarginElement.ActualWidth;
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
                return _structureMarginElement.Enabled;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Compare(marginName, StructureMargin.Name, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _structureMarginElement.Dispose();
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
        #endregion

        #region IOverviewTipManager Members
        public bool UpdateTip(IVerticalScrollBar margin, MouseEventArgs e, ToolTip tip)
        {
            return this.Enabled ? _structureMarginElement.UpdateTip(margin, e, tip) : false;
        }
        #endregion

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(Name);
        }
    }
}
