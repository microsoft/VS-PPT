using Microsoft.VisualStudio.Editor.PeekF1;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualStudio.PeekF1Tests
{
    public class UrlBuilderTests
    {
        [Fact]
        public void TestDefault()
        {
            // default culture is en-US
            string expected = HelpUrlBuilder.HelpUrlPrefix + "en-US" + HelpUrlBuilder.HelpUrlSuffix;

            string actual = HelpUrlBuilder.Build(new Dictionary<string, string[]>());
            Assert.Equal(expected, actual, ignoreCase: true);
            Assert.True(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
        }

        [Fact]
        public void TestKeyword()
        {
            { // test one keyword
                string keyword = "keyword1";
                string expected = string.Format(HelpUrlBuilder.HelpUrlPrefix +
                    "en-US&k=k({0})" + HelpUrlBuilder.HelpUrlSuffix, Uri.EscapeDataString(keyword));

                string actual = HelpUrlBuilder.Build(new Dictionary<string, string[]>
                {
                    { "keyword", new[] { keyword } }
                });

                Assert.Equal(expected, actual, ignoreCase: true);
                Assert.True(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
            }

            { // test multiple keywords
                string[] keywords = new[] { "keyword1", "keyword2" };
                string expected = HelpUrlBuilder.HelpUrlPrefix + "en-US";
                foreach (var keyword in keywords)
                {
                    expected += string.Format("&k=k({0})", Uri.EscapeDataString(keyword));
                }
                expected += HelpUrlBuilder.HelpUrlSuffix;

                string actual = HelpUrlBuilder.Build(new Dictionary<string, string[]>
                {
                    { "keyword", keywords }
                });

                Assert.Equal(expected, actual, ignoreCase: true);
                Assert.True(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
            }
        }

        [Fact]
        public void TestDevLang()
        {
            string expected = string.Format(HelpUrlBuilder.HelpUrlPrefix + "en-US;k(DevLang-{0})" + HelpUrlBuilder.HelpUrlSuffix,
                Uri.EscapeDataString("csharp"));

            string actual = HelpUrlBuilder.Build(new Dictionary<string, string[]>(StringComparer.CurrentCultureIgnoreCase)
            {
                { "devlang", new[] { "csharp" } }
            });

            Assert.Equal(expected, actual, ignoreCase: true);
            Assert.True(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
        }

        [Fact]
        public void TestFrameworkMoniker()
        {
            string expected = string.Format(HelpUrlBuilder.HelpUrlPrefix + "en-US;k(TargetFrameworkMoniker-{0})" + HelpUrlBuilder.HelpUrlSuffix,
                Uri.EscapeDataString(".NETFramework,Version=v4.5.2"));

            string actual = HelpUrlBuilder.Build(new Dictionary<string, string[]>(StringComparer.CurrentCultureIgnoreCase)
            {
                { "TargetFrameworkMoniker", new[] { ".NETFramework,Version=v4.5.2" } }
            });

            Assert.Equal(expected, actual, ignoreCase: true);
            Assert.True(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
        }
    }
}
