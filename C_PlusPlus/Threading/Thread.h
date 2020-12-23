#ifdef WIN32
#include <Windows.h>
#elif linux
#include <pthread.h>

#define WINAPI
#endif

#pragma once

namespace threading
{
	class Thread
	{
	public:
#ifdef WIN32
		Thread(unsigned long(WINAPI * pFunc) (void* pThis), void* pParams);
#elif linux
		Thread(void* (*pFunc)(void* pThis), void* pParams);
#endif
		~Thread(void);

		unsigned long getThreadId();
		void Resume();
		void WaitForDeath();
	private:
		bool m_Running;
#ifdef WIN32
		void* m_Handle;
#elif linux
		pthread_t m_Handle;
		void* (*m_Func)(void* pThis);
		void* m_Params;
#endif
		unsigned long m_ThreadId;     // thread id
	};
}