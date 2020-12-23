#include "ActiveObject.h"

namespace threading
{
	// The constructor of the derived class
	// should call
	//    _thread.Resume ();
	// at the end of construction

	ActiveObject::ActiveObject ()
		: m_IsDying (0),
	#pragma warning(disable: 4355) // 'this' used before initialized
		  m_Thread (ThreadEntry, this)
	#pragma warning(default: 4355)
	{
	}

	ActiveObject::~ActiveObject(void)
	{
	}

	void ActiveObject::Kill ()
	{
		m_IsDying++;
		FlushThread ();
		// Let's make sure it's gone
		m_Thread.WaitForDeath ();
	}

#ifdef WIN32
	unsigned long WINAPI ActiveObject::ThreadEntry (void* pThis)
#elif linux
	void* ActiveObject::ThreadEntry (void* pThis)
#endif
	{
		ActiveObject * pActive = (ActiveObject *) pThis;
		pActive->InitThread ();
		pActive->Run ();
#ifdef WIN32
		return 0;
#endif
	}
}