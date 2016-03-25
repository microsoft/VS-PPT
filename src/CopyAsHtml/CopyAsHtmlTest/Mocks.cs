using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using System.Windows.Media;

namespace CopyAsHtmlTest
{
    public class MockClassifier : IClassifier
    {
#pragma warning disable 67
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return new List<ClassificationSpan>
                {
                    new ClassificationSpan(
                        new SnapshotSpan(
                            span.Snapshot, 
                            new Span(1, 1)), 
                            new MockClassificationType("keyword"))
                };
        }
    }

    public class MockClassificationFormatMap : IClassificationFormatMap
    {
        public void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties, IClassificationType priority)
        {
            throw new NotImplementedException();
        }

        public void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties)
        {
            throw new NotImplementedException();
        }

        public void BeginBatchUpdate()
        {
            throw new NotImplementedException();
        }

#pragma warning disable 67
        public event EventHandler<EventArgs> ClassificationFormatMappingChanged;
#pragma warning restore 67

        public System.Collections.ObjectModel.ReadOnlyCollection<IClassificationType> CurrentPriorityOrder
        {
            get { throw new NotImplementedException(); }
        }

        public TextFormattingRunProperties DefaultTextProperties
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void EndBatchUpdate()
        {
            throw new NotImplementedException();
        }

        public string GetEditorFormatMapKey(IClassificationType classificationType)
        {
            throw new NotImplementedException();
        }

        public TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType)
        {
            throw new NotImplementedException();
        }

        public TextFormattingRunProperties GetTextProperties(IClassificationType classificationType)
        {
            Color color = Colors.Black;

            if (classificationType.Classification != "text")
            {
                color = Colors.Blue;
            }

            return TextFormattingRunProperties.CreateTextFormattingRunProperties(
                new Typeface("Consolas"), 12, color);
        }

        public bool IsInBatchUpdate
        {
            get { throw new NotImplementedException(); }
        }

        public void SetExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties)
        {
            throw new NotImplementedException();
        }

        public void SetTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties)
        {
            throw new NotImplementedException();
        }

        public void SwapPriorities(IClassificationType firstType, IClassificationType secondType)
        {
            throw new NotImplementedException();
        }
    }

    public class MockClassificationType : IClassificationType
    {
        private string text;

        public MockClassificationType()
            : this("text")
        {

        }

        static MockClassificationType _default = new MockClassificationType();
        public static MockClassificationType Default
        {
            get
            {
                return _default;
            }
        }

        public MockClassificationType(string classificationTypeText)
        {
            this.text = classificationTypeText;
        }

        public IEnumerable<IClassificationType> BaseTypes
        {
            get { throw new NotImplementedException(); }
        }

        public string Classification
        {
            get { return text; }
        }

        public bool IsOfType(string type)
        {
            return type == text;
        }
    }

    public class MockTextSnapshot : ITextSnapshot
    {
        private string _text;

        public MockTextSnapshot(string text)
        {
            _text = text;
        }

        public Microsoft.VisualStudio.Utilities.IContentType ContentType
        {
            get { throw new NotImplementedException(); }
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            throw new NotImplementedException();
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        {
            throw new NotImplementedException();
        }

        public ITextSnapshotLine GetLineFromPosition(int position)
        {
            throw new NotImplementedException();
        }

        public int GetLineNumberFromPosition(int position)
        {
            throw new NotImplementedException();
        }

        public string GetText()
        {
            throw new NotImplementedException();
        }

        public string GetText(int startIndex, int length)
        {
            throw new NotImplementedException();
        }

        public string GetText(Span span)
        {
            return _text.Substring(span.Start, span.Length);
        }

        public int Length
        {
            get { return _text.Length; }
        }

        public int LineCount
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<ITextSnapshotLine> Lines
        {
            get { throw new NotImplementedException(); }
        }

        public ITextBuffer TextBuffer
        {
            get { throw new NotImplementedException(); }
        }

        public char[] ToCharArray(int startIndex, int length)
        {
            throw new NotImplementedException();
        }

        public ITextVersion Version
        {
            get { throw new NotImplementedException(); }
        }

        public void Write(System.IO.TextWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Write(System.IO.TextWriter writer, Span span)
        {
            throw new NotImplementedException();
        }

        public char this[int position]
        {
            get { throw new NotImplementedException(); }
        }
    }
}
