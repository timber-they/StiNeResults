using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace StiNeResults
{
    class Program
    {
        static void Main(string[] args)
        {
            var driver = new FirefoxDriver("./");
            driver.Navigate().GoToUrl("https://www.stine.uni-hamburg.de/");
            if (!driver.Title.StartsWith("Universität Hamburg"))
            {
                Console.Error.WriteLine($"Page called '{driver.Title}', won't proceed");
                return;
            }

            var user = driver.FindElement(By.Id("field_user"));
            if (user == null)
            {
                Console.Error.WriteLine("User field not found");
                return;
            }
            user.SendKeys("baw8431");

            var password = driver.FindElement(By.Id("field_pass"));
            if (password == null)
            {
                Console.Error.WriteLine("Password field not found");
                return;
            }

            string pwd;
            if (File.Exists("password"))
                pwd = File.ReadAllText("password");
            else
            {
                Console.WriteLine("Enter password");
                pwd = Console.ReadLine();
            }
            password.SendKeys(pwd);

            var login = driver.FindElement(By.Id("logIn_btn"));
            if (login == null)
            {
                Console.Error.WriteLine("Login button not found");
                return;
            }
            login.Click();

            Console.WriteLine("Authenticated");

            var studium = driver.FindElement(By.LinkText("Studium"));
            if (studium == null)
            {
                Console.Error.WriteLine("Studium tab not found");
                return;
            }
            studium.Click();

            var prüfungsergebnisse = driver.FindElement(By.LinkText("Prüfungsergebnisse"));
            if (prüfungsergebnisse == null)
            {
                Console.Error.WriteLine("Prüfungsergebnisse button not found");
                return;
            }
            prüfungsergebnisse.Click();

            Console.WriteLine("On tab");

            List<IWebElement> ergebnisse;
            do
            {
                ergebnisse = driver.FindElements(By.TagName("tr")).Skip(1).ToList();
                Console.WriteLine($"Got {ergebnisse.Count} Ergebnisse");
                foreach (var ergebnis in ergebnisse)
                {
                    var cols = ergebnis.FindElements(By.TagName("td"));
                    if (cols.Count != 5)
                    {
                        Console.Error.WriteLine($"Invalid column count: {cols.Count}");
                        continue;
                    }

                    Console.WriteLine($"Ergebnis für '{cols[0].Text}': {cols[2].Text}");
                }
                driver.Navigate().Refresh();
            } while (ergebnisse.Count == 3);

            Console.WriteLine("Got new Ergebnis!");
            Console.Beep(440, 10000);
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("█");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("█");
            }
        }
    }
}