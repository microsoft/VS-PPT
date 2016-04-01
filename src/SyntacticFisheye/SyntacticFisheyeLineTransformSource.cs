using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.SyntacticFisheye
{
    [Export(typeof(EditorOptionDefinition))]
    [Name(SyntacticFisheyeCompressBlankLines.StaticName)]
    public sealed class SyntacticFisheyeCompressBlankLines : ViewOptionDefinition<bool>
    {
        public const string StaticName = "SyntacticFisheyeCompressBlankLines";
        public static readonly EditorOptionKey<bool> StaticKey = new EditorOptionKey<bool>(SyntacticFisheyeCompressBlankLines.StaticName);

        public override bool Default { get { return true; } }

        public override EditorOptionKey<bool> Key { get { return SyntacticFisheyeCompressBlankLines.StaticKey; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    [Name(SyntacticFisheyeCompressSimpleLines.StaticName)]
    public sealed class SyntacticFisheyeCompressSimpleLines : ViewOptionDefinition<bool>
    {
        public const string StaticName = "SyntacticFisheyeCompressSimpleLines";
        public static readonly EditorOptionKey<bool> StaticKey = new EditorOptionKey<bool>(SyntacticFisheyeCompressSimpleLines.StaticName);
        public override bool Default { get { return true; } }
        public override EditorOptionKey<bool> Key { get { return SyntacticFisheyeCompressSimpleLines.StaticKey; } }
    }

    public class SyntacticFisheyeLineTransformSource : ILineTransformSource
    {
        #region private members
        private static readonly LineTransform s_defaultTransform = new LineTransform(0.0, 0.0, 1.0);  //No compression
        private static readonly LineTransform s_simpleTransform = new LineTransform(0.0, 0.0, 0.75);  //75% vertical compression
        //private static readonly LineTransform _blankTransform = new LineTransform(0.0, 0.0, 0.5);    //50% vertical compression
        #endregion

        /// <summary>
        /// Static class factory that ensures a single instance of the line transform source/view.
        /// </summary>
        public static SyntacticFisheyeLineTransformSource Create(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty<SyntacticFisheyeLineTransformSource>(delegate { return new SyntacticFisheyeLineTransformSource(view); });
        }

        private readonly IWpfTextView _view;
        private bool _compressBlankLines;
        private bool _compressSimpleLines;

        private SyntacticFisheyeLineTransformSource(IWpfTextView view)
        {
            _view = view;

            _compressBlankLines = _view.Options.GetOptionValue(SyntacticFisheyeCompressBlankLines.StaticKey);
            _compressSimpleLines = _view.Options.GetOptionValue(SyntacticFisheyeCompressSimpleLines.StaticKey);
            _view.Options.OptionChanged += this.OnOptionChanged;
            _view.Closed += this.OnClosed;
        }

        private void OnOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            if ((e.OptionId == SyntacticFisheyeCompressBlankLines.StaticName) || (e.OptionId == SyntacticFisheyeCompressSimpleLines.StaticName))
            {
                _compressBlankLines = _view.Options.GetOptionValue(SyntacticFisheyeCompressBlankLines.StaticKey);
                _compressSimpleLines = _view.Options.GetOptionValue(SyntacticFisheyeCompressSimpleLines.StaticKey);

                if (!(_view.IsClosed || _view.InLayout))
                {
                    var firstLine = _view.TextViewLines.FirstVisibleLine;
                    _view.DisplayTextLineContainingBufferPosition(firstLine.Start, firstLine.Top - _view.ViewportTop, ViewRelativePosition.Top);
                }
            }
        }

        private void OnClosed(object sender, System.EventArgs e)
        {
            _view.Options.OptionChanged -= this.OnOptionChanged;
            _view.Closed -= this.OnClosed;
        }

        #region ILineTransformSource Members
        public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
        {
            if ((!(_compressBlankLines || _compressSimpleLines)) ||
                (line.Length > 100) || (line.End > line.Start.GetContainingLine().End) ||
                (!line.IsFirstTextViewLineForSnapshotLine) || (!line.IsLastTextViewLineForSnapshotLine))
            {
                return s_defaultTransform;   //Long or wrapped lines -- even if they don't contain interesting characters -- get the default transform to avoid the cost of checking the entire line.
            }

            bool allWhiteSpace = true;
            for (int i = line.Start; (i < line.End); ++i)
            {
                char c = line.Snapshot[i];
                if (char.IsLetterOrDigit(c))
                {
                    return s_defaultTransform;
                }
                else if (_compressBlankLines && _compressSimpleLines)
                {
                    //intentional no-op
                }
                else if (!char.IsWhiteSpace(c))
                {
                    if (!_compressSimpleLines)
                    {
                        return s_defaultTransform;
                    }

                    allWhiteSpace = false;
                }
            }

            return (allWhiteSpace && !_compressBlankLines) ? s_defaultTransform : s_simpleTransform;
        }
        #endregion
    }
}