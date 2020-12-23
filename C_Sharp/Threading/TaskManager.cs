using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Threading
{
    public class TaskManager
    {
        static List<NamedTask> Tasks = new List<NamedTask>();
        static int AddedTasks = 0;

        public static void WaitAll()
        {
            while(Tasks.Count > 0)
                Thread.Sleep(120);
        }

        public static void AddTask(NamedTask t)
        {
            AddedTasks++;
            lock(Tasks)
                Tasks.Add(t);
        }

        public static void run(CancellationToken token)
        {
            int completed = 0;
            int faulted = 0;
            DateTime lastUpdate = DateTime.Now;
            DateTime timeout = DateTime.Now;
            Dictionary<String, int> m_NamedTasks = new Dictionary<string, int>();
            while (!token.IsCancellationRequested)
            {
                int queued = 0;
                int running = 0;
                timeout = DateTime.Now;
                for (int i = 0;
                    i < Tasks.Count &&
                    (DateTime.Now - timeout < TimeSpan.FromMinutes(1));
                    i++)
                {
                    NamedTask nts = Tasks[i];
                    if (nts != null)
                    {
                        if (nts.Status == TaskStatus.WaitingToRun ||
                            nts.Status == TaskStatus.Created ||
                            nts.Status == TaskStatus.WaitingForActivation ||
                            nts.Status == TaskStatus.WaitingForChildrenToComplete)
                            queued++;
                        else if (nts.Status == TaskStatus.Running)
                            running++;
                        else if (nts.Status == TaskStatus.Faulted)
                        {
                            faulted++;
                            //Utilities.Logger.log(nts.Exception.Message, Utilities.MessageSeverity.ERROR);
                            Console.WriteLine(nts.Exception.Message);
                            //Utilities.Logger.log(nts.Exception.StackTrace, Utilities.MessageSeverity.ERROR);
                            Console.WriteLine(nts.Exception.StackTrace);
                            if (nts.Exception.InnerException != null)
                            {
                                //Utilities.Logger.log(nts.Exception.InnerException.Message, Utilities.MessageSeverity.ERROR);
                                Console.WriteLine(nts.Exception.InnerException.Message);
                                //Utilities.Logger.log(nts.Exception.InnerException.StackTrace, Utilities.MessageSeverity.ERROR);
                                Console.WriteLine(nts.Exception.InnerException.StackTrace);
                            }
                            lock (Tasks)
                                Tasks.RemoveAt(i);
                            i--;
                        }
                        else if (nts.Status == TaskStatus.RanToCompletion)
                        {
                            completed++;
                            lock (Tasks)
                                Tasks.RemoveAt(i);
                            i--;
                        }
                        if (nts != null &&
                            m_NamedTasks.ContainsKey(nts.Name))
                            m_NamedTasks[nts.Name]++;
                        else if(nts != null)
                            m_NamedTasks.Add(nts.Name, 1);
                    }
                    else
                    {
                        completed++;
                        lock (Tasks)
                            Tasks.RemoveAt(i);
                        i--;
                    }
                }

                if (DateTime.Now - lastUpdate > TimeSpan.FromSeconds(3))
                {
                    StringBuilder output = new StringBuilder("");
                    output.AppendLine("Added:     " + AddedTasks);
                    output.AppendLine("Queued:    " + queued);
                    output.AppendLine("Running:   " + running);
                    output.AppendLine("Completed: " + completed);
                    output.AppendLine("Faulted:   " + faulted);
                    output.AppendLine("Total:     " + Tasks.Count);
                    String[] keys = m_NamedTasks.Keys.ToArray<String>();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        String name = keys[i];
                        String nStr = name;
                        if (String.IsNullOrEmpty(nStr))
                            nStr = "UnNamed Tasks";
                        output.AppendLine(nStr + ": " + m_NamedTasks[name]);
                    }
                    m_NamedTasks.Clear();
                    output.AppendLine("--------------------");
                    Console.Write(output.ToString());
                    AddedTasks = 0;
                    completed = 0;
                    faulted = 0;
                    lastUpdate = DateTime.Now;
                }
                Thread.Sleep(12);
            }
        }
    }
}
