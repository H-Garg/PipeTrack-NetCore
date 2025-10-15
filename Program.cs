using System;

namespace HelloDotNet
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to .NET!");
            Console.WriteLine("Enter your name:");
            string name = Console.ReadLine();

            Console.WriteLine($"Hello, {name}! This is your first .NET program.");
            
            // Wait for user before closing
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
