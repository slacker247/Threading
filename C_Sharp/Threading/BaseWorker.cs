using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace Threading
{
	public class BaseWorker
	{
        public delegate void queueCallbackDelegate(BaseWorker obj);
		public delegate void threadCallbackDelegate(string name = "");
        public delegate void progressCallbackDelegate(long id, ThreadManager.ThreadProcessState state);

		public string m_ThreadIdName;
        protected String m_Proc = "";
        protected Dictionary<String, String> m_Params;
        protected long m_HistoryId = -1;
        protected long m_UserId = -1;
        protected Thread m_Thread;
        protected System.Threading.ThreadState m_ThreadState;
        protected bool m_Stop = false;
        
        protected Action m_SimpleMethod;
        protected Action<object> m_ParamMethod;
        public object Params{get;set;}

		//The seed thread is the initial thead created by the collector as a result of the 
		// command sent to the collector.  All other threads that spawn from a seed thread
		// are child threads and therefore not seed threads. (used for progress tracking)
		public bool m_SeedThread;

        public queueCallbackDelegate queueCallback;
        public threadCallbackDelegate threadCallback;
        public progressCallbackDelegate progressCallback;

		/// <summary>
		/// Default constructor
		/// </summary>
		public BaseWorker()
		{
			init();
		}

        public BaseWorker(Action method)
        {
            init();
            m_SimpleMethod = method;
        }

        public BaseWorker(Action<object> method)
        {
            init();
            m_ParamMethod = method;
        }

        ~BaseWorker()
        {
            //queueCallback;
            //threadCallback;
            //progressCallback;
        }

		/// <summary>
		/// This function will initialize the neccessary class members so if initialization is 
		///  forgotten the result does not crash the application.
		/// </summary>
		protected void init()
		{
            m_ThreadState = System.Threading.ThreadState.Unstarted;
			m_SeedThread = false;
		}

        public int ThreadId
        {
            get
            {
                int retVal = -1;
                if(m_Thread != null)
                    retVal = m_Thread.ManagedThreadId;
                return retVal;
            }
        }
        public String Name
        {
            get
            {
                return m_ThreadIdName;
            }
            set
            {
                this.m_ThreadIdName = value;
            }
        }

        public System.Threading.ThreadState ThreadState
        {
            get
            {
                return this.m_ThreadState;
            }
            set
            {
                this.m_ThreadState = value;
            }
        }

        public void Start()
        {
            if(String.IsNullOrEmpty(this.m_Thread.Name))
                this.m_Thread.Name = m_ThreadIdName;
            if (m_SimpleMethod != null)
                this.m_Thread.Start();
            else if (m_ParamMethod != null)
                this.m_Thread.Start(Params);
        }

        public void Start(object parms)
        {
            this.m_Thread.Start(parms);
        }

        public void Join()
        {
            this.m_Thread.Join();
        }
        public bool Join(int timeout)
        {
            return this.m_Thread.Join(timeout);
        }
        public void Stop()
        {
            this.m_Stop = true;
        }
        public int setProc(String proc)
        {
            int status = -1;
            m_Proc = proc;
            return status;
        }

        public int setParams(Dictionary<String, String> paramList)
        {
            int status = -1;
            m_Params = paramList;
            return status;
        }

        protected virtual void doWork()
        {
            ThreadState = System.Threading.ThreadState.Running;
            m_SimpleMethod();
            ThreadState = System.Threading.ThreadState.Stopped;
            if (threadCallback != null)
                threadCallback(ThreadId.ToString());
        }

        protected virtual void doWorkP(object parms)
        {
            ThreadState = System.Threading.ThreadState.Running;
            m_ParamMethod(parms);
            ThreadState = System.Threading.ThreadState.Stopped;
            if (threadCallback != null)
                threadCallback(ThreadId.ToString());
        }

        public void setThread(Thread th)
        {
            this.m_Thread = th;
        }

		/// <summary>
		/// This function will create a thread for the worker.
        /// 
        /// This is a child thread of a main worker thread.  So
        /// it passes all the attributes of the parent thread to
        /// the child thread.
		/// </summary>
		/// <param name="worker">The worker thread to create a thread for.</param>
		/// <returns>The created thread.</returns>
		public void createThread(ref BaseWorker worker)
		{
            if (worker != this)
            {
                worker.queueCallback = queueCallback;
                worker.threadCallback = threadCallback;
                worker.progressCallback = progressCallback;
                worker.m_HistoryId = this.m_HistoryId;
                worker.m_UserId = this.m_UserId;
                worker.Params = this.Params;
                //worker.setProc(this.m_Proc);
            }

            if(progressCallback != null)
                progressCallback(worker.m_HistoryId, ThreadManager.ThreadProcessState.ADDED);

            Thread thread = null;
            if (worker.m_SimpleMethod != null)
            {
                thread = new Thread(worker.doWork);
            }
            else if (worker.m_ParamMethod != null)
            {
                ParameterizedThreadStart pts = new ParameterizedThreadStart(worker.doWorkP);
                thread = new Thread(pts);
            }

            if (thread != null)
            {
                thread.SetApartmentState(ApartmentState.STA);

                String name = m_ThreadIdName;
                if (String.IsNullOrEmpty(name))
                    name = this.GetType().ToString();
                thread.Name = name + "-" + thread.ManagedThreadId;
                if (String.IsNullOrEmpty(worker.m_ThreadIdName))
                    worker.m_ThreadIdName = thread.Name;
                else
                    worker.m_ThreadIdName += "-" + thread.ManagedThreadId;
                if (thread.Name.Length == 0)
                    Console.WriteLine("No name Thread.");

                worker.setThread(thread);
            }
		}
	}
}
