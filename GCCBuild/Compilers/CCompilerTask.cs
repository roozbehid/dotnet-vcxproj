/**
 * CCTask
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
using System.Text.RegularExpressions;
using CCTask.Compilers;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CCTask
{
    public class CCompilerTask : Task
    {
        [Required]
        public ITaskItem[] Sources { get; set; }

        public string[] AdditionalIncludeDirectories { get; set; }
        public string AdditionalOptions { get; set; }
        public string BufferSecurityCheck { get; set; }
        public string CppLanguageStandard { get; set; }
        public string CLanguageStandard { get; set; }
        public string CompileAs { get; set; }
        public Boolean ConformanceMode { get; set; }
        public string DebugInformationFormat { get; set; }
        public Boolean UseWSL { get; set; }
        public string WSLApp { get; set; }

        public string PrecompiledHeader { get; set; }
        public string PrecompiledHeaderFile { get; set; }
        public string PrecompiledHeaderOutputFile { get; set; }
        public string Verbose { get; set; }
        public string WarningLevel { get; set; }
        public string Optimization { get; set; }
        public string ObjectFileName { get; set; }
        public string PositionIndependentCode { get; set; }
        public string Platform { get; set; }
        public string[] PreprocessorDefinitions { get; set; }

        public string FunctionLevelLinking { get; set; }
        public string GCCToolCompilerExe { get; set; }
        public string GCCToolCompilerPath { get; set; }
        public string GCCToolCompilerArchitecture { get; set; }
        public ITaskItem[] GCCToolCompiler_Flags { get; set; }
        public string GCCToolCompiler_Flags_Flatten { get; set; }
        

        public string OS { get; set; }
        public string ConfigurationType { get; set; }


        [Output]
        public ITaskItem[] ObjectFiles { get; set; }

        public string ObjectFilesDirectory { get; set; }

        public bool Parallel { get; set; }

        public CCompilerTask()
        {
            flag_regex_array = new Regex(@"@{(.?)}");
            Parallel = true;
        }

        public override bool Execute()
        {
            if (String.IsNullOrEmpty(GCCToolCompilerPath))
                GCCToolCompilerPath = "";
            if (!UseWSL)
                WSLApp = null;

            compiler = new GCC(string.IsNullOrEmpty(GCCToolCompilerExe) ? DefaultCompiler : Path.Combine(GCCToolCompilerPath, GCCToolCompilerExe), WSLApp);

            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically

            var objectFiles = new List<string>();
            var compilationResult = System.Threading.Tasks.Parallel.ForEach(Sources.Select(x => x), new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Parallel ? -1 : 1 }, (source, loopState) =>
            {
                string objectFile;
                List<string> CommandLineArgs = new List<string>();

                GenericFlagsMapper(CommandLineArgs, source, "IncludeDirs");
                GenericFlagsMapper(CommandLineArgs, source, "AdditionalOptions");
                GenericFlagsMapper(CommandLineArgs, source, "Preprocessor");
                GenericFlagsMapper(CommandLineArgs, source, "Warnings");
                GenericFlagsMapper(CommandLineArgs, source, "Optimization");
                GenericFlagsMapper(CommandLineArgs, source, "Conformance");
                GenericFlagsMapper(CommandLineArgs, source, "CompileAs");
                GenericFlagsMapper(CommandLineArgs, source, "DebugInfo");
                GenericFlagsMapper(CommandLineArgs, source, "ConfigurationType");
                GenericFlagsMapper(CommandLineArgs, source, "Platform");

                var flags = (CommandLineArgs != null && CommandLineArgs.Any()) ? CommandLineArgs.Aggregate(string.Empty, (curr, next) => string.Format("{0} {1}", curr, next)) : string.Empty;

                if (!String.IsNullOrEmpty(source.GetMetadata("ObjectFileName")))
                {
                    if (Utilities.IsPathDirectory(source.GetMetadata("ObjectFileName")))
                        objectFile = Path.Combine(source.GetMetadata("ObjectFileName"), Path.GetFileNameWithoutExtension(source.ItemSpec) + ".o");
                    else
                        objectFile = source.GetMetadata("ObjectFileName");
                }
                else
                    objectFile = Path.GetFileNameWithoutExtension(source.ItemSpec) + ".o";

                if (!compiler.Compile(source.ItemSpec, objectFile, flags))
                {
                    loopState.Break();
                }

                lock (objectFiles)
                {
                    objectFiles.Add(objectFile);
                }
            });
            if (compilationResult.LowestBreakIteration != null)
            {
                return false;
            }

            ObjectFiles = objectFiles.Any() ? objectFiles.Select(x => new TaskItem(x)).ToArray() : new TaskItem[0];

            return true;

        }

        private readonly Regex flag_regex_array;
        private ICompiler compiler;

        public void GenericFlagsMapper(List<String> CommandLineArgs, ITaskItem source,string ItemSpec)
        {
            try
            {
                var allitems = GCCToolCompiler_Flags.Where(x => (x.ItemSpec == ItemSpec));
                if (allitems == null)
                    return;
                var item = allitems.First();
                if (item.GetMetadata("MappingVariable") != null)
                {
                    var map = item.GetMetadata("MappingVariable");
                    if (String.IsNullOrEmpty(map))
                    {
                        if (String.IsNullOrEmpty(item.GetMetadata("Flag")))
                        {
                            CommandLineArgs.Add(item.GetMetadata("Flag"));
                        }
                    }
                    else
                    {
                        var metadata = source.GetMetadata(map);
                        // check if you have flags too. if so then
                        var flag = item.GetMetadata("flag");
                        var Flag_WSLAware = item.GetMetadata("Flag_WSLAware");
                        if (String.IsNullOrEmpty(flag))
                        {
                            if (String.IsNullOrEmpty(metadata))
                                metadata = "IsNullOrEmpty";

                            if (!String.IsNullOrEmpty(item.GetMetadata(metadata)))
                            {
                                CommandLineArgs.Add(item.GetMetadata(metadata));
                            }
                            else if (!String.IsNullOrEmpty(item.GetMetadata("OTHER")))
                            {
                                CommandLineArgs.Add(item.GetMetadata("OTHER"));
                            }
                        }
                        else
                        {
                            var match = flag_regex_array.Match(flag);
                            if (match.Success)
                            {
                                var item_sep = match.Groups[1].Value;
                                var item_arguments = metadata.Split(new String[] { item_sep },StringSplitOptions.RemoveEmptyEntries);
                                foreach (var item_ar in item_arguments)
                                {
                                    if (String.IsNullOrWhiteSpace(Flag_WSLAware) || (!UseWSL) || (!String.IsNullOrWhiteSpace(Flag_WSLAware) && !Flag_WSLAware.ToLower().Equals("true")))
                                        CommandLineArgs.Add(flag.Replace(match.Groups[0].Value, item_ar));
                                    else
                                        CommandLineArgs.Add(flag.Replace(match.Groups[0].Value, Utilities.ConvertWinPathToWSL(item_ar)));
                                }
                            }
                            else
                            {
                                //just use flags. mistake in their props!
                                CommandLineArgs.Add(flag);
                            }
                            
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"You did not specified correct/enough items in GCCToolCompiler_Flags {ex}");
            }
        }

        private const string DefaultCompiler = "gcc.exe";
    }
}

