// DynamicLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "DynamicLib.h"


	MATHLIBRARY_API void PrintfFromDynamicLib() {
		printf("Called from Dynamic Library.\n");
	}


