using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;


namespace StiNeResults
{
    class Program
    {
        private static FirefoxDriver _driver;
        private static WebDriverWait _wait;
        private static Config        _config;

        static void Main (string [] args)
        {
            _config = GetConfig ();

            if (!Start ())
                return;
            if (!Init ())
                return;
            if (!Login ())
                return;
            if (!Navigate ())
                return;

            AnalyzeErgebnisse ();
        }

        private static bool Start ()
        {
            for (var i = 0; i < _config.Tries; i++)
            {
                try
                {
                    _driver?.Close ();
                    _driver?.Dispose ();
                    _driver = new FirefoxDriver ("./");
                    _wait   = new WebDriverWait (_driver, TimeSpan.FromSeconds (30));
                    _driver.Manage ()?.Window?.Maximize ();
                    if (_driver.Manage ()?.Timeouts () != null)
                        _driver.Manage ().Timeouts ().ImplicitWait = TimeSpan.FromMilliseconds (20);

                    Console.WriteLine ("Started");
                    return true;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine ($"Start threw an exception: {e}, {e.StackTrace}");
                }

                Console.Error.WriteLine ($"Couldn't start, try {i}/{_config.Tries}");
            }

            return false;
        }

        private static bool Init ()
        {
            for (var i = 0; i < _config.Tries; i++)
            {
                try
                {
                    _driver.Navigate ().GoToUrl ("https://www.stine.uni-hamburg.de/");
                    DismissDialog ();
                    _wait.Until (driver => driver.Title.StartsWith ("Universität Hamburg"));
                    if (_driver.Title.StartsWith ("Universität Hamburg"))
                    {
                        Console.WriteLine ("Initialized");
                        return true;
                    }

                    Console.Error.WriteLine ($"Page called '{_driver.Title}'");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine ($"Init threw an exception: {e}, {e.StackTrace}");
                }

                Console.Error.WriteLine ($"Couldn't init, try {i + 1}/{_config.Tries}");
            }

            Console.Error.WriteLine ("Couldn't init");

            return false;
        }

        private static bool Login ()
        {
            for (var i = 0; i < _config.Tries; i++)
            {
                if (LoginTry ())
                {
                    Console.WriteLine ("Authenticated");
                    return true;
                }

                Init ();
            }

            return false;

            static bool LoginTry ()
            {
                try
                {
                    var user = _driver.FindElement (By.Id ("field_user"));
                    if (user == null)
                    {
                        Console.Error.WriteLine ("User field not found");
                        return false;
                    }

                    user.SendKeys (_config.User);

                    var password = _driver.FindElement (By.Id ("field_pass"));
                    if (password == null)
                    {
                        Console.Error.WriteLine ("Password field not found");
                        return false;
                    }

                    password.SendKeys (_config.Password);

                    var login = _driver.FindElement (By.Id ("logIn_btn"));
                    if (login == null)
                    {
                        Console.Error.WriteLine ("Login button not found");
                        return false;
                    }

                    login.Click ();
                    _wait.Until (driver => driver.FindElements (By.Id ("field_user")).Count == 0);
                    if (_driver.FindElements (By.Id ("field_user")).Count == 0)
                        return true;

                    Console.Error.WriteLine ("Login unsuccessful");
                    return false;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine ($"Login threw an exception: {e}, {e.StackTrace}");
                    return false;
                }
            }
        }

        private static void DismissDialog ()
        {
            try
            {
                if (_driver.SwitchTo ().Alert () != null)
                    _driver.SwitchTo ().Alert ().Accept ();
            }
            catch (Exception e)
            {
                Console.WriteLine ("No dialog to dismiss");
                Debug.WriteLine ($"Dismissing the dialog threw an exception: {e.Message}, {e.StackTrace}");
            }
        }

        private static Config GetConfig ()
        {
            if (File.Exists ("config"))
            {
                var content = File.ReadAllLines ("config");
                if (content.Length < 2)
                    Console.Error.WriteLine ("Invalid config file");
                else
                {
                    var tries = 10;
                    if (content.Length > 2 && !int.TryParse (content [2], out tries))
                        Console.Error.WriteLine ("Invalid tries parameter");
                    else
                        return new Config
                        {
                            User     = content [0],
                            Password = content [1],
                            Tries    = tries
                        };
                }
            }

            Console.WriteLine ("Enter username");
            var username = Console.ReadLine ();
            Console.WriteLine ("Enter password");
            var password = Console.ReadLine ();

            return new Config {User = username, Password = password};
        }

        private static bool Navigate ()
        {
            for (var i = 0; i < _config.Tries; i++)
            {
                if (NavigateTry ())
                {
                    Console.WriteLine ("On tab");
                    return true;
                }

                Console.WriteLine ($"Navigate failed, try {i + 1}/{_config.Tries}");
                try
                {
                    _driver.Navigate ().Refresh ();
                    DismissDialog ();
                }
                catch (Exception e)
                {
                    Console.WriteLine ($"Refresh threw an exception: {e}, {e.StackTrace}");
                    return false;
                }
            }

            Console.WriteLine ("Navigate failed");
            return false;

            static bool NavigateTry ()
            {
                try
                {
                    var studium = _driver.FindElement (By.LinkText ("Studium"));
                    if (studium == null)
                    {
                        Console.Error.WriteLine ("Studium tab not found");
                        return false;
                    }

                    studium.Click ();
                    _wait.Until (driver => _driver.FindElements (By.LinkText ("Prüfungsergebnisse")).Count != 0);

                    var prüfungsergebnisse = _driver.FindElement (By.LinkText ("Prüfungsergebnisse"));
                    if (prüfungsergebnisse == null)
                    {
                        Console.Error.WriteLine ("Prüfungsergebnisse button not found");
                        return false;
                    }

                    prüfungsergebnisse.Click ();
                    return true;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine ($"Navigate threw an exception: {e}, {e.StackTrace}");
                    return false;
                }
            }
        }

        private static void AnalyzeErgebnisse ()
        {
            int?               initialErgebnisCount = null;
            List <IWebElement> ergebnisse           = null;
            do
                try
                {
                    _wait.Until (driver => _driver.FindElements (By.TagName ("tr")).Count != 0);
                    ergebnisse = _driver.FindElements (By.TagName ("tr")).Skip (1).ToList ();
                    if (ergebnisse.Count == 0)
                        throw new Exception ("Invalid tab");
                    initialErgebnisCount ??= ergebnisse.Count;
                    Console.WriteLine ($"Got {ergebnisse.Count} Ergebnisse");
                    foreach (var cols in ergebnisse.Select (ergebnis => ergebnis.FindElements (By.TagName ("td"))))
                    {
                        if (cols.Count != 5)
                        {
                            Console.Error.WriteLine ($"Invalid column count: {cols.Count}");
                            continue;
                        }

                        Console.WriteLine ($"Ergebnis für '{cols [0].Text}': {cols [2].Text}");
                    }

                    _driver.Navigate ().Refresh ();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine ($"Ergebnisse threw an exception: {e}, {e.StackTrace}");
                    if (!(Init () && Login () && Navigate ()))
                        return;
                }
            while (ergebnisse == null || ergebnisse.Count <= initialErgebnisCount);

            Console.WriteLine ("Got new Ergebnis!");
            Notify ();
        }

        private static void Notify ()
        {
            Console.Beep (440, 10000);
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write ("█");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write ("█");
            }
        }
    }
}