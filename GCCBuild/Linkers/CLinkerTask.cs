using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;

namespace GCCBuild
{
	public class CLinkerTask : Task
	{
		[Required]
		public ITaskItem[] ObjectFiles { get; set; }

		public ITaskItem[] Libraries { get; set; }

        public Boolean GenerateDebugInformation { get; set; }
        public string GCCToolLinkerExe { get; set; }
        public string GCCToolLinkerPath { get; set; }
        public string GCCToolLinkerArchitecture { get; set; }
        public Boolean GCCBuild_ConvertPath { get; set; }
        public Boolean GCCToolSupportsResponsefile { get; set; }
        public string GCCBuild_ShellApp { get; set; }
        public string GCCBuild_PreRunApp { get; set; }
        public string GCCBuild_SubSystem { get; set; }
        public string GCCBuild_ConvertPath_mntFolder { get; set; }

        public string OS { get; set; }
        public string Platform { get; set; }
        public string ConfigurationType { get; set; }
        public ITaskItem[] GCCToolLinker_Flags { get; set; }
        public string GCCToolLinker_AllFlags { get; set; }
        public string ProjectFile { get; set; }
        public string IntPath { get; set; }


        public CLinkerTask()
        {
            
        }


        [Required]
		public string OutputFile { get; set; }
        ShellAppConversion shellApp;
        string GCCToolLinkerPathCombined;

        public override bool Execute()
        {
            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically; this put in constructor causes NRE 

            if (!ObjectFiles.Any())
            {
                return true;
            }

            var lfiles = new List<string>();
            var ofiles = ObjectFiles.Select(x => x.ItemSpec);

            if (String.IsNullOrEmpty(GCCToolLinkerPath))
                GCCToolLinkerPath = "";
            GCCToolLinkerPathCombined = GCCToolLinkerPath;

            shellApp = new ShellAppConversion(GCCBuild_SubSystem, GCCBuild_ShellApp, GCCBuild_PreRunApp, 
                GCCBuild_ConvertPath, GCCBuild_ConvertPath_mntFolder, IntPath);

            if (OS.Equals("Windows_NT") && String.IsNullOrWhiteSpace(shellApp.shellapp))
                GCCToolLinkerPathCombined = Utilities.FixAppPath(GCCToolLinkerPathCombined, GCCToolLinkerExe);
            else
                GCCToolLinkerPathCombined = Path.Combine(GCCToolLinkerPath, GCCToolLinkerExe);

            string OutputFile_Converted = OutputFile;

            if (shellApp.convertpath)
                OutputFile_Converted = shellApp.ConvertWinPathToWSL(OutputFile);

            else if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));


            // linking
            Dictionary<string, string> Flag_overrides = new Dictionary<string, string>();
            Flag_overrides.Add("OutputFile", OutputFile_Converted);

            var flags = Utilities.GetConvertedFlags(GCCToolLinker_Flags, GCCToolLinker_AllFlags, ObjectFiles[0], Flag_overrides, shellApp);

            Logger.Instance.LogCommandLine($"{GCCToolLinkerPathCombined} {flags}");

            using (var runWrapper = new RunWrapper(GCCToolLinkerPathCombined, flags, shellApp, GCCToolSupportsResponsefile))
            {
                bool result = runWrapper.RunLinker(String.IsNullOrEmpty(ObjectFiles[0].GetMetadata("SuppressStartupBanner")) || ObjectFiles[0].GetMetadata("SuppressStartupBanner").Equals("true") ? false : true);
                if (result)
                {
                    string allofiles = String.Join(",", ofiles);
                    if (allofiles.Length > 100)
                        allofiles = allofiles.Substring(0, 100) + "...";
                    Logger.Instance.LogMessage($"  ({allofiles}) => {OutputFile_Converted}");
                }

                return result;
            }
        }


        private const string DefaultLinker = "g++";


    }
}

