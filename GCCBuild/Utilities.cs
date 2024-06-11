using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace GCCBuild
{
    public class ShellAppConversion
    {
        public ShellAppConversion(string subsystem, string shellapp, string prerunapp, Boolean convertpath, string convertpath_mntFolder, string tmpfolder)
        {
            this.subsystem = subsystem;
            this.shellapp = shellapp;
            this.convertpath = convertpath;
            this.convertpath_mntFolder = convertpath_mntFolder;
            this.prerunapp = prerunapp;
            this.tmpfolder = tmpfolder;
        }

        public string subsystem;
        public string shellapp;
        public Boolean convertpath;
        public string convertpath_mntFolder;
        public string prerunapp;
        public string tmpfolder;

        public string ConvertWinPathToWSL(string path)
        {
            try
            {
                StringBuilder FullPath = new StringBuilder(Path.GetFullPath(path));
                FullPath[0] = (FullPath[0].ToString().ToLower())[0];
                return convertpath_mntFolder + FullPath.ToString().Replace(@":\", @"/").Replace(@"\", @"/");
            }
            catch
            {
                Logger.Instance.LogMessage("!! ----- error in GCCBuld NTPath -> WSL");
                return path;
            }
        }

        public string ConvertWSLPathToWin(string path)
        {
            try
            {
                if ((path.Length < 8) || (path.IndexOf(convertpath_mntFolder) != 0) )
                    return path;
                var fileUri = new Uri((path.Substring(convertpath_mntFolder.Length, path.Length - convertpath_mntFolder.Length)[0] + ":\\" + path.Substring(convertpath_mntFolder.Length + 2, path.Length - (convertpath_mntFolder.Length + 2))).Replace("/", "\\"));
                var referenceUri = new Uri(Directory.GetCurrentDirectory() + "\\");
                return referenceUri.MakeRelativeUri(fileUri).ToString().Replace(@"/", @"\");
            }
            catch
            {
                Logger.Instance.LogMessage("!! ----- error in GCCBuld WSL -> NTPath");
                return path;
            }
        }
    }

	internal static class Utilities
	{
        static Regex flag_regex_array = new Regex(@"@{(.?)}");

        public static string MakeRelative(string filePath, string referencePath)
        {
            if (filePath.StartsWith("."))
                filePath = Path.GetFullPath(filePath);
            var fileUri = new Uri(filePath);
            var referenceUri = new Uri(referencePath);
            return referenceUri.MakeRelativeUri(fileUri).ToString();
        }

        public static bool isLinux()
        {
            int platform = (int)Environment.OSVersion.Platform;
            if (platform == 4 || platform == 128 || platform == 6)
                return true;
            else
                return false;
        }
        public static void TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage($"!! ----- error in TryDeleteFile {ex}");
            }
        }
        public static String GetConvertedFlags(ITaskItem[] ItemFlags, string flag_string, ITaskItem source, Dictionary<String, String> overrides, ShellAppConversion shellApp)
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
                    flagsBuilder.Append(GenericFlagsMapper(ItemFlags, source, match.Value.Substring(1), shellApp));

                movi += match.Length;

                match = match.NextMatch();
            }

            if (movi < flag_string.Length)
                flagsBuilder.Append(flag_string.Substring(movi, flag_string.Length - movi));

            return flagsBuilder.ToString();
        }

        public static String GenericFlagsMapper(ITaskItem[] ItemFlags, ITaskItem source, string ItemSpec, ShellAppConversion shellApp)
        {
            StringBuilder str = new StringBuilder();

            try
            {
                var allitems = ItemFlags.Where(x => (x.ItemSpec == ItemSpec));
                if (!allitems.Any())
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
                                    string item_ar_fixed = item_ar;
                                    //a hacky fix for issue #6, gcc.exe -I "d:\" will consider \" as escaped string!
                                    if (!String.IsNullOrWhiteSpace(Flag_WSLAware) && Flag_WSLAware.ToLower().Equals("true") && item_ar_fixed.EndsWith("\\"))
                                        item_ar_fixed = item_ar_fixed.Substring(0, item_ar_fixed.Length - 1);

                                    if (String.IsNullOrWhiteSpace(Flag_WSLAware) || (!shellApp.convertpath) || (!String.IsNullOrWhiteSpace(Flag_WSLAware) && !Flag_WSLAware.ToLower().Equals("true")))
                                        str.Append(flag.Replace(match.Groups[0].Value, item_ar_fixed));
                                    else
                                    {
                                        str.Append(flag.Replace(match.Groups[0].Value, shellApp.ConvertWinPathToWSL(item_ar_fixed)));
                                    }
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
                Logger.Instance.LogMessage($"You did not specified correct/enough items in GCCToolxxxx_Flags {ex}");
            }

            return str.ToString().TrimEnd();
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

        public static String GetEnviromentPath()
        {
            char seperator = ';';
            var enviromentPath = Environment.GetEnvironmentVariable("PATH");
            if (!isLinux())
            {
                enviromentPath = $".{seperator}" + enviromentPath + seperator + Environment.GetEnvironmentVariable("SystemRoot") + @"\sysnative";
            }
            else
            {
                seperator = ':';
                if (String.IsNullOrEmpty(enviromentPath) || enviromentPath.Length < 3)
                {
                    enviromentPath = "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/snap/bin";
                    Environment.SetEnvironmentVariable("PATH", enviromentPath);
                }
            }

            return enviromentPath;
        }
        /// <summary>
        /// if you provide thepath it will only search current directory and the path for correct executable
        /// if you proide null or emprty string it will go through all the paths
        /// It will eventually find what extension is correct for your app 
        /// </summary>
        /// <param name="thepath"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        /// This is only called in Windows OS so no need for checking for linux stuff!
        public static string FixAppPath(string thepath, string app)
        {
            char seperator = isLinux() ? ':' : ';';
            var enviromentPath = GetEnviromentPath();

            if (!String.IsNullOrEmpty(thepath))
                enviromentPath = $".{seperator}" + thepath;

            var paths = enviromentPath.Split(seperator);

            List<string> pathEXT;
            
            if (!isLinux())
                pathEXT = System.Environment.GetEnvironmentVariable("PATHEXT").Split(seperator).ToList();
            else
                pathEXT = new List<string> { "" };

            if ((app.IndexOf(".") > 0))
                pathEXT.Insert(0, "");


            //var exePath = (from ext in pathEXT
            //               from path in paths
            //               where File.Exists(Path.Combine(path, app + ext))
            //               select Path.Combine(path, app + ext)).FirstOrDefault();

            string exePath = null;
            foreach (var ext in pathEXT)
            {
                foreach (var path in paths)
                {
                    string fullPath = Path.Combine(path, app + ext);
                    bool exists = File.Exists(fullPath);
                    Logger.Instance.LogMessage($"FixAppPath : Checking path: {fullPath} - Exists: {exists}");
                    if (exists)
                    {
                        exePath = fullPath;
                        break;
                    }
                }
                if (exePath != null)
                    break;
            }

            string result;
            if (!String.IsNullOrEmpty(exePath))
                result = exePath;
            else
                result = app;


            Logger.Instance.LogMessage($"FixAppPath thepath:{thepath} app:{app} result:{result} pathEXT:{String.Join(",", pathEXT)} paths:{String.Join(",", paths)} envPath:{String.Join(",", enviromentPath)}");
            return result;
        }

    }
}

