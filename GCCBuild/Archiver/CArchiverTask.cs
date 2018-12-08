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
        public string Platform { get; set; }
        public string ConfigurationType { get; set; }
        public ITaskItem[] GCCToolArchiver_Flags { get; set; }
        public string GCCToolArchiver_AllFlags { get; set; }

        public CArchiverTask()
        {
            CommandLineArgs = new List<string>();
        }


        [Required]
        public string OutputFile { get; set; }

        public override bool Execute()
        {
            if (String.IsNullOrEmpty(WSLApp))
                UseWSL = false;
            if (!UseWSL)
                WSLApp = null;

            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically; this put in constructor causes NRE 

            if (!ObjectFiles.Any())
            {
                return true;
            }

            var lfiles = new List<string>();
            var ofiles = ObjectFiles.Select(x => x.ItemSpec);

            if (String.IsNullOrEmpty(GCCToolArchiverPath))
                GCCToolArchiverPath = "";

            string GCCToolArchiverCombined = GCCToolArchiverPath;

            if (String.IsNullOrEmpty(GCCToolArchiverCombined))
                GCCToolArchiverCombined = Utilities.FixAppPath(GCCToolArchiverExe);
            else
                GCCToolArchiverCombined = Path.Combine(GCCToolArchiverPath, GCCToolArchiverExe);

            if (UseWSL)
                OutputFile = Utilities.ConvertWinPathToWSL(OutputFile);
            else if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));

            // archiing - librerian
            var archiver = new GAR(GCCToolArchiverCombined, WSLApp);

            Dictionary<string, string> Flag_overrides = new Dictionary<string, string>();
            Flag_overrides.Add("OutputFile", OutputFile);

            var flags = Utilities.GetConvertedFlags(GCCToolArchiver_Flags, GCCToolArchiver_AllFlags, ObjectFiles[0], Flag_overrides, UseWSL);


            return archiver.Archive(ofiles, OutputFile, flags);
        }

        private const string DefaultLinker = "ar";
        private List<string> CommandLineArgs { get; }

    }
}

