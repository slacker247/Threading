using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Threading;

namespace TestThreading
{
    class Program
    {
        // 200 checks
        // Duration: 00:00:48.5025523
        // Duration: 00:00:46.4483556
        // Duration: 00:00:46.4556956
        // Duration: 00:00:46.4491453
        // Duration: 00:00:46.3756094
        // Duration: 00:00:46.4478263

        // Duration: 00:00:45.4138977
        // Duration: 00:00:45.4934402
        // Duration: 00:00:45.4601453

        // 2,000,000 checks
        // Duration: 00:05:15.0462948
        // Recursion
        // Duration: 00:00:12.5003418

        // Duration: 00:05:14.6243855
        // Duration: 00:30:58.0044613
        // Duration: 00:03:12.4177579
        // threading
        // Duration: 00:00:36.4281392
        // Duration: 00:00:12.5870208
        // 200,000,000

        static bool isPrime(int n, int i = 2)
        {
            if (n <= 2)
                return (n == 2 ? true : false);
            else if (n % i == 0)
                return false;
            else if (i * i > n)
                return true;
            return isPrime(n, i+1);
        }

        static void Main(string[] args)
        {
            var mgr = new ThreadManager();
            mgr.MaxThreads = 38;
            mgr.start();
            var testValidation = new int[] {2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71,
                                  73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173,
                                  179, 181, 191, 193, 197, 199 };
            var results = new List<int>();
            results.Add(2);
            Console.WriteLine("Prime: " + 2);

            var start = DateTime.Now;
            var tasks = new List<Action>();
            Console.WriteLine("Creating threads...");
            for(int i = 3; i <= 100000000; i += 100)
            {
                var tsk = new Action(() =>
                {
                    for (int ii = i; ii <= i + 100; ii++)
                    {
                        //bool isP = isPrime(i);
                        object primeLock = new object();
                        bool isP = true;
                        var half = (ii / 2.0);
                        for (int n = 2; n <= half - 1; n++)
                        {
                            if (ii % n == 0)
                            {
                                lock (primeLock)
                                {
                                    isP = false;
                                    //state.Break();
                                    //break;
                                }
                            }
                        }
                        if (isP)
                        {
                            results.Add(i);
                            //Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                        }
                    }
                });
                tasks.Add(tsk);
            }
            Console.WriteLine("Calc Primes...");
            foreach (var tsk in tasks)
                mgr.addThread(tsk);
            mgr.waitAll();
            //Task.WaitAll(tasks.ToArray());

            var end = DateTime.Now;
            var delta = end - start;

            /*
            var validated = true;
            if (testValidation.Length != results.Count)
                validated = false;
            else
                foreach (int i in testValidation)
                {
                    if (!results.Contains(i))
                        validated = false;
                }
            if (!validated)
                Console.WriteLine("Failed Validation.");
            */

            Console.WriteLine("Duration: " + delta);
            Console.ReadKey();
        }
    }
}
