using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections;


namespace Threading
{ 
	/// <summary>
	/// This class handles all thread creation, scheduling, and callbacks for the 
	/// collection process. In Addition, this class causes updates to the user's 
	/// history table in the user_db with progress/status of the collection.
    /// 
    /// debugging:
    /// http://msdn.microsoft.com/en-us/magazine/cc163528.aspx
    /// http://stackoverflow.com/questions/4373683/unable-to-load-sos-in-windbg
	/// </summary>
	public class ThreadManager
	{
		//These class members are used for data management between multiple threads
        volatile List<BaseWorker> m_ThreadQueue;
        volatile List<BaseWorker> m_DeadThreads = new List<BaseWorker>();
        volatile Dictionary<string, BaseWorker> m_ActiveThreads;
		private System.Object m_Lock = new System.Object();
        protected Dictionary<long, long> m_CancelThreads = new Dictionary<long,long>();

		//used for progress tracking
		volatile int m_TotalThreads;
		volatile int m_ActiveSeedThreads;

        /// <summary>
        /// The max user allowed threads to run concurrently.
        /// </summary>
        public int MaxThreads { get; set; }
		/// <summary>
		/// Var used to manage the System limit on threads
		/// </summary>
		private int m_MaxThreads;

        protected bool m_Run = true;

        protected String m_InstanceName = "Manager_";
        protected static int m_InstanceCount = 0;

        public enum ThreadProcessState
        {
            ADDED,
            STARTED,
            COMPLETED
        };

		#region Constructors
		/// <summary>
		/// Default constructor.
		/// </summary>
		public ThreadManager()
		{
			//get the number of logical processors aka max threads
            m_MaxThreads = Environment.ProcessorCount + 1;
            m_ThreadQueue = new List<BaseWorker>();
            m_ActiveThreads = new Dictionary<string, BaseWorker>();
			m_TotalThreads = 0;
			m_ActiveSeedThreads = 1;
            m_InstanceCount++;
            m_InstanceName += m_InstanceCount.ToString();
		}
		#endregion

        public void waitAll()
        {
            bool waiting = true;
            while(waiting)
            {
                if(m_ActiveThreads.Count == 0 &&
                    m_ThreadQueue.Count == 0)
                {
                    waiting = false;
                }
                Thread.Sleep(2000);
            }
        }

        public int getActiveThreads()
        {
            return m_ActiveThreads.Count;
        }

        public int getQueuedThreads()
        {
            return m_ThreadQueue.Count;
        }

        public void start()
        {
            BaseWorker th = new BaseWorker(runThreadsLoop);
            th.Name = "Main Thread Loop";
            th.createThread(ref th);
            th.Start();
        }

        public void stop()
        {
            m_Run = false;
            int i = 0;
            while(m_DeadThreads.Count > 0)
            {
                if (m_DeadThreads[i].Join(150))
                {
                    m_DeadThreads.RemoveAt(i);
                    i--;
                }
                i++;
            }
        }

        // TODO : implement
        public void stopThreads(long uid, long hid)
        {
            throw new NotImplementedException("stopThreads is not impelemented.");
            m_CancelThreads[uid] = hid;
        }

		/// <summary>
		/// This function will set the numer of initial threads needed by the collector.
		/// </summary>
		/// <param name="count">The number of initial theads.</param>
		public void setSeedThreads(int count)
		{
			m_ActiveSeedThreads = count;
			m_TotalThreads += count;
		}

        // Pause all requested cancel threads
        protected void setCancelledThreads()
        {
            while (m_Run)
            {
                DateTime start = DateTime.Now;
                for (int i = 0;
                    i < m_CancelThreads.Keys.Count &&
                    DateTime.Now - start < TimeSpan.FromSeconds(2);
                    i++)
                {
                    long uid = m_CancelThreads.Keys.ElementAt<long>(i);
                    long hid = m_CancelThreads[uid];
                    for (int b = 0;
                        b < m_ActiveThreads.Count &&
                        DateTime.Now - start < TimeSpan.FromSeconds(2);
                        b++)
                    {
                        String key = m_ActiveThreads.Keys.ElementAt<String>(b);
                        BaseWorker th = m_ActiveThreads[key];
                        if (false)
                        {
                            th.Stop();
                        }
                    }
                    for (int n = 0;
                        n < m_ThreadQueue.Count &&
                        DateTime.Now - start < TimeSpan.FromSeconds(2);
                        n++)
                    {
                        BaseWorker th = m_ThreadQueue[n];
                        if (false)
                        {
                            m_ThreadQueue.RemoveAt(n);
                            n--;
                        }
                    }
                    m_CancelThreads.Remove(uid);
                }
                Thread.Sleep(120);
            }
        }

