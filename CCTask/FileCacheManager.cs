using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System.Text;

namespace CCTask
{
	public class FileCacheManager : IDisposable
	{
		public FileCacheManager(string directory = null)
		{
			hasherSource = new ThreadLocal<MD5>(() => MD5.Create());
			hashDb = new Dictionary<Tuple<string, string>, string>();

			hashDbFile = Path.Combine(directory ?? Directory.GetCurrentDirectory(), HashDbFilename);
			Directory.CreateDirectory(Path.GetDirectoryName(hashDbFile));
			Load();
		}

		public void Dispose()
		{
			Save();
		}

		public bool SourceHasChanged(IEnumerable<string> sources, string args)
		{
			var changed = false;
			foreach(var source in sources)
			{
				changed = changed | SourceHasChanged(source, args);
			}
			return changed;
		}

		private string CalculateMD5(string s)
		{
			var md5 = hasherSource.Value;
			return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(s))).ToLower().Replace("-", "");
		}

		private void Load()
		{
			if(!File.Exists(hashDbFile))
			{
				return;
			}
			lock(hashDb)
			{
				foreach(var line in File.ReadLines(hashDbFile))
				{
					var fileAndHash = line.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					hashDb.Add(Tuple.Create(fileAndHash[0], fileAndHash[1]), fileAndHash[2]);
				}
			}
		}

		private void Save()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(hashDbFile));
			lock(hashDb)
			{
				File.WriteAllLines(hashDbFile, hashDb.Select(x => string.Format("{0};{1};{2}", x.Key.Item1, x.Key.Item2, x.Value)));
			}
		}

		private bool SourceHasChanged(string sourcePath, string args)
		{
			if(!File.Exists(sourcePath))
			{
				return true;
			}
			string hash;
			using(var stream = File.OpenRead(sourcePath))
			{
				var hasher = hasherSource.Value;
				hash = BitConverter.ToString(hasher.ComputeHash(stream));
			}

			var result = false;
			var hashKey = Tuple.Create(sourcePath, args);
			lock(hashDb)
			{
				if(!hashDb.ContainsKey(hashKey))
				{
					result = true;
				}
				else
				{
					result = (hashDb[hashKey] != hash);
				}
				if(result)
				{
					hashDb[hashKey] = hash;
				}
			}
			return result;
		}

		private readonly Dictionary<Tuple<string, string>, string> hashDb;
		private readonly string hashDbFile;
		private readonly ThreadLocal<MD5> hasherSource;

		private const string HashDbFilename = "hashes";
	}
}

