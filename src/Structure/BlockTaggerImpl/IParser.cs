using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    public interface IParser
    {
        Task<CodeBlock> ParseAsync(ITextSnapshot snapshot, CancellationToken token);
    }
}
