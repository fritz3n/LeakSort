using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakStream : IDisposable
	{
		public string Path { get; }
		private FileStream fileStream = null;
		private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		public int LastUsed { get; private set; }

		public LeakStream(string path)
		{
			Path = path;
		}

		public void Open() => fileStream = new FileStream(Path,
				FileMode.Append, FileAccess.Write, FileShare.None,
				bufferSize: 4096, useAsync: true);

		public async Task WriteLine(string line, int index)
		{
			try
			{
				await semaphore.WaitAsync();
				byte[] encodedText = Encoding.Unicode.GetBytes(line + "\n");
				await fileStream.WriteAsync(encodedText, 0, encodedText.Length);
			}
			finally
			{
				semaphore.Release();
			}
		}

		public void Dispose()
		{
			fileStream.Flush();
			fileStream.Dispose();
			GC.SuppressFinalize(this);
		}

		public async Task DisposeAsync()
		{
			await fileStream.FlushAsync();
			await fileStream.DisposeAsync();
			GC.SuppressFinalize(this);
		}
	}
}
