using System;

namespace TinyJitHook.Core.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Main2();
        }
        static void Main2()
        {
            try
            {
                Console.WriteLine("Hello World2!");
                Main3();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error! {ex}");
            }
        }
        static void Main3()
        {
            Console.WriteLine("Hello World3!");
        }
    }
}
