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
            Console.WriteLine("Hello World! Printed from CS code.");
            PrintfFromDynamicLib();
            Console.WriteLine("Hello World! Ended from CS code.");
        }
    }
}
