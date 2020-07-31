using LeakSort;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Query
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string query;
            if (args.Length == 0)
            {
                Console.WriteLine("Enter a query:");
                query = Console.ReadLine();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendJoin(' ', args);
                query = sb.ToString();
            }
            LeakReader lr = new LeakReader(3, @"D:\leaks");
            IEnumerable<string> res = await lr.Lookup(query);
            foreach (string elm in res)
            {
                Console.WriteLine(elm);
            }
#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}
