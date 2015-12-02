using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    internal abstract class CsharpVBBlockParser : IParser
    {
        protected abstract void ParseSyntaxNode(ITextSnapshot snapshot, SyntaxNode parentSyntaxNode, CodeBlock parentCodeBlockNode, CancellationToken token, int level);

        public async Task<CodeBlock> ParseAsync(ITextSnapshot snapshot, CancellationToken token)
        {
            CodeBlock parentCodeBlockNode = null;
            try
            {
                parentCodeBlockNode = await GetandParseSyntaxNodeAsync(snapshot, token);
            }
            catch (TaskCanceledException)
            {
                //ignore the exception.
            }

            return parentCodeBlockNode;
        }

        private async Task<CodeBlock> GetandParseSyntaxNodeAsync(ITextSnapshot snapshot, CancellationToken token)
        {
            Document document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            SyntaxNode parentSyntaxNode = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);

            CodeBlock parentCodeBlockNode = new CodeBlock(null, BlockType.Root, null, new SnapshotSpan(snapshot, 0, snapshot.Length), 0, 0);

            ParseSyntaxNode(snapshot, parentSyntaxNode, parentCodeBlockNode, token, 0);

            return parentCodeBlockNode;
        }

        protected static string StatementFromSpan(ITextSnapshot snapshot, int start, int end)
        {
            if (start >= end)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(end - start);
            bool skippingWhitespace = true;
            bool appendWhitespace = false;

            for (int i = start; (i < end); ++i)
            {
                char c = snapshot[i];

                if (char.IsWhiteSpace(c))
                {
                    skippingWhitespace = true;
                }
                else
                {
                    if (skippingWhitespace)
                    {
                        if (appendWhitespace)
                        {
                            builder.Append(' ');
                        }
                        else
                        {
                            appendWhitespace = true;
                        }

                        skippingWhitespace = false;
                    }

                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
