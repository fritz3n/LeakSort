using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakSaver : IDisposable
	{
		private const string legalCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";

		private Dictionary<string, LeakStreamWriter> fileStreams = new Dictionary<string, LeakStreamWriter>();
		private HashSet<string> directories = new HashSet<string>();
		private int writeCount;
		private readonly int layers;
		private readonly string basePath;

		public int WriteCount { get => writeCount; private set => writeCount = value; }

		public LeakSaver(int layers, string basePath)
		{
			this.layers = layers;
			this.basePath = basePath;
		}

		public async Task SaveLine(string line)
		{
			Interlocked.Increment(ref writeCount);
			string directory = GetDirectory(basePath, line, layers - 1); //describes the dir in which the file belongs
			string path = GetFilePath(basePath, line, layers);

			if (fileStreams.TryGetValue(path, out LeakStreamWriter outStream))
			{
				await outStream.WriteLine(line, WriteCount);
				return;
			}
			else
			{
				CreateDirectory(directory);

				LeakStreamWriter stream = new LeakStreamWriter(Path.Combine(basePath, path));
				fileStreams.Add(path, stream);
				stream.Open();
				await stream.WriteLine(line, WriteCount);
			}
		}

		private void CreateDirectory(string directory)
		{
			if (directories.Contains(directory))
				return;
			string[] subPaths = directory.Split(Path.AltDirectorySeparatorChar,
							  Path.DirectorySeparatorChar);

			string curPath = string.Empty;

			for (int i = 0; i < subPaths.Count(); i++)
			{
				curPath = Path.Combine(curPath, subPaths[i]);
				if (!directories.Contains(curPath))
					Directory.CreateDirectory(Path.Combine(basePath, curPath));
			}
		}

		public static string GetPathOfComponents(string basePath, string text, int layers)
		{
			int layerCount = Math.Min(layers, text.Length);

			string[] components = new string[layerCount + 1];
			components[0] = basePath;
			for (int i = 0; i < layerCount; i++)
			{
				if (legalCharacters.IndexOf(text[i], StringComparison.InvariantCultureIgnoreCase) != -1)
				{
					components[i + 1] = text[i].ToString().ToLowerInvariant();
				}
				else
				{
					components[i + 1] = "symbols";
				}
			}

			return Path.Combine(components);
		}

		public static string GetDirectory(string basePath, string line, int layers) => GetPathOfComponents(basePath, line, layers);
		public static string GetFilePath(string basePath, string line, int layers) => GetPathOfComponents(basePath, line, layers) + ".txt";


		public void Dispose()
		{
			foreach (var kv in fileStreams)
			{
				kv.Value.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		public async Task DisposeAsync()
		{
			foreach(var kv in fileStreams)
            {
				await kv.Value.DisposeAsync();
            }
			GC.SuppressFinalize(this);
		}
	}
}
