using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.KeyUtility
{
    public static class ConsolePasswordReader
    {
        public static string ReadPassword()
        {
            string password = "";
            // From: https://stackoverflow.com/questions/3404421/password-masking-console-application
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                }
            } while (true);

            return password;
        }
    }
}
