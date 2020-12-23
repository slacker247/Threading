#include "Thread.h"

#pragma once

namespace threading
{
	class ActiveObject
	{
	public:

		ActiveObject(void);
		virtual ~ActiveObject(void);

		void Kill();

	protected:
		virtual void InitThread() = 0;
		virtual void Run() = 0;
		virtual void FlushThread() = 0;

#ifdef WIN32
		static unsigned long WINAPI ThreadEntry(void *pThis);
#elif linux
		static void* ThreadEntry(void *pThis);
#endif

		int m_IsDying;
		Thread m_Thread;
	};
}