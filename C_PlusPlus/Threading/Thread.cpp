#include "Thread.h"

namespace threading
{
#ifdef WIN32
	Thread::Thread(unsigned long(WINAPI * pFunc) (void* pThis), void* pParams)
	{
		m_Running = false;
		m_ThreadId = -1;
        m_Handle = CreateThread (
            0, // Security attributes
            0, // Stack size
            pFunc,
            pParams,
            CREATE_SUSPENDED,
            &m_ThreadId);
	}
#elif linux
	Thread::Thread(void* (*pFunc)(void* pThis), void* pParams)
	{
		m_Running = false;
		m_ThreadId = -1;
		m_Func = pFunc;
		m_Params = pParams;
	}
#endif

	Thread::~Thread(void)
	{
#ifdef WIN32
		CloseHandle(m_Handle);
#elif linux
		if(m_ThreadId != -1)
			pthread_cancel((pthread_t) m_Handle);
#endif
	}

	unsigned long Thread::getThreadId()
	{
		return this->m_ThreadId;
	}

	void Thread::Resume()
	{
		m_Running = true;
#ifdef WIN32
		ResumeThread(m_Handle);
#elif linux
		m_ThreadId = pthread_create( &m_Handle, NULL, m_Func, m_Params);
#endif
	}

	void Thread::WaitForDeath()
    {
		if(m_Running)
		{
#ifdef WIN32
			WaitForSingleObject(m_Handle, 2000);
#elif linux
			void *status;
			pthread_join((pthread_t)m_Handle, &status);
#endif
		}
		m_Running = false;
    }
}