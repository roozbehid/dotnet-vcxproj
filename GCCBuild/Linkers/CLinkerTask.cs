using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using CCTask.Linkers;
using System;

namespace CCTask
{
	public class CLinkerTask : Task
	{
		[Required]
		public ITaskItem[] ObjectFiles { get; set; }

		public ITaskItem[] Libraries { get; set; }

        public string[] AdditionalDependencies { get; set; }
        public string[] AdditionalLibraryDirectories { get; set; }
        public string AdditionalOptions { get; set; }

        public Boolean GenerateDebugInformation { get; set; }
        public string GCCToolLinkerExe { get; set; }
        public string GCCToolLinkerPath { get; set; }
        public string GCCToolLinkerArchitecture { get; set; }
        public Boolean UseWSL { get; set; }
        public string WSLApp { get; set; }
        public string OS { get; set; }
        public string Platform { get; set; }
        public string ConfigurationType { get; set; }
        public ITaskItem[] GCCToolLinker_Flags { get; set; }
        public string GCCToolLinker_AllFlags { get; set; }


        public CLinkerTask()
        {
            
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
            string GCCToolLinkerPathCombined = GCCToolLinkerPath;

            if (String.IsNullOrEmpty(GCCToolLinkerPathCombined))
                GCCToolLinkerPathCombined = Utilities.FixAppPath(GCCToolLinkerExe);
            else
                GCCToolLinkerPathCombined = Path.Combine(GCCToolLinkerPath, GCCToolLinkerExe);

            if (UseWSL)
                OutputFile = Utilities.ConvertWinPathToWSL(OutputFile);
            else if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));


            // linking

            var linker = new GLD(GCCToolLinkerPathCombined, WSLApp);
            Dictionary<string, string> Flag_overrides = new Dictionary<string, string>();
            Flag_overrides.Add("OutputFile", OutputFile);

            var flags = Utilities.GetConvertedFlags(GCCToolLinker_Flags, GCCToolLinker_AllFlags, ObjectFiles[0], Flag_overrides, UseWSL);

            return linker.Link(ofiles, OutputFile, flags);
        }

        
        private const string DefaultLinker = "g++";


    }
}

