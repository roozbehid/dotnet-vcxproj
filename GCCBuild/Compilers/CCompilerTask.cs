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
using System.Text;

namespace CCTask
{
    public class CCompilerTask : Task
    {
        [Required]
        public ITaskItem[] Sources { get; set; }

        public string BufferSecurityCheck { get; set; }
        public string CppLanguageStandard { get; set; }
        public string CLanguageStandard { get; set; }
        public string DebugInformationFormat { get; set; }
        public Boolean UseWSL { get; set; }
        public string WSLApp { get; set; }

        public string PrecompiledHeader { get; set; }
        public string PrecompiledHeaderFile { get; set; }
        public string PrecompiledHeaderOutputFile { get; set; }
        public string Verbose { get; set; }
        public string ObjectFileName { get; set; }
        public string PositionIndependentCode { get; set; }
        public string Platform { get; set; }
        public string[] PreprocessorDefinitions { get; set; }

        public string FunctionLevelLinking { get; set; }
        public string GCCToolCompilerExe { get; set; }
        public string GCCToolCompilerPath { get; set; }
        public string GCCToolCompilerArchitecture { get; set; }
        public ITaskItem[] GCCToolCompiler_Flags { get; set; }
        public string GCCToolCompiler_AllFlags { get; set; }
        public string GCCToolCompiler_AllFlagsDependency { get; set; }



        public string OS { get; set; }
        public string ConfigurationType { get; set; }


        [Output]
        public ITaskItem[] ObjectFiles { get; set; }

        public string ObjectFilesDirectory { get; set; }

        public bool Parallel { get; set; }

        public CCompilerTask()
        {
            Parallel = false;
        }

        public override bool Execute()
        {
            if (String.IsNullOrEmpty(GCCToolCompilerPath))
                GCCToolCompilerPath = "";
            if (String.IsNullOrEmpty(WSLApp))
                UseWSL = false;

            if (!UseWSL)
                WSLApp = null;

            compiler = new GCC(string.IsNullOrEmpty(GCCToolCompilerExe) ? DefaultCompiler : Path.Combine(GCCToolCompilerPath, GCCToolCompilerExe), WSLApp);

            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically

            var objectFiles = new List<string>();
            var compilationResult = System.Threading.Tasks.Parallel.ForEach(Sources.Select(x => x), new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Parallel ? -1 : 1 }, (source, loopState) =>
            {
                string objectFile;

                if (!String.IsNullOrEmpty(source.GetMetadata("ObjectFileName")))
                {
                    if (Utilities.IsPathDirectory(source.GetMetadata("ObjectFileName")))
                        objectFile = Path.Combine(source.GetMetadata("ObjectFileName"), Path.GetFileNameWithoutExtension(source.ItemSpec) + ".o");
                    else
                        objectFile = source.GetMetadata("ObjectFileName");
                }
                else
                    objectFile = Path.GetFileNameWithoutExtension(source.ItemSpec) + ".o";

                string sourceFile = source.ItemSpec;
                if (UseWSL)
                {
                    objectFile = Utilities.ConvertWinPathToWSL(objectFile);
                    sourceFile = Utilities.ConvertWinPathToWSL(sourceFile);
                }


                Dictionary<string, string> Flag_overrides = new Dictionary<string, string>();
                Flag_overrides.Add("SourceFile", sourceFile);
                Flag_overrides.Add("OutputFile", objectFile);

                var flags = Utilities.GetConvertedFlags(GCCToolCompiler_Flags, GCCToolCompiler_AllFlags, source, Flag_overrides,UseWSL);
                var flags_dependency = Utilities.GetConvertedFlags(GCCToolCompiler_Flags, GCCToolCompiler_AllFlagsDependency, source, Flag_overrides, UseWSL);


                if (!compiler.Compile(sourceFile, objectFile, flags, flags_dependency))
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

        private ICompiler compiler;

  
        private const string DefaultCompiler = "gcc.exe";
    }
}