        protected void setActiveThreads()
        {
            while (m_Run)
            {
                DateTime start = DateTime.Now;
                if ((m_ActiveThreads.Count() < m_MaxThreads) && (m_ThreadQueue.Count > 0)
                    )
                {
                    lock (m_Lock)
                    {
                        start = DateTime.Now;
                        while (m_ActiveThreads.Count() < m_MaxThreads &&
                            m_ThreadQueue.Count() > 0 &&
                            DateTime.Now - start < TimeSpan.FromSeconds(2))
                        {
                            //pop a thread and start, and increment active threads
                            BaseWorker popThread = m_ThreadQueue[0];
                            m_ThreadQueue.RemoveAt(0);
                            if (popThread != null &&
                                !m_ActiveThreads.ContainsKey(popThread.ThreadId.ToString()))
                            {
                                m_ActiveThreads[popThread.ThreadId.ToString()] = popThread;
                                if (popThread.ThreadState == System.Threading.ThreadState.Unstarted)
                                {
                                    popThread.Start();
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(120);
            }
        }


        static PerformanceCounter cpuCounter = new PerformanceCounter();
        static bool m_Init = false;
        protected static void init()
        {
            if (!m_Init)
            {
                cpuCounter.CategoryName = "Processor";
                cpuCounter.CounterName = "% Processor Time";
                cpuCounter.InstanceName = "_Total";
                m_Init = true;
            }
        }

        /// <summary>
        /// Usage percentage
        /// </summary>
        /// <returns></returns>
        public static float getCPUCounter()
        {
            init();

            // now matches task manager reading
            float secondValue = cpuCounter.NextValue();

            return secondValue;
            //			return 1;
        }


        protected void setMaxThreads()
        {
            bool oneTime = true;
            DateTime idleTime = DateTime.Now;


            while (m_Run)
            {
                if (m_MaxThreads < MaxThreads)
                {
                    float cpuUsage = getCPUCounter();
                    if (cpuUsage < 80f)
                    {
                        m_MaxThreads++;
                    }
                    else
                        m_MaxThreads = Environment.ProcessorCount + 1;

                    if (m_MaxThreads < Environment.ProcessorCount + 1)
                        m_MaxThreads = Environment.ProcessorCount + 1;
                }
                else
                    m_MaxThreads = MaxThreads;

                int totalThreadCount = (m_ThreadQueue.Count + m_ActiveThreads.Count + m_DeadThreads.Count);

                if (totalThreadCount == 0 && oneTime)
                {
                    Console.WriteLine("\nIdle...");
                    //if (mainProc.HandleCount > 2000)
                    //    Environment.FailFast("Releasing Resources");
                    oneTime = false;
                }

                if (totalThreadCount > 0)
                {
                    oneTime = true;
                    idleTime = DateTime.Now;
                }
                Thread.Sleep(120);
            }
        }

        protected void gcCleanup()
        {
            Process mainProc = System.Diagnostics.Process.GetCurrentProcess();
            int reqMax = MaxThreads;
            DateTime gcTime = DateTime.Now;
            while (m_Run)
            {
                if (DateTime.Now - gcTime > TimeSpan.FromSeconds(3))
                {
                    //Console.WriteLine("GC");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    gcTime = DateTime.Now;
                    //if(mainProc.HandleCount > 10000)
                    //{
                    //    MaxThreads = 1;
                    //}
                    //else if(mainProc.HandleCount < 5000)
                    //{
                    //    MaxThreads = reqMax;
                    //}
                }
                Thread.Sleep(120);
            }
        }

        /// <summary>
        /// This function serves as the master run thread loop.
        /// </summary>
        public void runThreadsLoop()
		{
            DateTime printStatusTime = DateTime.Now;
            int count = 0;
            BaseWorker cancelTh = new BaseWorker(this.setCancelledThreads);
            cancelTh.Name = "setCancelledThreads";
            cancelTh.createThread(ref cancelTh);
            cancelTh.Start();
            BaseWorker activeTh = new BaseWorker(this.setActiveThreads);
            activeTh.Name = "setActiveThreads";
            activeTh.createThread(ref activeTh);
            activeTh.Start();
            BaseWorker maxTh = new BaseWorker(this.setMaxThreads);
            maxTh.Name = "setMaxThreads";
            maxTh.createThread(ref maxTh);
            maxTh.Start();
            BaseWorker gcTh = new BaseWorker(this.gcCleanup);
            gcTh.Name = "gcCleanup";
            gcTh.createThread(ref gcTh);
            gcTh.Start();

            // maybe create 3 threads to manage the arrays.
            while (m_Run)
			{
                DateTime start = DateTime.Now;
                int completedThreads = 0;

                if (DateTime.Now - printStatusTime > TimeSpan.FromSeconds(5)
                    && (m_DeadThreads.Count > 0 || m_ActiveThreads.Count > 0))
                {
                    start = DateTime.Now;
                    for (int i = 0;
                        i < m_DeadThreads.Count &&
                        DateTime.Now - start < TimeSpan.FromSeconds(2);
                        i++)
                    {
                        if (m_DeadThreads[i] == null ||
                            m_DeadThreads[i].Join(150))
                        {
                            //m_DeadThreads[i] = null;
                            m_DeadThreads.RemoveAt(i);
                            //Console.Write(".");
                            i--;
                            completedThreads++;
                        }
                    }
#if DEBUG
                    Console.WriteLine(m_InstanceName + "\n" +
                                      "Added Threads: " + m_TotalThreads +
                                      "\nQueued Threads: " + m_ThreadQueue.Count +
                                      "\nActive Threads: " + m_ActiveThreads.Count +
                                      "\nCompleted Threads: " + completedThreads +
                                      "\nDead Threads: " + m_DeadThreads.Count +
                                      "\nMax Threads: " + m_MaxThreads +
                                      "\nRequested Threads: " + MaxThreads);
                    m_TotalThreads = 0; // added threads
                    completedThreads = 0;
#endif
                    printStatusTime = DateTime.Now;
                }
                else
                {
                    if (m_ActiveThreads.Count > m_MaxThreads)
                        Thread.Sleep(120);
                    //else
                    //    Thread.Sleep(300);
                }
                Thread.Sleep(120);
			}

			//update history to complete
			Console.WriteLine("All threads complete, press any key to continue.");
		}

        public void addThread(Action<object> method, object parms, String name = "")
        {
            Threading.BaseWorker worker = new Threading.BaseWorker(method);
            worker.Params = parms;
            worker.Name = name;
            worker.createThread(ref worker);
            this.createQueueThread(worker);
            Thread.Sleep(12);
        }

        public void addThread(Action method, String name = "")
        {
            Threading.BaseWorker worker = new Threading.BaseWorker(method);
            worker.Name = name;
            worker.createThread(ref worker);
            this.createQueueThread(worker);
            Thread.Sleep(12);
        }

        /// <summary>
        /// This function will take in a worker instance object, a callback handle, and 
        ///  a social media type.  Then it will create and queue a thread for running.
        /// </summary>
        /// <param name="worker">The worker instance object.</param>
        public void createQueueThread(BaseWorker worker)
        {
            //create worker thread
            m_TotalThreads++;
            worker.queueCallback += new BaseWorker.queueCallbackDelegate(queueCallback);
            worker.threadCallback += new BaseWorker.threadCallbackDelegate(threadCallback);
            worker.progressCallback += new BaseWorker.progressCallbackDelegate(progressCallback);
            worker.m_SeedThread = true;

            worker.createThread(ref worker);

            lock (m_Lock)
            {
                //queue thread
                m_ThreadQueue.Add(worker);
            }
        }

		/// <summary>
		/// This function serves as a callback method for queueing threads and
		///  when they complete.
		/// </summary>
		/// <param name="thread">A handle to the worker thread.</param>
		void queueCallback(BaseWorker thread)
		{
			lock (m_Lock)
			{
				//if worker isn't null then add thread to queue
                if (thread != null)
                {
                    m_ThreadQueue.Add(thread);
                }
			}
		}

		/// <summary>
		/// This function serves as a callback method for queueing threads and
		///  when they complete.
		/// </summary>
		/// <param name="name">The name of the thread that is finished working.</param>
		void threadCallback(string name = "")
		{
            lock (m_Lock)
            {
                if (name != null)
                {
                    if (m_ActiveThreads.ContainsKey(name))
                    {
                        //decrement active threads and cleanup
                        BaseWorker finishedThread = m_ActiveThreads[name];

                        m_ActiveThreads.Remove(name);
                        m_DeadThreads.Add(finishedThread);
                    }
                    else
                        Console.WriteLine("Error: No Active threads with the name: " + name);
                }
                else
                    Console.WriteLine("Error: Thread name is not valid.");
            }
        }

		/// <summary>
		/// This function serves as a callback method for tracking completion progress
		/// </summary>
		/// <param name="mainReturnCnt">The number of threads that will be returned by a starting
		/// thread. Only sent by an initial thread and used for progress tracking.</param>
		/// <param name="isSeedThread">Switch stating if thread is a seed thread.</param>
		void progressCallback(long id, ThreadProcessState state)
		{
			lock (m_Lock)
			{
                //if (m_UserHistoryItems.ContainsKey(id))
                //{
                //    History history = m_UserHistoryItems[id];
                //    History dbHistory = new History(history.getId(), history.getUserId());
                //    HistoryParametersBase parameters = dbHistory.getParameters();
                //    if (parameters.getErrorCode() == History.ErrorType.Canceled)
                //        this.stopThreads(dbHistory.getUserId(), dbHistory.getId());
                //    String status = "";
                //    if (state == ThreadProcessState.ADDED)
                //    {
                //        // need to increase when we add the thread.  otherwise we may complete the thread
                //        // before the spawned threads start and in that case we'll report that we're complete 
                //        // too early.
                //        long cnt = parameters.getTotalThreads() ?? 0;
                //        cnt++;
                //        parameters.setTotalThreads(cnt);
                //        status = History.STATUS_INPROGRESS;
                //    }
                //    else if (state == ThreadProcessState.STARTED)
                //    {
                //        // just update the last updated time (to show progress)
                //        parameters.setLastProgressUpdate();
                //    }
                //    else if (state == ThreadProcessState.COMPLETED)
                //    {
                //        long cnt = parameters.getCompletedThreads() ?? 0;
                //        cnt++;
                //        parameters.setCompletedThreads(cnt);

                //        // TODO - We should really be paying attention to changes to the history status here
                //        // but we're caching the history item in the dictionary.
                //        if (cnt >= parameters.getTotalThreads() && dbHistory.getStatus() != History.STATUS_ERROR)
                //        {
                //            status = History.STATUS_COMPLETE;
                //            parameters.setEndTime(DateTime.UtcNow);
                //        }
                //    }
                //    if (!String.IsNullOrEmpty(status) &&
                //        parameters.getErrorCode() != History.ErrorType.Canceled)
                //    {
                //        dbHistory.setStatus(status);
                //    }
                //    if(parameters.getErrorCode() != History.ErrorType.Canceled)
                //        parameters.setErrorCode(History.ErrorType.None);
                //    dbHistory.setParameters(parameters);
                //    dbHistory.save();
                //    String historyStatus = dbHistory.getStatus();
                //    if (historyStatus == History.STATUS_COMPLETE || historyStatus == History.STATUS_ERROR)
                //        m_UserHistoryItems.Remove(id);
                //}
			}
		}
	}
}
