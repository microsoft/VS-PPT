using FixMixedTabs;
using Xunit;

namespace FixMixedTabsUnitTests
{
        public class HasMixedTabsUnitTests
    {
        [Fact]
        public void TabThen4SpacesTest()
        {
            string text = "\tABC\n    DEF";
            Assert.True(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 4, snapshot: new MockSnapshot(text)));
        }

        [Fact]
        public void SpacesThenTabTest()
        {
            string text = "    ABC\n\tDEF";
            Assert.True(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 4, snapshot: new MockSnapshot(text)));
        }

        [Fact]
        public void NotEnoughSpacesTest()
        {
            string text = "...ABC\n\tDEF";
            Assert.False(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 4, snapshot: new MockSnapshot(text)));
        }

        [Fact]
        public void SpacesTabsMixedOnOneLineTest()
        {
            string text = "  \t  ABC\n\tDEF";
            Assert.True(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 2, snapshot: new MockSnapshot(text)));
        }

        [Fact]
        public void BadTabSizeTest()
        {
            string text = " ABC\n\tDEF";
            Assert.False(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 0, snapshot: new MockSnapshot(text)));
        }

        [Fact]
        public void SpacesNotAtLineStartTest()
        {
            string text = "A  ABC\n\tDEF";
            Assert.False(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 2, snapshot: new MockSnapshot(text)));
        }

        [Fact]
        public void TabsNotAtLineStartTest()
        {
            string text = "A\tABC\n..DEF";
            Assert.False(MixedTabsDetector.HasMixedTabsAndSpaces(tabSize: 2, snapshot: new MockSnapshot(text)));
        }
    }
}
