using FixMixedTabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FixMixedTabsUnitTests
{
    [TestClass]
    public class HasMixedTabsUnitTests
    {
        [TestMethod]
        public void TabThen4SpacesTest()
        {
            string text = "\tABC\n    DEF";
            Assert.IsTrue(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 4, snapshot: new MockSnapshot(text)));
        }

        [TestMethod]
        public void SpacesThenTabTest()
        {
            string text = "    ABC\n\tDEF";
            Assert.IsTrue(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 4, snapshot: new MockSnapshot(text)));
        }

        [TestMethod]
        public void NotEnoughSpacesTest()
        {
            string text = "...ABC\n\tDEF";
            Assert.IsFalse(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 4, snapshot: new MockSnapshot(text)));
        }

        [TestMethod]
        public void SpacesTabsMixedOnOneLineTest()
        {
            string text = "  \t  ABC\n\tDEF";
            Assert.IsTrue(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 2, snapshot: new MockSnapshot(text)));
        }

        [TestMethod]
        public void BadTabSizeTest()
        {
            string text = " ABC\n\tDEF";
            Assert.IsFalse(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 0, snapshot: new MockSnapshot(text)));
        }

        [TestMethod]
        public void SpacesNotAtLineStartTest()
        {
            string text = "A  ABC\n\tDEF";
            Assert.IsFalse(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 2, snapshot: new MockSnapshot(text)));
        }

        [TestMethod]
        public void TabsNotAtLineStartTest()
        {
            string text = "A\tABC\n..DEF";
            Assert.IsFalse(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 2, snapshot: new MockSnapshot(text)));
        }
    }
}
