#ifdef WIN32
#include <Windows.h>
#elif linux
#include <pthread.h>
#endif

#pragma once

namespace threading
{
	class Event
	{
	public:
		Event();
		~Event();

		// put into signaled state
		void Release();
#ifdef WIN32
		void Wait();
		operator void*() {return m_Handle;};
#elif linux
		void Wait(pthread_mutex_t mutex);
		pthread_cond_t getHandle();
#endif

	private:
#ifdef WIN32
		void* m_Handle;
#elif linux
		pthread_cond_t m_Handle;
#endif
	};
}