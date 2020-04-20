using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace Dll2Sdk.Utils
{
    class Options
    {
        [Option("dummyDllPath", Required = true, HelpText = "The path for resolving input file or directory dependancies.")]
        public string DummyDllPath
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

        [Option("clean", HelpText = "Remove method.Rid")]
        public bool Clean
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
                    DummyDllPath = "Path to directory dummydll.",
                    OutDirectory = "Path to write generated files."
                });
            }
        }
    }
}
