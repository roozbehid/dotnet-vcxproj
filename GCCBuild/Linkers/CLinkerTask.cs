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
        public Boolean GCCBuild_ConvertPath { get; set; }
        public string GCCBuild_ShellApp { get; set; }
        public string GCCBuild_SubSystem { get; set; }
        public string GCCBuild_ConvertPath_mntFolder { get; set; }

        public string OS { get; set; }
        public string Platform { get; set; }
        public string ConfigurationType { get; set; }
        public ITaskItem[] GCCToolLinker_Flags { get; set; }
        public string GCCToolLinker_AllFlags { get; set; }
        public string ProjectFile { get; set; }


        public CLinkerTask()
        {
            
        }


        [Required]
		public string OutputFile { get; set; }

        public override bool Execute()
        {
            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically; this put in constructor causes NRE 

            if (!ObjectFiles.Any())
            {
                return true;
            }

            var lfiles = new List<string>();
            var ofiles = ObjectFiles.Select(x => x.ItemSpec);
            string GCCToolLinkerPathCombined = GCCToolLinkerPath;

            if (OS.Equals("Windows_NT"))
                GCCToolLinkerPathCombined = Utilities.FixAppPath(GCCToolLinkerPathCombined, GCCToolLinkerExe);
            else
                GCCToolLinkerPathCombined = Path.Combine(GCCToolLinkerPath, GCCToolLinkerExe);

            ShellAppConversion shellApp = new ShellAppConversion(GCCBuild_SubSystem, GCCBuild_ShellApp, GCCBuild_ConvertPath, GCCBuild_ConvertPath_mntFolder);

            if (shellApp.convertpath)
                OutputFile = shellApp.ConvertWinPathToWSL(OutputFile);
            else if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));


            // linking

            var linker = new GLD(GCCToolLinkerPathCombined, shellApp);
            Dictionary<string, string> Flag_overrides = new Dictionary<string, string>();
            Flag_overrides.Add("OutputFile", OutputFile);

            var flags = Utilities.GetConvertedFlags(GCCToolLinker_Flags, GCCToolLinker_AllFlags, ObjectFiles[0], Flag_overrides, shellApp);

            return linker.Link(ofiles, OutputFile, flags);
        }

        
        private const string DefaultLinker = "g++";


    }
}

