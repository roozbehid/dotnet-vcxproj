GCCTask
======

A XBuild/MSBuild task to compile C sources by Konrad Kruczyński.

What is it?
-----------
So, at this moment this is just more or less a construction site ;) What I like to have here in the future is the XBuild/MSBuild task that enables you to compile C sources on Linux, Windows or Mac. So, for example, if you have C# project that uses native binaries, you can compile it within xbuild. That's all ;)

What it can do now?
-------------------
Currently the task is **Linux only**. This is my usually used environment, so I've started there. The task is able to compile simple C programs. You provide list of sources, flags and output name, the task compiles (look at the example). Moreover:

* Object files (.o) that are created during compilation are in the ``buildcache_{output_file_name}`` subdirectory of the directory that the contains mentioned output. They are reused on recompilation if possible. In other words if you have three source files, compile, then modify one of them, only one is recompiled (also the linking process happens again). So, this works as ``make`` with the difference that ``make`` uses modification dates and I'm using hashes of the sources (also stored in the buildcache directory). Props to Peter Giełda for the idea.
* Compilation is done using all your cores. Because compilation of each translation unit is independent from the other ones, we can compile them in parallel. This is what I do; again, this is more or less what ``make`` does with the ``-j`` switch, here it is done by default and automatically.
* ``gcc -MM`` is used to obtain dependencies for source files (or rather for object files), header files in particular. So yes, if you modify a header file, all files including that header will be rebuilt.

Example
-------
You can find sample project (three C files compiled to nothing-doing program) in the ``Testing project`` subdirectory. Go to that directory and run:

.. code::

   xbuild test.proj

and see what happens.

Want more?
----------
Write an issue, do a pull request.

Licence
-------
The project uses the MIT licence as you can tell from the source code.
