using CommandLine;

namespace Runner
{
    [Verb("generate-document", HelpText = "Generated markdown documentation for rules")]
    public class GenerateDocumentationOptions
    {
        [Option('o', "OutputFolder", Required = false, HelpText = "Name of the output folder", Default = "wiki")]
        public string OutputFolder { get; }

        public GenerateDocumentationOptions(string outputFolder)
        {
            OutputFolder = outputFolder;
        }
    }
}