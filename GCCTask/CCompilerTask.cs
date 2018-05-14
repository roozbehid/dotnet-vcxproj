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

        public string OS { get; set; }
        public string ConfigurationType { get; set; }


        [Output]
		public ITaskItem[] ObjectFiles { get; set; }

		public string ObjectFilesDirectory { get; set; }

		public bool Parallel { get; set; }

		public CCompilerTask()
		{
			regex = new Regex(@"\.cpp$");
			Parallel = true;
            CommandLineArgs = new List<string>();
		}

        public override bool Execute()
        {
            if (String.IsNullOrEmpty(GCCToolCompilerPath))
                GCCToolCompilerPath = "";
            if (!UseWSL)
                WSLApp = null;

            compiler = new GCC(string.IsNullOrEmpty(GCCToolCompilerExe) ? DefaultCompiler : Path.Combine(GCCToolCompilerPath,GCCToolCompilerExe), WSLApp);

            Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically

            SetWarningsLevel(WarningLevel);
            SetOptimization(Optimization);
            SetPreprocessorDefinitions(PreprocessorDefinitions);
            SetAdditionalOptions(AdditionalOptions);
            SetAdditionalIncludeDirectories(AdditionalIncludeDirectories);
            SetCompileAs(CompileAs);

            if (ConfigurationType == "DynamicLibrary")
                CommandLineArgs.Add("-fPIC");
            if (Platform == "x64")
                CommandLineArgs.Add("-m64");

            if (ConformanceMode)
                CommandLineArgs.Add("-fpermissive");

            var flags = (CommandLineArgs != null && CommandLineArgs.Any()) ? CommandLineArgs.Aggregate(string.Empty, (curr, next) => string.Format("{0} {1}", curr, next)) : string.Empty;

            var objectFiles = new List<string>();
            var compilationResult = System.Threading.Tasks.Parallel.ForEach(Sources.Select(x => x), new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Parallel ? -1 : 1 }, (source, loopState) =>
            {
                string tmpObjectFilesDirectory;
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

		private readonly Regex regex;
		private ICompiler compiler;
        private List<string> CommandLineArgs { get; }
        public bool SetCompileAs(string CompileAs)
        {
            if (CompileAs == "CompileAsCpp") 
                CommandLineArgs.Add("-x c++");
            if (CompileAs == "CompileAsC") 
                CommandLineArgs.Add("-x c");
            return true;
        }

        public bool SetAdditionalOptions(string AdditionalOptions)
        {
            if (!string.IsNullOrWhiteSpace(AdditionalOptions))
                CommandLineArgs.Add(AdditionalOptions);


            return true;
        }
        public bool SetAdditionalIncludeDirectories(string[] AdditionalIncludeDirectories)
        {
            if (AdditionalIncludeDirectories == null)
                return true;
            foreach (var addInc in AdditionalIncludeDirectories)
            {
                CommandLineArgs.Add("-I \"" + addInc + "\"");
            }
            return true;
        }

        public bool SetPreprocessorDefinitions(string[] PreprocessorDefinitions)
        {
            if (PreprocessorDefinitions == null)
                return true;
            foreach (var prep in PreprocessorDefinitions)
            {
                CommandLineArgs.Add("-D"+prep);
            }
            return true;
        }

        public bool SetOptimization(string Optimization)
        {
            if (!string.IsNullOrWhiteSpace(Optimization))
            {
                switch (Optimization)
                {
                    case "Disabled":
                        break;
                    case "MinSpace":
                        CommandLineArgs.Add("-Os");
                        break;
                    case "MaxSpeed":
                        CommandLineArgs.Add("-O2");
                        break;
                    case "Full":
                        CommandLineArgs.Add("-O3");
                        break;
                }
            }
            return true;
        }


                public bool SetWarningsLevel(string WarningLevel)
        {
            if (!string.IsNullOrWhiteSpace(WarningLevel))
            {
                switch (WarningLevel)
                {
                    case "TurnOffAllWarnings ":
                        CommandLineArgs.Add("-w");
                        break;
                    case "Level1":
                        CommandLineArgs.Add("-Wall");
                        CommandLineArgs.Add("-Wno-comment");
                        CommandLineArgs.Add("-Wno-parentheses");
                        CommandLineArgs.Add("-Wno-missing-braces");
                        CommandLineArgs.Add("-Wno-write-strings");
                        CommandLineArgs.Add("-Wno-unknown-pragmas");
                        CommandLineArgs.Add("-Wno-attributes");
                        CommandLineArgs.Add("-Wformat=0");

                        break;
                    case "Level2":
                        CommandLineArgs.Add("-Wall");
                        CommandLineArgs.Add("-Wno-comment");
                        CommandLineArgs.Add("-Wno-parentheses");
                        CommandLineArgs.Add("-Wno-missing-braces");
                        CommandLineArgs.Add("-Wno-write-strings");
                        CommandLineArgs.Add("-Wno-unknown-pragmas");
                        CommandLineArgs.Add("-Wno-attributes");
                        CommandLineArgs.Add("-Wformat=0");

                        break;
                    case "Level3":
                        CommandLineArgs.Add("-Wall");
                        CommandLineArgs.Add("-Wno-comment");
                        CommandLineArgs.Add("-Wno-parentheses");
                        CommandLineArgs.Add("-Wno-missing-braces");
                        CommandLineArgs.Add("-Wno-write-strings");
                        CommandLineArgs.Add("-Wno-unknown-pragmas");
                        CommandLineArgs.Add("-Wno-attributes");
                        CommandLineArgs.Add("-Wformat=0");

                        break;
                    case "Level4":
                        CommandLineArgs.Add("-Wall");
                        CommandLineArgs.Add("-Wnull-dereference");
                        CommandLineArgs.Add("-Wformat=1");
                        CommandLineArgs.Add("-Wduplicated-cond");
                        CommandLineArgs.Add("-Wduplicated-branches");
                        break;
                    case "EnableAllWarnings":
                        CommandLineArgs.Add("-Wall");
                        CommandLineArgs.Add("-Wextra");
                        CommandLineArgs.Add("-Wduplicated-cond");
                        CommandLineArgs.Add("-Wduplicated-branches");
                        CommandLineArgs.Add("-Wlogical-op");
                        CommandLineArgs.Add("-Wrestrict");
                        CommandLineArgs.Add("-Wnull-dereference");
                        CommandLineArgs.Add("-Wold-style-cast");
                        CommandLineArgs.Add("-Wuseless-cast");
                        CommandLineArgs.Add("-Wjump-misses-init");
                        CommandLineArgs.Add("-Wdouble-promotion");
                        CommandLineArgs.Add("-Wshadow");
                        CommandLineArgs.Add("-Wformat=2");
                        break;
                }

            }

            return true;
        }

        private const string DefaultCompiler = "gcc.exe";
	}
}

