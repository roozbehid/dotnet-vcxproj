using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace CCTask
{
	internal static class Utilities
	{
        static Regex flag_regex_array = new Regex(@"@{(.?)}");

        public static String GetConvertedFlags(ITaskItem[] ItemFlags, string flag_string, ITaskItem source, Dictionary<String, String> overrides, bool UseWSL)
        {
            if (String.IsNullOrEmpty(flag_string))
                return "";
            if (source == null)
                return flag_string;

            Regex rg_FlagSet = new Regex("(\\B\\$\\w+)");
            var match = rg_FlagSet.Match(flag_string);
            StringBuilder flagsBuilder = new StringBuilder();
            int movi = 0;

            while (match.Success)
            {
                if (movi < match.Index)
                {
                    flagsBuilder.Append(flag_string.Substring(movi, match.Index - movi));
                    movi += match.Index - movi;
                }

                if (overrides.ContainsKey(match.Value.Substring(1)))
                {
                    flagsBuilder.Append(overrides[match.Value.Substring(1)]);
                }
                else
                    flagsBuilder.Append(GenericFlagsMapper(ItemFlags, source, match.Value.Substring(1), UseWSL));

                movi += match.Length;

                match = match.NextMatch();
            }

            if (movi < flag_string.Length)
                flagsBuilder.Append(flag_string.Substring(movi, flag_string.Length - movi));

            return flagsBuilder.ToString();
        }

        public static String GenericFlagsMapper(ITaskItem[] ItemFlags, ITaskItem source, string ItemSpec, bool UseWSL)
        {
            StringBuilder str = new StringBuilder();

            try
            {
                var allitems = ItemFlags.Where(x => (x.ItemSpec == ItemSpec));
                if (allitems == null)
                    return str.ToString();
                var item = allitems.First();
                if (item.GetMetadata("MappingVariable") != null)
                {
                    var map = item.GetMetadata("MappingVariable");
                    if (String.IsNullOrEmpty(map))
                    {
                        if (!String.IsNullOrEmpty(item.GetMetadata("Flag")))
                        {
                            str.Append(item.GetMetadata("Flag"));
                            str.Append(" ");
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
                                str.Append(item.GetMetadata(metadata));
                                str.Append(" ");
                            }
                            else if (!String.IsNullOrEmpty(item.GetMetadata("OTHER")))
                            {
                                str.Append(item.GetMetadata("OTHER"));
                                str.Append(" ");
                            }
                        }
                        else
                        {
                            var match = flag_regex_array.Match(flag);
                            if (match.Success)
                            {
                                var item_sep = match.Groups[1].Value;
                                var item_arguments = metadata.Split(new String[] { item_sep }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var item_ar in item_arguments)
                                {
                                    if (String.IsNullOrWhiteSpace(Flag_WSLAware) || (!UseWSL) || (!String.IsNullOrWhiteSpace(Flag_WSLAware) && !Flag_WSLAware.ToLower().Equals("true")))
                                        str.Append(flag.Replace(match.Groups[0].Value, item_ar));
                                    else
                                        str.Append(flag.Replace(match.Groups[0].Value, Utilities.ConvertWinPathToWSL(item_ar)));
                                    str.Append(" ");
                                }
                            }
                            else
                            {
                                //just use flags. mistake in their props!
                                str.Append(flag);
                                str.Append(" ");
                            }

                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"You did not specified correct/enough items in GCCToolxxxx_Flags {ex}");
            }

            return str.ToString().TrimEnd();
        }

        const string mntprefix = @"/mnt/";
        public static string ConvertWinPathToWSL(string path)
        {
            try
            {
                StringBuilder FullPath = new StringBuilder(Path.GetFullPath(path));
                FullPath[0] = (FullPath[0].ToString().ToLower())[0];
                return mntprefix + FullPath.ToString().Replace(@":\", @"/").Replace(@"\", @"/");
            }
            catch
            {
                Console.WriteLine("!! ----- error in GCCBuld NTPath -> WSL");
                return path;
            }
        }

        public static string ConvertWSLPathToWin(string path)
        {
            try
            {
                if ((path.Length < 8) || (path.IndexOf(mntprefix) != 0))
                    return path;
                var fileUri = new Uri((path.Substring(mntprefix.Length, path.Length - mntprefix.Length)[0] + ":\\" + path.Substring(mntprefix.Length + 2, path.Length - (mntprefix.Length + 2))).Replace("/", "\\"));
                var referenceUri = new Uri(Directory.GetCurrentDirectory() + "\\");
                return referenceUri.MakeRelativeUri(fileUri).ToString().Replace(@"/", @"\");
            }
            catch
            {
                Console.WriteLine("!! ----- error in GCCBuld WSL -> NTPath");
                return path;
            }
        }

        public static bool IsPathDirectory(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            path = path.Trim();

            if (Directory.Exists(path))
                return true;

            if (File.Exists(path))
                return false;

            // neither file nor directory exists. guess intention

            // if has trailing slash then it's a directory
            if (new[] { "\\", "/" }.Any(x => path.EndsWith(x)))
                return true; // ends with slash

            // if has extension then its a file; directory otherwise
            return string.IsNullOrWhiteSpace(Path.GetExtension(path));
        }
        public static string FixAppPath(string app)
        {
            var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");
            enviromentPath = ".;" + enviromentPath + ";" + Environment.GetEnvironmentVariable("SystemRoot") + @"\sysnative";

            //Console.WriteLine(enviromentPath);
            var paths = enviromentPath.Split(';');
            var pathEXT = System.Environment.GetEnvironmentVariable("PATHEXT").Split(';').ToList();
            if (app.IndexOf(".") > 0)
                pathEXT.Insert(0, "");

            var exePath = (from ext in pathEXT
                           from path in paths
                           where File.Exists(Path.Combine(path, app + ext))
                           select Path.Combine(path, app + ext)).FirstOrDefault();


            if (!String.IsNullOrEmpty(exePath))
                return exePath;
            return app;
        }

        public static bool RunAndGetOutput(string path, string options, out string output, string preLoadApp)
		{
            try
            {
                if (!string.IsNullOrEmpty(preLoadApp))
                {
                    var exePath = FixAppPath(preLoadApp);

                    if (!String.IsNullOrEmpty(exePath))
                    {
                        options = path + " " + options;
                        path = exePath;
                    }
                }

                var startInfo = new ProcessStartInfo(path, options);
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                var process = new Process { StartInfo = startInfo };
                Logger.Instance.LogCommandLine($"{path} {options}");
                process.Start();

                string cv_error = null;
                Thread et = new Thread(() => { cv_error = process.StandardError.ReadToEnd(); });
                et.Start();

                string cv_out = null;
                Thread ot = new Thread(() => { cv_out = process.StandardOutput.ReadToEnd(); });
                ot.Start();

                process.WaitForExit();
                output = cv_error + cv_out;
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error running program. Is your PATH and ENV variables correct? " + ex.ToString(),false);
                output = "FATAL";
                return false;
            }
		}
	}
}

