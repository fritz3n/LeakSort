using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeakSort
{
    class Program
    {
        static LeakInput li;
        static CancellationTokenSource tsc;
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            LeakReader lr = new LeakReader(3, @"D:\leaks");
            
            tsc = new CancellationTokenSource();
            InputProgress prog;
            using (LeakSaver ls = new LeakSaver(3, @"D:\leaks"))
            {
                li = new LeakInput(@"D:\dropbox", ls, lr);
                Task<InputProgress> inputTask = li.SortAllAsync(tsc.Token);
                Console.ReadLine();
                tsc.Cancel();
                Console.WriteLine("Stopping");
                prog = await inputTask;
                Console.WriteLine("Stopped inputing waiting for flush");
            }

            if (prog.IsDone)
            {
                System.Collections.Generic.IEnumerable<string> res = await lr.Lookup("ric");
            }
            else
            {
                Console.WriteLine("Progress: {0}%\t Done: {1}\tinMB: {2}", prog.Percentage * 100, prog.Completed, prog.Completed / 1000/1000);
                Console.Read();
            }
        }
    }
}
