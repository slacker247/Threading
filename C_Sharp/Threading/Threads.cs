using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Threading
{
    public class Threads
    {
        public static void waitForPooling(ref List<Thread> threads, int maxThreads)
        {
            bool limit = true;
            while (limit)
            {
                int activeThreads = 0;
                for (int i = 0; i < threads.Count; i++)
                {
                    if (threads[i].IsAlive)
                        activeThreads++;
                }
                if (activeThreads < maxThreads)
                    limit = false;
                else
                    Thread.Sleep(120);
            }
        }

        public static void waitForThreads(ref List<Thread> threads, bool print = false)
        {
            DateTime start = DateTime.Now;
            while (threads.Count > 0)
            {
                TimeSpan delta = DateTime.Now - start;
                for (int i = 0; i < threads.Count; i++)
                {
                    if (!threads[i].IsAlive &&
                        threads[i].Join(150))
                    {
                        if(print)
                            Console.WriteLine("Thread finished: " + threads[i].Name);
                        threads.RemoveAt(i);
                        i--;
                    }
                    //try
                    //{
                    //    if (delta > g_Timeout)
                    //    {
                    //        threads[i].Abort();
                    //    }
                    //}
                    //catch (Exception ex)
                    //{

                    //}
                }
                Thread.Sleep(12);
            }
        }
    }
}
