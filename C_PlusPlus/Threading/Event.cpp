#include "Event.h"

namespace threading
{
	Event::Event(void)
	{
		// start in non-signaled state (red light)
		// auto reset after every Wait
#ifdef WIN32
		m_Handle = CreateEvent (0, FALSE, FALSE, 0);
#elif linux
		//m_Handle = PTHREAD_COND_INITIALIZER;
		pthread_cond_init(&m_Handle, NULL);
#endif
	}

	Event::~Event(void)
	{
#ifdef WIN32
		CloseHandle (m_Handle);
#elif linux

#endif
	}

#ifdef linux
	pthread_cond_t Event::getHandle()
	{
		return m_Handle;
	}
#endif

	// put into signaled state
	void Event::Release()
	{
#ifdef WIN32
		SetEvent (m_Handle);
#elif linux
		pthread_cond_signal(&m_Handle);
#endif
	}

#ifdef WIN32
	void Event::Wait()
#elif linux
	void Event::Wait(pthread_mutex_t mutex)
#endif
	{
		// Wait until event is in signaled (green) state
#ifdef WIN32
		WaitForSingleObject (m_Handle, INFINITE);
#elif linux
		pthread_cond_wait(&m_Handle, &mutex);
#endif
	}
}