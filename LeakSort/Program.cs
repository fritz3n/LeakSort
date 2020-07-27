using System;

namespace LeakSort
{
	class Program
	{
		static async System.Threading.Tasks.Task Main(string[] args)
		{
			using (LeakSaver ls = new LeakSaver(3, @"D:\leaks"))
			{
				LeakInput li = new LeakInput(@"D:\dropbox", ls);
				await li.SortAllAsync();

			}

			LeakReader lr = new LeakReader(3, @"D:\leaks");
			var res = await lr.Lookup("ric");
		}
	}
}
