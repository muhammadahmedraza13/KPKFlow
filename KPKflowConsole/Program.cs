using KPKflowConsole.Logs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;


namespace KPKflowConsole
{
    public class Program
    {
        static bool isCompleted = true;

        public static string getAPIBaseUrl(string configKey) => ConfigurationManager.AppSettings[configKey];

        static void Main(string[] args)
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine("\r\nThe service will run automatically in 60 seconds. Please do not close this window.\r\n");
            Console.WriteLine($"[START] Application started at {startTime}");
            Log.WriteLog($"[START] Application started at {startTime}");

            Timer timer = new Timer(TimerCallback, null, 0, 1000 * 60 );
            Console.Read();
        }

        private static void TimerCallback(Object o)
        {
            var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (isCompleted)
            {
                isCompleted = false;

                Console.WriteLine($"[TIMER TRIGGERED] {currentTime} - Starting Auto Procurement process...");
                Log.WriteLog($"[TIMER TRIGGERED] {currentTime} - Starting Auto Procurement process...");

                try
                {
                    Auto_Procurement();

                    Console.WriteLine($"[SUCCESS] {currentTime} - Document Auto Procurement process completed successfully.");
                    Log.WriteLog($"[SUCCESS] {currentTime} - Document Auto Procurement process completed successfully.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[ERROR] {currentTime} - Exception in TimerCallback: {e.Message}");
                    Log.WriteLog($"[ERROR] {currentTime} - Exception in TimerCallback: {e.Message}");
                }

                Console.WriteLine($"[TIMER END] {currentTime} - Ready for next cycle.");
                Log.WriteLog($"[TIMER END] {currentTime} - Ready for next cycle.");

                isCompleted = true;
            }
            else
            {
                Console.WriteLine($"[SKIPPED] {currentTime} - Previous job still running.");
                Log.WriteLog($"[SKIPPED] {currentTime} - Previous job still running.");
            }
        }

        public static void Auto_Procurement()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                Console.WriteLine($"[Auto Procurement START] {startTime} - Initializing Auto Procurement process...");
                Log.WriteLog($"[Auto Procurement START] {startTime} - Initializing Auto Procurement process...");

                CookieContainer cookies;
                var client = LoginWithCookie(out cookies);

                if (client == null)
                {
                    Console.WriteLine($"[Auto Procurement ABORTED] {startTime} - Login failed, cannot proceed.");
                    Log.WriteLog($"[Auto Procurement ABORTED] {startTime} - Login failed, cannot proceed.");
                    return;
                }

                var baseUrl = getAPIBaseUrl("apiBaseURL").TrimEnd('/');
                client.DefaultRequestHeaders.Referrer = new Uri($"{baseUrl}/Home/Index");

                Console.WriteLine($"[API CALL] {startTime} - Requesting files for Auto Procurement...");
                Log.WriteLog($"[API CALL] {startTime} - Requesting files for Auto Procurement...");

                var response = client.GetAsync("Base/AutoProcurement").Result;

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[API SUCCESS] {startTime} - Retrieved file(s) and Auto Procurement successfully done!.");
                    Log.WriteLog($"[API SUCCESS] {startTime} -  Retrieved file(s) and Auto Procurement successfully done!.");
                }
                else
                {
                    Console.WriteLine($"[API FAILED] {startTime} - Status: {response.StatusCode}");
                    Log.WriteLog($"[API FAILED] {startTime} - Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Auto Procurement ERROR] {startTime} - {ex.Message}");
                Log.WriteLog($"[Auto Procurement ERROR] {startTime} - {ex.Message}");
            }
        }

        public static HttpClient LoginWithCookie(out CookieContainer cookies)
        {
            cookies = new CookieContainer();
            var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                Console.WriteLine($"[LOGIN] {currentTime} - Attempting login...");
                Log.WriteLog($"[LOGIN] {currentTime} - Attempting login...");

                var handler = new HttpClientHandler
                {
                    CookieContainer = cookies,
                    UseCookies = true,
                    AllowAutoRedirect = true
                };

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(getAPIBaseUrl("apiBaseURL"));

                var formData = new Dictionary<string, string>
                {
                    { "LoginName", "bidforwarder" },
                    { "Password", "Abcd@1234" },
                    { "RememberMe", "false" }
                };

                var content = new FormUrlEncodedContent(formData);

                var response = client.PostAsync("Login/UserLogin", content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[LOGIN FAILED] {currentTime} - Status: {response.StatusCode}");
                    Log.WriteLog($"[LOGIN FAILED] {currentTime} - Status: {response.StatusCode}");
                    return null;
                }

                Console.WriteLine($"[LOGIN SUCCESS] {currentTime} - Authenticated successfully.");
                Log.WriteLog($"[LOGIN SUCCESS] {currentTime} - Authenticated successfully.");

                return client;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN ERROR] {currentTime} - {ex.Message}");
                Log.WriteLog($"[LOGIN ERROR] {currentTime} - {ex.Message}");
                return null;
            }
        }
        public static HttpClient Login()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer(),
                    UseCookies = true,
                    AllowAutoRedirect = true
                };

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(getAPIBaseUrl("apiBaseURL"));

                var loginObj = new
                {
                    Email = "console@example.com",
                    LoginName = "console",
                    Password = "Admin@123",
                    RememberMe = "false"
                };

                var loginContent = new StringContent(
                    JsonConvert.SerializeObject(loginObj),
                    Encoding.UTF8,
                    "application/json"
                );

                var loginResponse = client.PostAsync("Login/ConsoleLogin", loginContent).Result;

                if (!loginResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("Login failed!");
                    return null;
                }

                Console.WriteLine("Login successful!");
                return client; 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Log.WriteLog(ex.Message);
                return null;
            }
        }

    }
}
