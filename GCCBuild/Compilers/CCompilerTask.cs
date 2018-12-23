/**
 * CCompilerTask
 * 
 * Copyright 2018 Roozbeh <roozbeh@gmail.com>
 * Copyright 2012 Konrad Kruczyński <konrad.kruczynski@gmail.com>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:

 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Linq;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using static GCCBuild.Utilities;

namespace GCCBuild
{

    public class CCompilerTask : Task
    {
        [Required]
        public ITaskItem[] Sources { get; set; }

        public String GCCBuild_SubSystem { get; set; }
        public string GCCBuild_ShellApp { get; set; }
        public Boolean GCCBuild_ConvertPath { get; set; }
        public string GCCBuild_ConvertPath_mntFolder { get; set; }

        public string GCCToolCompilerExe { get; set; }
        public string GCCToolCompilerPath { get; set; }
        public string GCCToolCompilerArchitecture { get; set; }
        public ITaskItem[] GCCToolCompiler_Flags { get; set; }
        public string GCCToolCompiler_AllFlags { get; set; }
        public string GCCToolCompiler_AllFlagsDependency { get; set; }

        public string IntPath { get; set; }
        

        public string OS { get; set; }
        public string ConfigurationType { get; set; }
        public string Platform { get; set; }
        public string ProjectFile { get; set; }

        [Output]
        public ITaskItem[] ObjectFiles { get; set; }

        public bool Parallel { get; set; }

        public CCompilerTask()
        {
#if RELEASE
            Parallel = true;
#else
            Parallel = false;
#endif
        }

        string GCCToolCompilerPathCombined;
        ShellAppConversion shellApp;

        ConcurrentDictionary<string, List<String>> dependencyDict = new ConcurrentDictionary<string, List<String>>();
        ConcurrentDictionary<string, FileInfo> fileinfoDict = new ConcurrentDictionary<string, FileInfo>();

        public override bool Execute()
        {
            if (String.IsNullOrEmpty(GCCToolCompilerPath))
                GCCToolCompilerPath = "";
            if (String.IsNullOrEmpty(IntPath))
                IntPath = "";

            if (!Sources.Any())
                return true;

            GCCToolCompilerPathCombined = GCCToolCompilerPath;

            shellApp = new ShellAppConversion(GCCBuild_SubSystem, GCCBuild_ShellApp, GCCBuild_ConvertPath, GCCBuild_ConvertPath_mntFolder);

            if (OS.Equals("Windows_NT") && String.IsNullOrWhiteSpace(shellApp.shellapp))
                GCCToolCompilerPathCombined = FixAppPath(GCCToolCompilerPathCombined, GCCToolCompilerExe);
            else
                GCCToolCompilerPathCombined = Path.Combine(GCCToolCompilerPath, GCCToolCompilerExe);


            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically

            // load or create tracker file
            string trackerFile = Path.Combine(IntPath, Path.GetFileNameWithoutExtension(ProjectFile) + ".tracker");
            try
            {
                
                if (File.Exists(trackerFile))
                {
                    XElement rootElement = XElement.Parse(File.ReadAllText(trackerFile));
                    foreach (var el in rootElement.Elements())
                    {
                        dependencyDict.TryAdd(el.Attribute("Object").Value, el.Value.Split(';').ToList());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage($"Accessing .tracker file caused an exception! {ex}");
                //just ignore it is ok!
            }

            var objectFiles = new List<string>();
            var compilationResult = System.Threading.Tasks.Parallel.ForEach(Sources.Select(x => x), 
                new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Parallel ? -1 : 1 }, (source, loopState) =>
            {
                string objectFile;

                if (!Compile(source, out objectFile))
                {
                    loopState.Break();
                }

                lock (objectFiles)
                {
                    objectFiles.Add(objectFile);
                }
            });

            if (dependencyDict.Count> 0)
            {
                try
                {
                    XElement el = new XElement("root",
                    dependencyDict.Select(kv =>
                    {
                        var x = new XElement("File", String.Join(";", kv.Value));
                        x.Add(new XAttribute("Object", kv.Key));
                        return x;
                    }));
                    File.WriteAllText(trackerFile, el.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage($"Writing to .tracker file caused an exception! {ex}");
                }
            }

            if (compilationResult.LowestBreakIteration != null)
            {
                return false;
            }

            ObjectFiles = objectFiles.Any() ? objectFiles.Select(x => new TaskItem(x)).ToArray() : new TaskItem[0];

            return true;

        }


        public bool Compile(ITaskItem source,out string objectFile)
        {
            //Console.WriteLine($"  {source.ItemSpec}");
            Logger.Instance.LogMessage($"  {source.ItemSpec}");
            if (!String.IsNullOrEmpty(source.GetMetadata("ObjectFileName")))
            { //ObjectFileName is actually a folder name which is usaully $(IntDir) or $(IntDir)/%(RelativeDir)/
                if (IsPathDirectory(source.GetMetadata("ObjectFileName")))
                    objectFile = Path.Combine(source.GetMetadata("ObjectFileName"), Path.GetFileNameWithoutExtension(source.ItemSpec) + ".o");
                else
                    objectFile = source.GetMetadata("ObjectFileName");
            }
            else
                objectFile = Path.GetFileNameWithoutExtension(source.ItemSpec) + ".o";

            string sourceFile = source.ItemSpec;
            string projectfile_name = ProjectFile;

            string objectFile_converted = objectFile;
            string sourceFile_converted = sourceFile;
            string projectfile_name_converted = projectfile_name;

            if (shellApp.convertpath)
            {
                objectFile_converted = shellApp.ConvertWinPathToWSL(objectFile);
                sourceFile_converted = shellApp.ConvertWinPathToWSL(sourceFile);
                projectfile_name_converted = shellApp.ConvertWinPathToWSL(projectfile_name);
            }
            else
            {
                //here just shotens path it will help with huge projects
                objectFile_converted = shellApp.MakeRelative(Path.GetFullPath(objectFile), Environment.CurrentDirectory + Path.DirectorySeparatorChar);
            }

            Dictionary<string, string> Flag_overrides = new Dictionary<string, string>();
            Flag_overrides.Add("SourceFile", sourceFile_converted);
            Flag_overrides.Add("OutputFile", objectFile_converted);

            var flags = GetConvertedFlags(GCCToolCompiler_Flags, GCCToolCompiler_AllFlags, source, Flag_overrides, shellApp);
            var flags_dep = GetConvertedFlags(GCCToolCompiler_Flags, GCCToolCompiler_AllFlagsDependency, source, Flag_overrides, shellApp);

            // let's get all dependencies
            string gccOutput;

            if (Path.GetDirectoryName(objectFile) != "")
                Directory.CreateDirectory(Path.GetDirectoryName(objectFile));

            // This part is to get all dependencies and so know what files to recompile!
            bool needRecompile = true;
            if (!String.IsNullOrEmpty(flags_dep))
                try
                {
                    FileInfo sourceInfo = fileinfoDict.GetOrAdd(sourceFile, (x) => new FileInfo(x));
                    IEnumerable<string> dependencies;

                    if (dependencyDict.ContainsKey(objectFile) && File.Exists(objectFile) && sourceInfo.LastWriteTime < fileinfoDict.GetOrAdd(objectFile, (x) => new FileInfo(x)).LastWriteTime)
                    {
                        // You dont need to run dependency extraction!
                        // you still need to go through all dependency chains and check files!
                        dependencies = dependencyDict[objectFile];
                    }
                    else
                    {
                        // run dependency extraction if there is an object file there....if not obviously you have to recompile!
                        if (!RunAndGetOutput(GCCToolCompilerPathCombined, flags_dep, out gccOutput, shellApp,
                                 String.IsNullOrEmpty(source.GetMetadata("SuppressStartupBanner")) || source.GetMetadata("SuppressStartupBanner").Equals("true") ? false : true
                            ))
                        {
                            if (gccOutput == "FATAL")
                                return false;
                            Logger.Instance.LogDecide(gccOutput, shellApp);
                            ///return false;
                        }
                        dependencies = ParseGccMmOutput(gccOutput).Union(new[] { sourceFile, ProjectFile });
                        dependencyDict.AddOrUpdate(objectFile, dependencies.ToList(), (x,y) => dependencies.ToList() );
                    }

                    //if there is no object file then offcourse you need to recompile! no need to get into dependency checking
                    if (File.Exists(objectFile))
                    {
                        needRecompile = false;
                        FileInfo objInfo = fileinfoDict.GetOrAdd(objectFile, (x) => new FileInfo(x));

                        foreach (var dep in dependencies)
                        {
                            string depfile = dep;

                            if (shellApp.convertpath)
                                depfile = shellApp.ConvertWSLPathToWin(dep);//here use original! convert back to windows

                            FileInfo fi = fileinfoDict.GetOrAdd(depfile, (x) => new FileInfo(x));
                            if (fi.Exists == false || fi.Attributes == FileAttributes.Directory || fi.Attributes == FileAttributes.Device)
                                continue;
                            if (fi.LastWriteTime > objInfo.LastWriteTime)
                            {
                                needRecompile = true;
                                break;
                            }

                        }
                    }

                }
                catch
                {
                    needRecompile = true;
                    Logger.Instance.LogError("Internal error while trying to get dependencies from gcc", null);
                }

            bool runCompileResult = false;
            if (needRecompile)
            {
                var runWrapper = new RunWrapper(GCCToolCompilerPathCombined, flags, shellApp);
                runCompileResult = runWrapper.RunCompiler(String.IsNullOrEmpty(source.GetMetadata("SuppressStartupBanner")) || source.GetMetadata("SuppressStartupBanner").Equals("true") ? false : true);
                if (runCompileResult)
                    Logger.Instance.LogMessage($"  {source.ItemSpec} => {objectFile_converted}");
            }
            else
            {
                Logger.Instance.LogMessage($"  {source.ItemSpec} => {objectFile_converted} (not compiled - already up to date)");
                runCompileResult = true;
            }

            return runCompileResult;

        }

        private static IEnumerable<string> ParseGccMmOutput(string gccOutput)
        {
            string[] results = gccOutput.Split(new char[] { ' ', ':', '\n', '"', '\'' },StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in results)
            {
                if (item.Equals("\\"))
                    continue;
                if (item.EndsWith(".o"))
                    continue;

                if ((item.IndexOfAny(Path.GetInvalidPathChars()) >= 0) || (Path.GetFileName(item).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                    continue;

                yield return item;

            }
        }


        private const string DefaultCompiler = "gcc.exe";
    }
}

