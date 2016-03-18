using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting.Implementation;

namespace CopyAsHtmlTest
{
    [TestClass]
    public class FormattedStringBuilderTests
    {
        [TestMethod]
        public void FormattedStringBuilderEndToEnd1()
        {
            var classificationFormatMap = new MockClassificationFormatMap();
            var htmlMarkupProvider = new HtmlMarkupProvider(
                classificationFormatMap, 
                MockClassificationType.Default,
                Brushes.White);
            var classifier = new MockClassifier();

            var formattedStringBuilder = new FormattedStringBuilder(
                htmlMarkupProvider,
                classifier,
                MockClassificationType.Default,
                waitIndicator: null);

            var snapshot = new MockTextSnapshot("bla");
            var spans = new NormalizedSnapshotSpanCollection(new [] 
            {
                new SnapshotSpan(snapshot, 0, 3)
            });

            var actualResult = formattedStringBuilder.AppendSnapshotSpans(spans);
            var expectedResult = "<pre style=\"font-family:Consolas;font-size:12;color:black;background:white;\">b<span style=\"color:blue;\">l</span>a\r\n</pre>";

            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
