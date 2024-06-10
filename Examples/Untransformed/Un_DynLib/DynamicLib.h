#pragma once

#if defined(__GNUC__)
#ifdef DYNAMICLIB_EXPORTS  
#define MATHLIBRARY_API __attribute__((visibility("default")))   
#else  
#define MATHLIBRARY_API 
#endif 
#else
	#ifdef DYNAMICLIB_EXPORTS  
	#define MATHLIBRARY_API __declspec(dllexport)   
	#else  
	#define MATHLIBRARY_API __declspec(dllimport)   
	#endif 
#endif


MATHLIBRARY_API void PrintfFromDynamicLib();