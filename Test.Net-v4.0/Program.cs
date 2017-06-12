using Lambda2Js.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace Test.Net_v4._0
{
    class Program
    {
        static void Main(string[] args)
        {
            Assert.Default = new MyAssert();

            Console.WriteLine("Tests with .NET 4.0");

            var allTestClasses = typeof(GeneralTests).Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TestClassAttribute>().Any())
                .ToArray();

            foreach (var t in allTestClasses)
            {
                var methods = t.GetMethods()
                    .Where(m => m.GetCustomAttributes<TestMethodAttribute>().Any())
                    .ToArray();

                var obj = Activator.CreateInstance(t);

                foreach (var mi in methods)
                {
                    bool ok = false;
                    try
                    {
                        mi.Invoke(obj, null);
                        ok = true;
                    }
                    catch (Exception ex)
                    {
                        ok = mi.GetCustomAttributes<ExpectedException>().Where(x => x.Accept(ex)).Any();

                        if (!ok)
                        {
                            var prev = Console.ForegroundColor;
                            Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
                            Console.Write(ok ? "  OK  " : " FAIL ");
                            Console.ForegroundColor = prev;
                            Console.WriteLine($"{ex.GetType().Name}");
                        }
                    }
                }
            }

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }
    }
}
