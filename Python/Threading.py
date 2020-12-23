import sys
import signal
import time
import datetime
import threading
import multiprocessing
import psutil
import gc
import uuid
from enum import Enum
import random

#
# Summary:
#     Specifies the execution states of a System.Threading.Thread.
class ThreadState(Enum):
    #
    # Summary:
    #     The thread has been started, it is not blocked, and there is no pending System.Threading.ThreadAbortException.
    Running = 0
    #
    # Summary:
    #     The thread is being requested to stop. This is for internal use only.
    StopRequested = 1
    #
    # Summary:
    #     The thread is being requested to suspend.
    SuspendRequested = 2
    #
    # Summary:
    #     The thread is being executed as a background thread, as opposed to a foreground
    #     thread. This state is controlled by setting the System.Threading.Thread.IsBackground
    #     property.
    Background = 4
    #
    # Summary:
    #     The System.Threading.Thread.Start method has not been invoked on the thread.
    Unstarted = 8
    #
    # Summary:
    #     The thread has stopped.
    Stopped = 16
    #
    # Summary:
    #     The thread is blocked. This could be the result of calling System.Threading.Thread.Sleep(System.Int32)
    #     or System.Threading.Thread.Join, of requesting a lock — for example, by calling
    #     System.Threading.Monitor.Enter(System.Object) or System.Threading.Monitor.Wait(System.Object,System.Int32,System.Boolean)
    #     — or of waiting on a thread synchronization object such as System.Threading.ManualResetEvent.
    WaitSleepJoin = 32
    #
    # Summary:
    #     The thread has been suspended.
    Suspended = 64
    #
    # Summary:
    #     The System.Threading.Thread.Abort(System.Object) method has been invoked on the
    #     thread, but the thread has not yet received the pending System.Threading.ThreadAbortException
    #     that will attempt to terminate it.
    AbortRequested = 128
    #
    # Summary:
    #     The thread state includes System.Threading.ThreadState.AbortRequested and the
    #     thread is now dead, but its state has not yet changed to System.Threading.ThreadState.Stopped.
    Aborted = 256

class ThreadProcessState(Enum):
    ADDED = 0
    STARTED = 1
    COMPLETED = 2



