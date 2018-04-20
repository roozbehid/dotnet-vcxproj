#pragma once

#ifdef DYNAMICLIB_EXPORTS  
#define MATHLIBRARY_API __declspec(dllexport)   
#else  
#define MATHLIBRARY_API __declspec(dllimport)   
#endif 

MATHLIBRARY_API void PrintfFromDynamicLib();