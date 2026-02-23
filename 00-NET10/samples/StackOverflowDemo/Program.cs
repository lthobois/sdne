using System;

static class Program
{
    static void Recursive() => Recursive();

    static void Main()
    {
        Console.WriteLine("Debut recursion infinie...");
        Recursive();
    }
}
