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
        public string ConfigurationType { get; set; }

        public CLinkerTask()
        {
            CommandLineArgs = new List<string>();
        }


        [Required]
		public string OutputFile { get; set; }

		public override bool Execute()
		{

            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically; this put in constructor causes NRE 
            Logger.Instance.LogMessage("LinkerTask output: {0}", OutputFile);

            if (!ObjectFiles.Any())
			{
				return true;
			}

			var lfiles = new List<string>();
			var ofiles = ObjectFiles.Select(x => x.ItemSpec);

            if (Libraries != null)
			{
				foreach(var library in Libraries.Select(x => x.ItemSpec))
				{
					if(File.Exists(library))
					{
						var directory = Path.GetDirectoryName(library);
						var fileName = Path.GetFileName(library);

						lfiles.Add(library);
                        CommandLineArgs.Add(string.Format(" -L{0} -l:{1}", directory, fileName));
					}
					else
					{
                        CommandLineArgs.Add(string.Format("-l{0}", library));
					}
				}
			}

            if (String.IsNullOrEmpty(GCCToolLinkerPath))
                GCCToolLinkerPath = "";

            if (ConfigurationType == "DynamicLibrary")
            {
                CommandLineArgs.Add("-shared");
                CommandLineArgs.Add("-Wl,-z,defs");//so no unresolved symbol ends up in .so file
            }

            SetAdditionalDeps(AdditionalDependencies);
            SetAdditionalOptions(AdditionalOptions);
            SetGenerateDebugInformation(GenerateDebugInformation);

            // linking
            var linker = new GLD(string.IsNullOrEmpty(GCCToolLinkerPath) ? DefaultLinker : Path.Combine(GCCToolLinkerPath, GCCToolLinkerExe), WSLApp);
            var flags = (CommandLineArgs != null && CommandLineArgs.Any()) ? CommandLineArgs.Aggregate(string.Empty, (curr, next) => string.Format("{0} {1}", curr, next)) : string.Empty;

            return linker.Link(ofiles, OutputFile, flags);
		}

        public bool SetGenerateDebugInformation(Boolean GenerateDebugInformation)
        {
            if (GenerateDebugInformation)
            {
                CommandLineArgs.Add("-g");
            }
            return true;
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