class BaseWorker:
    def __init__(self):
        self.queueCallbackDelegate = None # (obj: BaseWorker)
        self.threadCallbackDelegate = None # (name = "")
        self.progressCallbackDelegate = None # (id, state: ThreadProcessState)

        self.m_ThreadIdName = ""
        self.setName(uuid.uuid1())
        self.m_Proc = ""
        self.m_HistoryId = -1
        self.m_UserId = -1
        self.m_Thread = None
        self.m_ThreadState = ThreadState.Unstarted
        self.m_Stop = False
            
        self.m_SimpleMethod = None
        self.m_ParamMethod = None
        self.Params = None

        #The seed thread is the initial thead created by the collector as a result of the 
        # command sent to the collector.  All other threads that spawn from a seed thread
        # are child threads and therefore not seed threads. (used for progress tracking)
        self.m_SeedThread = False

        self.queueCallback = None
        self.threadCallback = None
        self.progressCallback = None

    def __str__(self):
        return str(self.__dict__)

    #/ <summary>
    #/ Default constructor
    #/ </summary>
    def default(self):
        self.init()

    def setSimpleMethod(self, method):
        self.m_SimpleMethod = method

    def setParmMethod(self, method):
        self.m_ParamMethod = method

    #/ <summary>
    #/ This function will initialize the neccessary class members so if initialization is 
    #/  forgotten the result does not crash the application.
    #/ </summary>
    def init(self):
        self.m_ThreadState = ThreadState.Unstarted
        self.m_SeedThread = False

    def ThreadId(self):
        retVal = -1
        if(self.m_Thread != None):
            retVal = self.m_Thread.ident
        return retVal

    def getName(self):
        return str(self.m_ThreadIdName)

    def setName(self, value):
        self.m_ThreadIdName = str(value)

    def getThreadState(self):
        return self.m_ThreadState

    def setThreadState(self, value):
        self.m_ThreadState = value

    def Start(self, parms=None):
        self.Params = parms
        if(len(self.m_Thread.name) == 0):
            self.m_Thread.name = self.getName()
        self.m_Thread.start()

    def Join(self, timeout=-1):
        self.m_Thread.join(timeout)
        return not self.m_Thread.isAlive()

    def IsAlive(self):
        return self.m_Thread.isAlive()

    def Stop(self):
        self.m_Stop = True

    def setProc(self, proc):
        status = -1
        self.m_Proc = proc
        return status

    def setParams(self, paramList):
        status = -1
        self.Params = paramList
        return status

    def doWork(self):
        self.setThreadState(ThreadState.Running)
        self.m_SimpleMethod()
        self.setThreadState(ThreadState.Stopped)
        if (self.threadCallback != None):
            self.threadCallback(self.ThreadId())

    def doWorkP(self, *parms):
        self.setThreadState(ThreadState.Running)
        self.m_ParamMethod(parms)
        self.setThreadState(ThreadState.Stopped)
        if (self.threadCallback != None):
            self.threadCallback(self.ThreadId())
        else:
            print("{} - thread callback is null".format(self.ThreadId()))

    def setThread(self, th):
        self.m_Thread = th

    #/ <summary>
    #/ This function will create a thread for the worker.
    #/ 
    #/ This is a child thread of a main worker thread.  So
    #/ it passes all the attributes of the parent thread to
    #/ the child thread.
    #/ </summary>
    #/ <param name="worker">The worker thread to create a thread for.</param>
    #/ <returns>The created thread.</returns>
    def createThread(self, worker):
        if (worker != self):
            worker.queueCallback = self.queueCallback
            worker.threadCallback = self.threadCallback
            worker.progressCallback = self.progressCallback
            worker.m_HistoryId = self.m_HistoryId
            worker.m_UserId = self.m_UserId
            worker.Params = self.Params
            #worker.setProc(self.m_Proc)

        if(self.progressCallback != None):
            self.progressCallback(worker.m_HistoryId, ThreadProcessState.ADDED)

        thread = None
        if (worker.m_SimpleMethod != None):
            thread = threading.Thread(target=worker.doWork)
        elif (worker.m_ParamMethod != None):
            thread = threading.Thread(target=worker.doWorkP, args=worker.Params)

        if (thread != None):
            name = self.getName()
            if (len(name) == 0):
                name = str(type(self))
            thread.name = name + "-" + str(thread.ident)
            if (len(worker.getName()) == 0):
                worker.setName(thread.name)
            else:
                worker.setName("-" + str(thread.ident))
            if (len(thread.name) == 0):
                print("No name Thread.")

            worker.setThread(thread)

