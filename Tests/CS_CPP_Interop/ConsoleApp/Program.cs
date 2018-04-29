using System;
using System.Runtime.InteropServices;

namespace ConsoleApp
{
    class Program
    {

        [DllImport("DynamicLib")]
        static extern void PrintfFromDynamicLib();
        static void Main(string[] args)
        {
            PrintfFromDynamicLib();
            Console.WriteLine("Hello World!");
        }
    }
}
