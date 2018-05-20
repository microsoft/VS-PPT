using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using CopyAsHtml;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.Text.Formatting.Implementation
{
    public sealed class HtmlMarkupProvider
    {
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly TextRunProperties _defaultTextProperties;
        private readonly IClassificationType _defaultClassificationType;
        private Dictionary<Color, string> _knownColors;
        private Dictionary<string, string> _defaultCss;

        public static int TabSize { get; set; }
        public static string LongestPrefix { get; internal set; }

        public HtmlMarkupProvider(
            IClassificationFormatMap classificationFormatMap,
            IClassificationType defaultClassificationType,
            Brush backgroundColor)
        {
            _classificationFormatMap = classificationFormatMap;
            _defaultClassificationType = defaultClassificationType;
            _defaultTextProperties = _classificationFormatMap.GetTextProperties(_defaultClassificationType);
            _defaultCss = GetCssStyles(_defaultClassificationType);
            _defaultCss["background"] = GetColor(backgroundColor);
        }

        public string GetMarkupBeforeCodeSnippet()
        {
            string result = ToolsOptionsPage.Instance.BeforeCodeSnippet;
            result = SubstituteCSSValues(result);
            return result;
        }

        public string GetMarkupAfterCodeSnippet()
        {
            string result = ToolsOptionsPage.Instance.AfterCodeSnippet;
            result = SubstituteCSSValues(result);
            return result;
        }

        /// <summary>
        /// This replaces a string like {font-family}, {font-size}
        /// with font-family: "Consolas";, font-size: 11; depending on the text properties
        /// for the default classification type.
        /// Used for saving the font, forecolor and background color of the default text
        /// </summary>
        private string SubstituteCSSValues(string result)
        {
            foreach (var styleKVP in _defaultCss)
            {
                string replacement = string.Empty;
                if (!string.IsNullOrEmpty(styleKVP.Value))
                {
                    replacement = styleKVP.Key + ":" + styleKVP.Value + ";";
                }

                result = result.Replace("{" + styleKVP.Key + "}", replacement);
            }

            result = result.Replace("{font-weight}", string.Empty);
            result = result.Replace("{font-style}", string.Empty);
            result = result.Replace("{color}", string.Empty);
            result = result.Replace("{background}", string.Empty);

            return result;
        }

        public string GetMarkupForSpan(ClassificationSpan classifiedSpan)
        {
            string result = classifiedSpan.Span.GetText();

            result = HtmlEscape(result);

            if (classifiedSpan.ClassificationType.Classification == _defaultClassificationType.Classification)
            {
                return result;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("<span");

            if (ToolsOptionsPage.Instance.EmitSpanClass)
            {
                sb.Append(" class=\"");
                sb.Append(classifiedSpan.ClassificationType.Classification);
                sb.Append("\"");
            }

            var css = GetCSS(classifiedSpan.ClassificationType);

            if (!ToolsOptionsPage.Instance.EmitSpanClass
                && (!ToolsOptionsPage.Instance.EmitSpanStyle || string.IsNullOrEmpty(css)))
            {
                return result;
            }

            if (ToolsOptionsPage.Instance.EmitSpanStyle && !string.IsNullOrEmpty(css))
            {
                sb.Append(" style=\"");
                sb.Append(css);
                sb.Append("\"");
            }

            sb.Append(">");
            sb.Append(result);
            sb.Append("</span>");

            return sb.ToString();
        }

        private static string HtmlEscape(string text)
        {
            if (ToolsOptionsPage.Instance.UnindentToRemoveExtraLeadingWhitespace && LongestPrefix != null)
            {
                text = text.Replace("\r\n" + LongestPrefix, "\r\n");
            }

            text = System.Security.SecurityElement.Escape(text);
            // HTML doesn't support XML's &apos;
            // need to use &#39; instead
            // http://blogs.msdn.com/kirillosenkov/archive/2010/03/19/apos-is-in-xml-in-html-use-39.aspx#comments
            // http://www.w3.org/TR/html4/sgml/entities.html
            // http://lists.whatwg.org/pipermail/whatwg-whatwg.org/2005-October/004973.html
            // http://en.wikipedia.org/wiki/List_of_XML_and_HTML_character_entity_references
            // http://fishbowl.pastiche.org/2003/07/01/the_curse_of_apos/
            // http://nedbatchelder.com/blog/200703/random_html_factoid_no_apos.html
            // Don't want to use System.Web.HttpUtility.HtmlEncode
            // because I don't want to take a dependency on System.Web
            text = text.Replace("&apos;", "&#39;");
            text = text.Replace(" ", ToolsOptionsPage.Instance.Space);

            if (TabSize < 1 || TabSize > 64)
            {
                TabSize = 4;
            }

            text = IntersperseLineBreaks(text);
            if (ToolsOptionsPage.Instance.ReplaceLineBreaksWithBR)
            {
                text = text.Replace(Environment.NewLine, "<br/>");
            }

            if (ToolsOptionsPage.Instance.ReplaceTabsWithSpaces)
            {
                var spaces = string.Concat(Enumerable.Repeat(ToolsOptionsPage.Instance.Space, TabSize));
                text = text.Replace("\t", spaces);
            }

            return text;
        }

        public static string UnindentToRemoveLeadingWhitespace(string text)
        {
            string commonWhitespacePrefix = CalculateLongestCommonWhitespacePrefix(text);
            if (string.IsNullOrEmpty(commonWhitespacePrefix))
            {
                return text;
            }

            text = TrimFromStartOfLines(text, commonWhitespacePrefix);
            return text;
        }

        private static string TrimFromStartOfLines(string text, string commonPrefix)
        {
            text = text.Replace("\r\n" + commonPrefix, "\r\n");
            if (text.StartsWith(commonPrefix))
            {
                text = text.Substring(commonPrefix.Length);
            }

            return text;
        }

        private enum ScanState
        {
            Beginning,
            NonWhitespace
        }

        public enum WhitespaceKind
        {
            Unknown,
            Spaces,
            Tabs,
            Mixed
        }

        public static string CalculateLongestCommonWhitespacePrefix(string text)
        {
            int longest = int.MaxValue;
            int current = 0;
            ScanState state = ScanState.Beginning;
            var whitespaceKind = WhitespaceKind.Unknown;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    if (state == ScanState.Beginning)
                    {
                        if (whitespaceKind == WhitespaceKind.Unknown || whitespaceKind == WhitespaceKind.Spaces)
                        {
                            whitespaceKind = WhitespaceKind.Spaces;
                            current++;
                        }
                        else
                        {
                            whitespaceKind = WhitespaceKind.Mixed;
                            state = ScanState.NonWhitespace;
                            if (longest > current)
                            {
                                longest = current;
                            }
                        }
                    }
                }
                else if (text[i] == '\t')
                {
                    if (state == ScanState.Beginning)
                    {
                        if (whitespaceKind == WhitespaceKind.Unknown || whitespaceKind == WhitespaceKind.Tabs)
                        {
                            whitespaceKind = WhitespaceKind.Tabs;
                            current++;
                        }
                        else
                        {
                            whitespaceKind = WhitespaceKind.Mixed;
                            state = ScanState.NonWhitespace;
                            if (longest > current)
                            {
                                longest = current;
                            }
                        }
                    }
                }
                else if (text[i] == '\r')
                {
                    state = ScanState.Beginning;
                    current = 0;
                }
                else if (text[i] == '\n')
                {
                    state = ScanState.Beginning;
                    current = 0;
                }
                else
                {
                    state = ScanState.NonWhitespace;
                    if (longest > current)
                    {
                        longest = current;
                    }
                }
            }

            if (longest > text.Length)
            {
                longest = text.Length;
            }

            string result = null;
            if (longest > 0)
            {
                if (whitespaceKind == WhitespaceKind.Spaces)
                {
                    result = new string(' ', longest);
                }
                else if (whitespaceKind == WhitespaceKind.Tabs)
                {
                    result = new string('\t', longest);
                }
            }

            return result;
        }

        private static string IntersperseLineBreaks(string text)
        {
            text = text.Replace("\n\r", "\n \r");
            return text;
        }

        /// <summary>
        /// Get the non-default styles and flatten them into a CSS attribute string
        /// </summary>
        private string GetCSS(IClassificationType classificationType)
        {
            var styles = GetCssStyles(classificationType);
            StringBuilder sb = new StringBuilder();
            foreach (var style in styles)
            {
                string defaultValue;
                if (_defaultCss.TryGetValue(style.Key, out defaultValue) && defaultValue == style.Value)
                {
                    continue;
                }

                sb.Append(style.Key + ":" + style.Value + ";");
            }

            var result = sb.ToString();
            return result;
        }

        private Dictionary<string, string> GetCssStyles(IClassificationType classificationType)
        {
            TextRunProperties properties =
                _classificationFormatMap.GetTextProperties(classificationType);

            var styles = new Dictionary<string, string>();

            var face = GetFontFamily(properties);
            if (!string.IsNullOrEmpty(face))
            {
                styles.Add("font-family", face);
            }

            string currentSize = GetFontSize(properties);
            if (!string.IsNullOrEmpty(currentSize))
            {
                styles.Add("font-size", currentSize);
            }

            var fontStyle = GetFontStyle(properties);
            if (!string.IsNullOrEmpty(fontStyle))
            {
                styles.Add("font-style", fontStyle);
            }

            var fontWeight = GetFontWeight(properties);
            if (!string.IsNullOrEmpty(fontWeight))
            {
                styles.Add("font-weight", fontWeight);
            }

            var foreground = GetColor(properties.ForegroundBrush);
            if (!string.IsNullOrEmpty(foreground))
            {
                styles.Add("color", foreground);
            }

            var background = GetColor(properties.BackgroundBrush);
            if (!string.IsNullOrEmpty(background))
            {
                styles.Add("background", background);
            }

            return styles;
        }

        private static string GetFontWeight(TextRunProperties properties)
        {
            string weight = properties.Typeface.Weight.ToString().ToLowerInvariant();
            if (weight == "normal")
            {
                return null;
            }

            return weight;
        }

        private static string GetFontStyle(TextRunProperties properties)
        {
            string style = properties.Typeface.Style.ToString().ToLowerInvariant();
            if (style == "normal")
            {
                style = null;
            }

            return style;
        }

        /// <summary>
        /// Gets string representation of Font Size in DIPs (Device Independent Pixels). Eg: "12px".
        /// </summary>
        private static string GetFontSize(TextRunProperties properties)
        {
            return ((int)properties.FontRenderingEmSize).ToString() + "px";
        }

        private string GetFontFamily(TextRunProperties properties)
        {
            return properties.Typeface.FontFamily.Source;
        }

        public string GetKnownColor(Color color)
        {
            if (_knownColors == null)
            {
                _knownColors = new Dictionary<Color, string>();
                foreach (var colorProperty in typeof(Colors).GetProperties())
                {
                    var c = (Color)colorProperty.GetValue(null, null);
                    _knownColors[c] = colorProperty.Name.ToLowerInvariant();
                }
            }

            string result = null;
            _knownColors.TryGetValue(color, out result);
            return result;
        }

        private string GetHtmlColorCode(Color c)
        {
            string result = GetKnownColor(c);

            if (string.IsNullOrEmpty(result))
            {
                result = string.Format(CultureInfo.InvariantCulture, "#{0:x2}{1:x2}{2:x2}", c.R, c.G, c.B);
            }

            return result;
        }

        private string GetColor(Brush brush)
        {
            var solidColorBrush = brush as SolidColorBrush;
            if (solidColorBrush == null)
            {
                return null;
            }

            var result = GetHtmlColorCode(solidColorBrush.Color);
            if (result == "transparent")
            {
                return null;
            }

            return result;
        }
    }
}
