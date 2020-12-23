#include <stdio.h>

#include "../Threading/ActiveObject.h"

using namespace threading;

#pragma once

class ThreadTest : public ActiveObject
{
public:
	ThreadTest(void);
	~ThreadTest(void);
protected:
	void InitThread(){;};
	void Run();
	void FlushThread(){;};
};
