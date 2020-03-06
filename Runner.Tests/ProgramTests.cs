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
    }
}
