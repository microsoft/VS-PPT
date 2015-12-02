using Microsoft.VisualStudio.Text;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    internal class BaseFilter
    {
        protected ITextSnapshot snapshot;
        protected int position;

        public BaseFilter(ITextSnapshot snapshot)
        {
            this.snapshot = snapshot;
            this.position = -1;
        }

        public char Character { get { return this.snapshot[this.position]; } }
        public int Position { get { return this.position; } }

        protected char PeekNextChar()
        {
            return PeekNextChar(1);
        }

        protected char PeekNextChar(int offset)
        {
            int p = this.position + offset;

            if ((0 <= p) && (p < this.snapshot.Length))
                return this.snapshot[p];
            else
                return ' ';
        }
    }
}
