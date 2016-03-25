using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text.Formatting.Implementation;

namespace CopyAsHtmlTest
{
    [TestClass]
    public class HtmlMarkupProviderTests
    {
        [TestMethod]
        public void CalculateLongestCommonWhitespacePrefix()
        {
            var samples = new Dictionary<string, int>
            {
                { "", 0 },
                { "   a\r\n   b", 3 },
                { "a\r\n   b", 0 },
                { "a\r\nb", 0 },
                { " a\r\n\r\n b", 1 },
                { "  a\r\nb", 0 },
                { "  a\r\n  b other  spaces\t \r\n  baz\t", 2 }
            };

            foreach (var sample in samples)
            {
                var actual = HtmlMarkupProvider.CalculateLongestCommonWhitespacePrefix(sample.Key) ?? string.Empty;
                Assert.AreEqual(sample.Value, actual.Length);
            }
        }

        [TestMethod]
        public void UnindentToRemoveLeadingWhitespace()
        {
            var samples = new Dictionary<string, string>
            {
                { "   a\r\n   b", "a\r\nb" },
                { "a\r\n   b", "a\r\n   b" },
                { "a\r\nb", "a\r\nb" },
                { "  a\r\nb", "  a\r\nb" },
                { "  a\r\n  b other  spaces\t \r\n  baz\t", "a\r\nb other  spaces\t \r\nbaz\t" },
                { " a\r\n  b \r\n   c\r\n  b\r\n a", "a\r\n b \r\n  c\r\n b\r\na" },
                { " a\r\n\r\n b", "a\r\n\r\nb" },
                { " a\r\n \r\n b", "a\r\n\r\nb" }
            };

            foreach (var sample in samples)
            {
                var actual = HtmlMarkupProvider.UnindentToRemoveLeadingWhitespace(sample.Key);
                Assert.AreEqual(sample.Value, actual);
            }
        }
    }
}
