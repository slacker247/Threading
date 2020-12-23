#ifdef WIN32
#include <Windows.h>
#elif linux
#include <pthread.h>
#endif

#pragma once

namespace threading
{
	class Mutex
	{
		friend class Lock;
	public:
		Mutex()
		{
#ifdef WIN32
			InitializeCriticalSection (& m_CritSection);
#elif linux
			//m_CritSection = PTHREAD_MUTEX_INITIALIZER;
			pthread_mutex_init(&m_CritSection, NULL);
#endif
		}

		~Mutex()
		{
#ifdef WIN32
			DeleteCriticalSection (& m_CritSection);
#elif linux
			pthread_mutex_destroy(&m_CritSection);
#endif
		}
	private:
		void Acquire()
		{
#ifdef WIN32
			EnterCriticalSection (& m_CritSection);
#elif linux
			pthread_mutex_lock(&m_CritSection);
#endif
		}

		void Release()
		{
#ifdef WIN32
			LeaveCriticalSection (& m_CritSection);
#elif linux
			pthread_mutex_unlock(&m_CritSection);
#endif
		}

#ifdef WIN32
		CRITICAL_SECTION m_CritSection;
#elif linux
		pthread_mutex_t m_CritSection;
#endif
	};

	class Lock
	{
	public:
		// Acquire the state of the semaphore
		Lock ( Mutex & mutex )
			: m_Mutex(mutex)
		{
			m_Mutex.Acquire();
		}
		// Release the state of the semaphore
		~Lock ()
		{
			m_Mutex.Release();
		}
	private:
		Mutex & m_Mutex;
	};
}