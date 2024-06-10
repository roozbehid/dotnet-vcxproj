call emsdk_env.bat 1>nul 2>nul 
emcc.bat   -DWIN32 -D_DEBUG -D_CONSOLE -Wall -Wno-comment -Wno-parentheses -Wno-missing-braces -Wno-write-strings -Wno-unknown-pragmas -Wno-attributes  -fpermissive -x c++ -g0  -m32  -w -MM HellowWorld.cpp
