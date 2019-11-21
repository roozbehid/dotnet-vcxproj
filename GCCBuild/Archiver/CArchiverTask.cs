using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using static GCCBuild.Utilities;
using System.Collections.Concurrent;

namespace GCCBuild
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

        public Boolean GCCBuild_ConvertPath { get; set; }
        public string GCCBuild_ShellApp { get; set; }
        public string GCCBuild_PreRunApp { get; set; }
        public string GCCBuild_SubSystem { get; set; }
        public string GCCBuild_ConvertPath_mntFolder { get; set; }
        public Boolean GCCToolSupportsResponsefile { get; set; }


        public string OS { get; set; }
        public string Platform { get; set; }
        public string ConfigurationType { get; set; }
        public ITaskItem[] GCCToolArchiver_Flags { get; set; }
        public string GCCToolArchiver_AllFlags { get; set; }

        public string ProjectFile { get; set; }
        public string IntPath { get; set; }
        public CArchiverTask()
        {
            CommandLineArgs = new List<string>();
        }


        [Required]
        public string OutputFile { get; set; }

        ConcurrentDictionary<string, FileInfo> fileinfoDict = new ConcurrentDictionary<string, FileInfo>();

        public override bool Execute()
        {
            if (String.IsNullOrEmpty(GCCBuild_ShellApp))
                GCCBuild_ConvertPath = false;
            if (!GCCBuild_ConvertPath)
                GCCBuild_ShellApp = null;
            if (!ObjectFiles.Any())
                return true;



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

            ShellAppConversion shellApp = new ShellAppConversion(GCCBuild_SubSystem, GCCBuild_ShellApp, GCCBuild_PreRunApp, 
                GCCBuild_ConvertPath, GCCBuild_ConvertPath_mntFolder, IntPath);

            if (OS.Equals("Windows_NT") && String.IsNullOrWhiteSpace(shellApp.shellapp))
                GCCToolArchiverCombined = Utilities.FixAppPath(GCCToolArchiverCombined, GCCToolArchiverExe);
            else
                GCCToolArchiverCombined = Path.Combine(GCCToolArchiverPath, GCCToolArchiverExe);

            string OutputFile_Converted = OutputFile;

            if (shellApp.convertpath)
                OutputFile_Converted = shellApp.ConvertWinPathToWSL(OutputFile);
            else if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));


            // archiving - librerian
            Dictionary<string, string> Flag_overrides = new Dictionary<string, string>();
            Flag_overrides.Add("OutputFile", OutputFile_Converted);

            bool needRearchive = true;
            if (File.Exists(OutputFile))
            {
                needRearchive = false;
                FileInfo libInfo = fileinfoDict.GetOrAdd(OutputFile, (x) => new FileInfo(x));
                foreach (var obj in ObjectFiles.Select(x => x.ItemSpec).Concat(new string[] {ProjectFile}) )
                {
                    string depfile = obj;

                    if (shellApp.convertpath)
                        depfile = shellApp.ConvertWSLPathToWin(obj);//here convert back to Windows path

                    FileInfo fi = fileinfoDict.GetOrAdd(depfile, (x) => new FileInfo(x));
                    if (fi.Exists == false || fi.Attributes == FileAttributes.Directory || fi.Attributes == FileAttributes.Device)
                        continue;
                    if (fi.LastWriteTime > libInfo.LastWriteTime)
                    {
                        needRearchive = true;
                        break;
                    }
                }
            }

            var flags = Utilities.GetConvertedFlags(GCCToolArchiver_Flags, GCCToolArchiver_AllFlags, ObjectFiles[0], Flag_overrides, shellApp);
            using (var runWrapper = new RunWrapper(GCCToolArchiverCombined, flags, shellApp, GCCToolSupportsResponsefile))
            {

                bool result = true;
                if (needRearchive)
                {
                    TryDeleteFile(OutputFile);
                    Logger.Instance.LogCommandLine($"{GCCToolArchiverCombined} {flags}");
                    result = runWrapper.RunArchiver(String.IsNullOrEmpty(ObjectFiles[0].GetMetadata("SuppressStartupBanner")) || ObjectFiles[0].GetMetadata("SuppressStartupBanner").Equals("true") ? false : true);
                }


                if (result)
                {
                    string allofiles = String.Join(",", ofiles);
                    if (allofiles.Length > 100)
                        allofiles = allofiles.Substring(0, 100) + "...";
                    if (needRearchive)
                        Logger.Instance.LogMessage($"  ({allofiles}) => {OutputFile_Converted}");
                    else
                        Logger.Instance.LogMessage($"  ({allofiles}) => {OutputFile_Converted} (not archive - already up to date)");
                }

                return result;
            }
        }


        private const string DefaultLinker = "ar";
        private List<string> CommandLineArgs { get; }

    }
}

