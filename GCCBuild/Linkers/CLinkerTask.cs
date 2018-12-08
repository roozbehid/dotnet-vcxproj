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
            Logger.Instance.LogMessage("LinkerTask output: {0}", OutputFile);

            if (!ObjectFiles.Any())
            {
                return true;
            }

            var lfiles = new List<string>();
            var ofiles = ObjectFiles.Select(x => x.ItemSpec);

            if (String.IsNullOrEmpty(GCCToolLinkerPath))
                GCCToolLinkerPath = "";

            if (UseWSL)
                OutputFile = Utilities.ConvertWinPathToWSL(OutputFile);
            else if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));


            // linking
            var linker = new GLD(string.IsNullOrEmpty(GCCToolLinkerPath) ? DefaultLinker : Path.Combine(GCCToolLinkerPath, GCCToolLinkerExe), WSLApp);
            Dictionary<string, string> Flag_overrides = new Dictionary<string, string>();
            Flag_overrides.Add("OutputFile", OutputFile);

            var flags = Utilities.GetConvertedFlags(GCCToolLinker_Flags, GCCToolLinker_AllFlags, ObjectFiles[0], Flag_overrides, UseWSL);

            return linker.Link(ofiles, OutputFile, flags);
        }


        public bool SetAdditionalOptions(string AdditionalOptions)
        {
            if (!string.IsNullOrWhiteSpace(AdditionalOptions))
                CommandLineArgs.Add(AdditionalOptions);

            return true;
        }

        public bool SetAdditionalDeps(string[] AdditionalDeps)
        {
            if (AdditionalDeps == null)
                return true;
            foreach (var adddep in AdditionalDeps)
            {
                if (Path.GetDirectoryName(adddep) != null)
                    CommandLineArgs.Add("-L\"" + Path.GetDirectoryName(adddep) + "\" -l:\"" + Path.GetFileName(adddep) + "\"");
                else
                    CommandLineArgs.Add("-l:\"" + adddep + "\"");

            }
            return true;
        }

        private const string DefaultLinker = "g++";
        private List<string> CommandLineArgs { get; }

    }
}

