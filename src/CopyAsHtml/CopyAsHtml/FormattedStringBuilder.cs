using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CopyAsHtml;
using Microsoft.VisualStudio.Language.Intellisense.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.Text.Formatting.Implementation
{
    public sealed class FormattedStringBuilder
    {
        private readonly HtmlMarkupProvider _htmlMarkupProvider;
        private readonly IClassifier _classifier;
        private readonly IClassificationType _defaultClassificationType;
        private readonly IWaitIndicator _waitIndicator;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        public FormattedStringBuilder(
            HtmlMarkupProvider htmlMarkupProvider,
            IClassifier classifier,
            IClassificationType defaultClassificationType,
            IWaitIndicator waitIndicator)
        {
            _htmlMarkupProvider = htmlMarkupProvider;
            _classifier = classifier;
            _defaultClassificationType = defaultClassificationType;
            _waitIndicator = waitIndicator;
        }

        public string AppendSnapshotSpans(NormalizedSnapshotSpanCollection spans)
        {
            AppendBeginCodeSnippet();

            // if there is a single stream selection span, we can attempt to unindent
            if (ToolsOptionsPage.Instance.UnindentToRemoveExtraLeadingWhitespace && spans.Count == 1)
            {
                var plaintext = spans[0].GetText();
                HtmlMarkupProvider.LongestPrefix = HtmlMarkupProvider.CalculateLongestCommonWhitespacePrefix(plaintext);
            }
            else
            {
                HtmlMarkupProvider.LongestPrefix = null;
            }

            foreach (var span in spans)
            {
                AppendSnapshotSpan(span);
            }

            AppendEndCodeSnippet();
            return _stringBuilder.ToString();
        }

        private void AppendBeginCodeSnippet()
        {
            Append(_htmlMarkupProvider.GetMarkupBeforeCodeSnippet());
        }

        private void AppendEndCodeSnippet()
        {
            Append(_htmlMarkupProvider.GetMarkupAfterCodeSnippet());
        }

        public void AppendSnapshotSpan(SnapshotSpan span)
        {
            var classifiedSpans = this.GetClassificationSpans(span);

            if (ToolsOptionsPage.Instance.UnindentToRemoveExtraLeadingWhitespace && HtmlMarkupProvider.LongestPrefix != null)
            {
                if (classifiedSpans.Count > 0 && classifiedSpans[0].Span.GetText() == HtmlMarkupProvider.LongestPrefix)
                {
                    classifiedSpans = classifiedSpans.Skip(1).ToList();
                }
            }

            foreach (var classifiedSpan in classifiedSpans)
            {
                AppendClassifiedSpan(classifiedSpan);
            }

            if (!span.GetText().Contains(Environment.NewLine))
            {
                var lineBreak = ToolsOptionsPage.Instance.ReplaceLineBreaksWithBR ? "<br/>" : Environment.NewLine;
                Append(lineBreak);
            }
        }

        public void AppendClassifiedSpan(ClassificationSpan classifiedSpan)
        {
            Append(_htmlMarkupProvider.GetMarkupForSpan(classifiedSpan));
        }

        private void Append(string text)
        {
            _stringBuilder.Append(text);
        }

        private IList<ClassificationSpan> GetClassificationSpansSync(SnapshotSpan parentSpan)
        {
            return _classifier.GetClassificationSpans(parentSpan);
        }

        /// <summary>
        /// Get the classified spans with unclassified spans given the "text" classification
        /// </summary>
        /// <remarks>
        /// This "fills in the gaps" between the classified spans by interspersing them with 
        /// spans classified with <see cref="_defaultClassificationType"/>
        /// </remarks>
        private IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan parentSpan)
        {
            var classifiedSpans = new List<ClassificationSpan>();
            var snapshot = parentSpan.Snapshot;

            int start = parentSpan.Start;
            foreach (ClassificationSpan classifiedSpan in GetClassificationSpansSync(parentSpan))
            {
                if (classifiedSpan.Span.Start > start)
                {
                    classifiedSpans.Add(
                        new ClassificationSpan(
                            new SnapshotSpan(
                                snapshot,
                                Span.FromBounds(start, classifiedSpan.Span.Start)),
                            _defaultClassificationType));
                }

                classifiedSpans.Add(classifiedSpan);
                start = classifiedSpan.Span.End;
            }

            if (start < parentSpan.End)
            {
                classifiedSpans.Add(
                    new ClassificationSpan(
                        new SnapshotSpan(
                            snapshot,
                            Span.FromBounds(start, parentSpan.End)),
                        _defaultClassificationType));
            }

            return classifiedSpans;
        }
    }
}
