using System.Xml.Linq;
using NUnit.Framework;

namespace ValidationLibrary.MarkdownGenerator.Tests
{
    public class VisualStudioDocParserTests
    {
        private const string namespaceMatch = "ValidationLibrary.Rules";

        [Test]
        public void ParseXmlComment_ReturnsEmptyForNoComments()
        {
            Assert.IsEmpty(VisualStudioDocParser.GetTypeSummaries(XDocument.Parse("<?xml version =\"1.0\"?><doc/>"), namespaceMatch));

            var xml = "<?xml version=\"1.0\"?>" +
                    "<doc>" +
                    "<members>" +
                    "</members>" +
                    "</doc>";
            Assert.IsEmpty(VisualStudioDocParser.GetTypeSummaries(XDocument.Parse(xml), namespaceMatch));
        }

        [Test]
        public void ParseXmlComment_ReturnsSummary()
        {
            var xml = "<?xml version=\"1.0\"?>" +
                    "<doc>" +
                    "<members>" +
                    "<member name=\"T:ValidationLibrary.Rules.HasLicenseRule\">" +
                    "<summary>" +
                    "Comment here" +
                    "</summary>" +
                    "</member>" +
                    "</members>" +
                    "</doc>";
            var result = VisualStudioDocParser.GetTypeSummaries(XDocument.Parse(xml), namespaceMatch);
            Assert.AreEqual(1, result.Length);

            Assert.AreEqual("HasLicenseRule", result[0].MemberName);
            Assert.AreEqual("Comment here", result[0].Summary);
        }

        [Test]
        public void ParseXmlComment_MissingSummaryDoesntBreak()
        {
            var xml = "<?xml version=\"1.0\"?>" +
                    "<doc>" +
                    "<members>" +
                    "<member name=\"T:ValidationLibrary.Rules.HasLicenseRule\">" +
                    "</member>" +
                    "</members>" +
                    "</doc>";
            var result = VisualStudioDocParser.GetTypeSummaries(XDocument.Parse(xml), namespaceMatch);
            Assert.AreEqual(1, result.Length);

            Assert.AreEqual("HasLicenseRule", result[0].MemberName);
            Assert.AreEqual("", result[0].Summary);
        }
    }
}
