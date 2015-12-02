using Microsoft.VisualStudio.Editor.PeekF1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.PeekF1Tests
{
    [TestClass]
    public class UrlBuilderTests
    {
        [TestMethod]
        public void TestDefault()
        {
            // default culture is en-US
            string expected = HelpUrlBuilder.HelpUrlPrefix + "en-US" + HelpUrlBuilder.HelpUrlSuffix;

            string actual = HelpUrlBuilder.Build(new Dictionary<string, string[]>());
            Assert.AreEqual(expected, actual, ignoreCase: true);
            Assert.IsTrue(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
        }

        [TestMethod]
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

                Assert.AreEqual(expected, actual, ignoreCase: true);
                Assert.IsTrue(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
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

                Assert.AreEqual(expected, actual, ignoreCase: true);
                Assert.IsTrue(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
            }
        }

        [TestMethod]
        public void TestDevLang()
        {
            string expected = string.Format(HelpUrlBuilder.HelpUrlPrefix + "en-US;k(DevLang-{0})" + HelpUrlBuilder.HelpUrlSuffix,
                Uri.EscapeDataString("csharp"));

            string actual = HelpUrlBuilder.Build(new Dictionary<string, string[]>(StringComparer.CurrentCultureIgnoreCase)
            {
                { "devlang", new[] { "csharp" } }
            });

            Assert.AreEqual(expected, actual, ignoreCase: true);
            Assert.IsTrue(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
        }

        [TestMethod]
        public void TestFrameworkMoniker()
        {
            string expected = string.Format(HelpUrlBuilder.HelpUrlPrefix + "en-US;k(TargetFrameworkMoniker-{0})" + HelpUrlBuilder.HelpUrlSuffix,
                Uri.EscapeDataString(".NETFramework,Version=v4.5.2"));

            string actual = HelpUrlBuilder.Build(new Dictionary<string, string[]>(StringComparer.CurrentCultureIgnoreCase)
            {
                { "TargetFrameworkMoniker", new[] { ".NETFramework,Version=v4.5.2" } }
            });

            Assert.AreEqual(expected, actual, ignoreCase: true);
            Assert.IsTrue(Uri.IsWellFormedUriString(actual, UriKind.Absolute));
        }
    }
}
