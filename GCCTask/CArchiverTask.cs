using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using CCTask.Linkers;
using System;

namespace CCTask
{
    public class CArchiverTask : Task
    {
        [Required]
        public ITaskItem[] ObjectFiles { get; set; }

        public string[] AdditionalDependencies { get; set; }
        public string[] AdditionalLibraryDirectories { get; set; }

        public string GCCToolArchiverExe { get; set; }
        public string GCCToolArchiverPath { get; set; }
        public string GCCToolArchiverArchitecture { get; set; }
        public Boolean UseWSL { get; set; }
        public string WSLApp { get; set; }
        public string OS { get; set; }
        public string ConfigurationType { get; set; }

        public CArchiverTask()
        {
            CommandLineArgs = new List<string>();
        }


        [Required]
        public string OutputFile { get; set; }

        public override bool Execute()
        {
            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically; this put in constructor causes NRE 
            Logger.Instance.LogMessage("ArchiverTask output: {0}", OutputFile);

            if (!ObjectFiles.Any())
            {
                return true;
            }

            var lfiles = new List<string>();
            var ofiles = ObjectFiles.Select(x => x.ItemSpec);

            if (String.IsNullOrEmpty(GCCToolArchiverPath))
                GCCToolArchiverPath = "";

            // linking
            var linker = new GAR(string.IsNullOrEmpty(GCCToolArchiverPath) ? DefaultLinker : Path.Combine(GCCToolArchiverPath, GCCToolArchiverExe), WSLApp);
            var flags = (CommandLineArgs != null && CommandLineArgs.Any()) ? CommandLineArgs.Aggregate(string.Empty, (curr, next) => string.Format("{0} {1}", curr, next)) : string.Empty;

            return linker.Archive(ofiles, OutputFile, flags);
        }

        private const string DefaultLinker = "ar";
        private List<string> CommandLineArgs { get; }

    }
}

