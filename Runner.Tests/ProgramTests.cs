using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Runner.Tests
{
    public class ProgramTests
    {
        [Test]
        public async Task Main_StartupWorks()
        {
            // This is just a quick dumb test for DI
            await Program.Main(new string[0]);
        }

        [Test]
        public async Task Main_DocumentationIsGeneratedToCorrectFolderWithSomeContent()
        {
            // Guid is used as a folder name to avoid outputting to some existing folder and deleting it.
            var folderName = Guid.NewGuid().ToString();

            try
            {
                await Program.Main(new string[] { "generate-document", "-o", folderName });

                Assert.IsTrue(Directory.Exists(folderName));
                var fileNames = Directory.GetFiles(folderName);

                Assert.Greater(fileNames.Length, 1, "There should be at least Rules.md and other rules.");
                Assert.Contains($"{folderName}{Path.DirectorySeparatorChar}Rules.md", fileNames, "There should be at least Rules.md");

                foreach (var fileName in fileNames)
                {
                    var lines = File.ReadAllLines(fileName);
                    Assert.IsTrue(fileName.EndsWith(".md"), "Currently all files should be markdown files.");
                    Assert.Greater(lines.Length, 0, "There should be some content in the file.");
                }
            }
            finally
            {
                Directory.Delete(folderName, true);
            }
        }
    }
}