class Threading:
    def __init__(self):
        self.ThreadQueue = []
        self.DeadThreads = []
        self.ActiveThreads = {}
        self.CancelThreads = {}
        self.TotalThreads = 0
        self.UserMaxThreads = 0
        self.MaxThreads = 0
        self.Run = True
        self.Lock = threading.Lock()
        self.InstanceName = "Manager_"
        self.InstanceCount = 0
        self.Debug = False
        self.ThreadIndex = 0
        signal.signal(signal.SIGINT, self.signal_handler)
        pass

    def signal_handler(self, sig, frame):
        self.stop()
        self.waitAll()
        sys.exit(0)

    def waitAll(self):
        waiting = True
        while(waiting):
            if(len(self.ActiveThreads) == 0 and len(self.ThreadQueue) == 0):
                waiting = False
            time.sleep(2)
        pass

    def start(self):
        self.Run = True
        th = threading.Thread(target=self.runThreadsLoop)
        th.start()
        pass

    def stop(self):
        self.ThreadQueue.clear()
        print("Waiting for running threads to stop...")
        while(len(self.ActiveThreads) > 0):
            i = 0
            while(len(self.DeadThreads) > 0 and i < len(self.DeadThreads)):
                if(i <= len(self.DeadThreads) and i >= 0 and self.DeadThreads[i].Join(120)):
                    self.DeadThreads.pop(i)
                    i = i - 1
                    pass
                i = i + 1
        print("All threads stopped.")
        self.Run = False
        pass

    def setSeedThreads(self, cnt):
        self.ActiveSeedThreads = cnt
        self.TotalThreads += cnt

    def setCancelledThreads(self):
        while(self.Run):
            start = datetime.datetime.now()
            i = 0
            while(i < len(self.CancelThreads) and
                datetime.datetime.now() - start < datetime.timedelta(seconds=2)):
                uid = self.CancelThreads.keys(i)
                hid = self.CancelThreads[uid]
                b = 0
                while (b < len(self.ActiveThreads) and
                    datetime.datetime.now() - start < datetime.timedelta(seconds=2)):
                    key = self.ActiveThreads.keys(b)
                    th = ActiveThreads[key]
                    b += 1
                n = 0
                while (n < len(self.ThreadQueue) and
                    datetime.datetime.now() - start < datetime.timedelta(seconds=2)):
                    th = ThreadQueue[n]
                    n += 1
                self.CancelThreads.pop(uid)
                i += 1
            time.sleep(0.120)
            pass
        pass

    def setActiveThreads(self):
        while (self.Run):
            start = datetime.datetime.now()
            if ((len(self.ActiveThreads) < self.MaxThreads) and (len(self.ThreadQueue) > 0)):
                with self.Lock:
                    start = datetime.datetime.now()
                    while (len(self.ActiveThreads) < self.MaxThreads and
                        len(self.ThreadQueue) > 0 and
                        datetime.datetime.now() - start < datetime.timedelta(seconds=2)):
                        #pop a thread and start, and increment active threads
                        popThread = self.ThreadQueue[0]
                        self.ThreadQueue.pop(0)
                        key = popThread.ThreadId()
                        if (popThread != None and
                            not key in self.ActiveThreads):
                            if key == None:
                                key = self.ThreadIndex
                                self.ThreadIndex += 1
                            self.ActiveThreads[key] = popThread
                            if (popThread.m_ThreadState == ThreadState.Unstarted):
                                popThread.Start()
                            else:
                                print("{} - setActiveThreads - thread already started".format(key))
                        else:
                            print("{} - setActiveThreads - Failed to start thread".format(key))
            time.sleep(0.120)
        pass

    def setMaxThreads(self):
        oneTime = True
        idleTime = datetime.datetime.now()

        while (self.Run):
            if (self.MaxThreads < self.UserMaxThreads):
                cpuUsage = psutil.cpu_percent()
                if (cpuUsage < 80.0):
                    self.MaxThreads += 1
                else:
                    self.MaxThreads = multiprocessing.cpu_count() + 1

                if (self.MaxThreads < multiprocessing.cpu_count() + 1):
                    self.MaxThreads = multiprocessing.cpu_count() + 1
            else:
                self.MaxThreads = self.UserMaxThreads

            totalThreadCount = (len(self.ThreadQueue) + len(self.ActiveThreads) + len(self.DeadThreads))

            if (totalThreadCount == 0 and oneTime):
                print("\nIdle...")
                oneTime = False

            if (totalThreadCount > 0):
                oneTime = True
                idleTime = datetime.datetime.now()
            time.sleep(0.120)

    def gcCleanup(self):
        #mainProc = System.Diagnostics.Process.GetCurrentProcess()
        reqMax = self.UserMaxThreads
        gcTime = datetime.datetime.now()
        while (self.Run):
            if (datetime.datetime.now() - gcTime > datetime.timedelta(seconds=3)):
                #print("GC")
                gc.collect()
                #gc.WaitForPendingFinalizers()
                gcTime = datetime.datetime.now()
            time.sleep(0.120)

    # <summary>
    # This function serves as the master run thread loop.
    # </summary>
    def runThreadsLoop(self):
        printStatusTime = datetime.datetime.now()
        count = 0
        cancelTh = BaseWorker()
        cancelTh.setSimpleMethod(self.setCancelledThreads)
        cancelTh.Name = "setCancelledThreads"
        cancelTh.createThread(cancelTh)
        cancelTh.Start()
        activeTh = BaseWorker()
        activeTh.setSimpleMethod(self.setActiveThreads)
        activeTh.Name = "setActiveThreads"
        activeTh.createThread(activeTh)
        activeTh.Start()
        maxTh = BaseWorker()
        maxTh.setSimpleMethod(self.setMaxThreads)
        maxTh.Name = "setMaxThreads"
        maxTh.createThread(maxTh)
        maxTh.Start()
        gcTh = BaseWorker()
        gcTh.setSimpleMethod(self.gcCleanup)
        gcTh.Name = "gcCleanup"
        gcTh.createThread(gcTh)
        gcTh.Start()

        # maybe create 3 threads to manage the arrays.
        while (self.Run):
            start = datetime.datetime.now()
            completedThreads = 0

            if (datetime.datetime.now() - printStatusTime > datetime.timedelta(seconds=5)
                and (len(self.DeadThreads) > 0 or len(self.ActiveThreads) > 0)):
                start = datetime.datetime.now()
                i = 0
                while(i < len(self.DeadThreads) and
                    datetime.datetime.now() - start < datetime.timedelta(seconds=2)):
                    if (self.DeadThreads[i] == None or
                        self.DeadThreads[i].Join(150)):
                        #m_DeadThreads[i] = null
                        self.DeadThreads.pop(i)
                        #Console.Write(".")
                        i -= 1
                        completedThreads += 1
                    i += 1
                if self.Debug:
                    print(self.InstanceName + "\n" +
                                    "Added Threads: " + str(self.TotalThreads) +
                                    "\nQueued Threads: " + str(len(self.ThreadQueue)) +
                                    "\nActive Threads: " + str(len(self.ActiveThreads)) +
                                    "\nCompleted Threads: " + str(completedThreads) +
                                    "\nDead Threads: " + str(len(self.DeadThreads)) +
                                    "\nMax Threads: " + str(self.MaxThreads) +
                                    "\nRequested Threads: " + str(self.UserMaxThreads))
                    self.TotalThreads = 0 # added threads
                    completedThreads = 0
                printStatusTime = datetime.datetime.now()
            else:
                if (len(self.ActiveThreads) > self.MaxThreads):
                    time.sleep(0.120)
                #else
                #    Thread.Sleep(300)
            time.sleep(0.120)

        #update history to complete
        print("All threads complete, press any key to continue.")

    def addThread(self, method, parms=None, name=""):
        worker = BaseWorker()
        if parms == None:
            worker.setSimpleMethod(method)
        else:
            worker.setParmMethod(method)
            worker.Params = parms
        worker.Name = name
        worker.createThread(worker)
        self.createQueueThread(worker)
        time.sleep(0.012)

    # <summary>
    # This function will take in a worker instance object, a callback handle, and 
    #  a social media type.  Then it will create and queue a thread for running.
    # </summary>
    # <param name="worker">The worker instance object.</param>
    def createQueueThread(self, worker: BaseWorker):
        #create worker thread
        self.TotalThreads += 1
        worker.queueCallback = self.queueCallback
        worker.threadCallback = self.threadCallback
        worker.progressCallback = self.progressCallback
        worker.m_SeedThread = True

        worker.createThread(worker)

        with self.Lock:
            #queue thread
            self.ThreadQueue.append(worker)

    # <summary>
    # This function serves as a callback method for queueing threads and
    #  when they complete.
    # </summary>
    # <param name="thread">A handle to the worker thread.</param>
    def queueCallback(self, thread: BaseWorker):
        with self.Lock:
            #if worker isn't null then add thread to queue
            if (thread != None):
                self.ThreadQueue.append(thread)

    # <summary>
    # This function serves as a callback method for queueing threads and
    #  when they complete.
    # </summary>
    # <param name="name">The name of the thread that is finished working.</param>
    def threadCallback(self, name=""):
        if (name != None):
            finishedThread = None
            if (name in self.ActiveThreads):
                #decrement active threads and cleanup
                finishedThread = self.ActiveThreads[name]
            else:
                for key in self.ActiveThreads:
                    if self.ActiveThreads[key].ThreadId() == name:
                        finishedThread = self.ActiveThreads[key]
                        name = key

            if finishedThread == None:
                print("Error: No Active threads with the name: {}".format(name))
                print("ActiveThreads: {}".format(self.ActiveThreads))
            else:
                with self.Lock:
                    self.ActiveThreads.pop(name)
                    self.DeadThreads.append(finishedThread)
        else:
            print("Error: Thread name is not valid.")

    def progressCallback(self, id, state):
        pass






