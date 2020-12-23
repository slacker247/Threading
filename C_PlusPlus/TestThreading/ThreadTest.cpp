#include "ThreadTest.h"

ThreadTest::ThreadTest(void)
{
	m_Thread.Resume();
}

ThreadTest::~ThreadTest(void)
{
	Kill();
}

void ThreadTest::Run()
{
    for (;;)
    {
        if (m_IsDying)
            return;

		// **** DO WORK **** //
		printf("%d: Doing work..\n", this->m_Thread.getThreadId());
		int a = 0;
		for(int i = 0; i < 50000; i++)
		{
			a = i + 14 * a;
		}
		this->Kill();
		printf("%d: Finished work..\n", this->m_Thread.getThreadId());
		// **** DO WORK **** //
    }
}