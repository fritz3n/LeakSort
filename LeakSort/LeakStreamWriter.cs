using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
	class LeakStreamWriter : IDisposable
	{
		public string Path { get; }
		private StreamWriter streamWriter = null;
		private ConcurrentQueue<string> queue;
		private CancellationTokenSource tokenSource = new CancellationTokenSource();
		private AsyncAutoResetEvent addedEvent = new AsyncAutoResetEvent(false);
		private AsyncManualResetEvent finishedEvent = new AsyncManualResetEvent(false);

		public int LastUsed { get; private set; }
		public const int MaxQueueCount = 1000;

		public LeakStreamWriter(string path)
		{
			Path = path;
			queue = new ConcurrentQueue<string>();
		}

		public void Open()
		{
			streamWriter = new StreamWriter(new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
			{
				AutoFlush = true
			};
			Task.Run(WriteLoop);
		}

		private async void WriteLoop()
		{
			try
			{
				while (true)
				{
					if (queue.IsEmpty)
					{
						await addedEvent.WaitAsync(tokenSource.Token);
						tokenSource.Token.ThrowIfCancellationRequested();
					}

					if (queue.TryDequeue(out string line))
					{
						await streamWriter.WriteLineAsync(line);
					}
				}
			}
			catch (TaskCanceledException) { }
			finally
			{
				finishedEvent.Set();
			}
		}
        
		public void Dispose()
        {
            exit = true;
            Writer.Wait();
            streamWriter.Flush();
            streamWriter.Dispose();
            GC.SuppressFinalize(this);
        }
		
		public async Task WriteLine(string line, int index)
		{
			if (tokenSource.IsCancellationRequested)
				throw new InvalidOperationException();

			while (queue.Count >= MaxQueueCount)
			{
				await Task.Delay(100);
			}
			queue.Enqueue(line);
		}

		public void Dispose()
		{
			tokenSource.Cancel();
			finishedEvent.Wait();
			streamWriter.Flush();
			streamWriter.Dispose();
			GC.SuppressFinalize(this);
		}

		public async Task DisposeAsync()
		{
			tokenSource.Cancel();
			await finishedEvent.WaitAsync();
			await streamWriter.FlushAsync();
			await streamWriter.DisposeAsync();
			GC.SuppressFinalize(this);
		}
	}
}
