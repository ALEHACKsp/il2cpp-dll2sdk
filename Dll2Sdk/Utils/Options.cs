using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace Dll2Sdk.Utils
{
    class Options
    {
        [Option("il2cppDumpFiles", Required = true, HelpText = "The path for resolving input file or directory dependancies.")]
        public string Il2cppDumpFiles
        {
            get;
            set;
        }
        [Option("outPath", Required = true, HelpText = "The path where all files will be written to.")]
        public string OutDirectory
        {
            get;
            set;
        }
        [Option("useForGitDiffs", HelpText = "Remove all variables for git comparison.")]
        public bool UseForGitDiffs
        {
            get;
            set;
        }
        /* List of (absolute path) input filenames */
        [Value(0)]
        public IEnumerable<string> InputFileNames
        {
            get;
            set;
        }
        [Usage(ApplicationAlias = "Dll2Sdk")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Full usage", new Options()
                {
                    Il2cppDumpFiles = "Path to directory dummydll.",
                    OutDirectory = "Path to write generated files."
                });
            }
        }
    }
}
