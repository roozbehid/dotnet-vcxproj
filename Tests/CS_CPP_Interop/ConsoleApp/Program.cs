using System;
using System.Runtime.InteropServices;

namespace ConsoleApp
{
    class Program
    {
        [DllImport("DynamicLib.dll")]
        static extern void PrintfFromDynamicLib();
        static void Main(string[] args)
        {
            PrintfFromDynamicLib();
            Console.WriteLine("Hello World!");
        }
    }
}
