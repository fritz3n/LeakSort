using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakSaver
	{
		private const string legalCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";

		private Dictionary<string, LeakStream> fileStreams = new Dictionary<string, LeakStream>();
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
			string directory = GetDirectory(line);
			string path = GetFilePath(line);

			if (fileStreams.TryGetValue(path, out LeakStream outStream))
			{
				await outStream.WriteLine(line, WriteCount);
				return;
			}
			CreateDirectory(directory);

			LeakStream stream = new LeakStream(Path.Combine(basePath, path));
			fileStreams.Add(path, stream);
			stream.Open();
			await stream.WriteLine(line, WriteCount);
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

		private string GetPathOfComponents(string text, int layers)
		{
			int layerCount = Math.Min(layers, text.Length);

			string[] components = new string[layerCount];

			for (int i = 0; i < layerCount; i++)
			{
				if (legalCharacters.IndexOf(text[i], StringComparison.InvariantCultureIgnoreCase) != -1)
				{
					components[i] = text[i].ToString().ToLowerInvariant();
				}
				else
				{
					components[i] = "symbols";
				}
			}

			return Path.Combine(components);
		}

		private string GetDirectory(string line) => GetPathOfComponents(line, layers);
		private string GetFilePath(string line) => GetPathOfComponents(line, layers + 1) + ".txt";
	}
}
