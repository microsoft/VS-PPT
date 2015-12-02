using Microsoft.VisualStudio.Text;
using System;

namespace FixMixedTabsUnitTests
{
    internal class MockSnapshotLine : ITextSnapshotLine
    {
        private readonly string _text;
        private readonly ITextSnapshot _snapshot;
        private readonly int _offset;

        public MockSnapshotLine(string text, ITextSnapshot snapshot, int offset)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            _text = text;
            _snapshot = snapshot;
            _offset = offset;
        }

        public SnapshotPoint End
        {
            get
            {
                return new SnapshotPoint(_snapshot, position: _offset + this.Length - 1);
            }
        }

        public SnapshotPoint EndIncludingLineBreak
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SnapshotSpan Extent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SnapshotSpan ExtentIncludingLineBreak
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Length
        {
            get
            {
                return _text.Length;
            }
        }

        public int LengthIncludingLineBreak
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int LineBreakLength
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int LineNumber
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ITextSnapshot Snapshot
        {
            get
            {
                return _snapshot;
            }
        }

        public SnapshotPoint Start
        {
            get
            {
                return new SnapshotPoint(this.Snapshot, _offset);
            }
        }

        public string GetLineBreakText()
        {
            throw new NotImplementedException();
        }

        public string GetText()
        {
            throw new NotImplementedException();
        }

        public string GetTextIncludingLineBreak()
        {
            throw new NotImplementedException();
        }
    }
}
