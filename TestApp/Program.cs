using System;
using System.Collections.Generic;
using ATriggerLib;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ATrigger Test.");

            const string API_KEY = "YOUR API KEY";
            const string API_SECRET = "YOUR API SECRET";
            const string TARGET_URL = "the url";

            //ref: http://atrigger.com/docs/wiki/13/library-net
            ATrigger.Initialize(API_KEY, API_SECRET, false, true);
            var tags = new Dictionary<string, string>
            {
                { "type", "test" }
            };
            ATrigger.Client.DoCreate(TimeQuantity.Day(), "1", TARGET_URL, tags);

            Console.WriteLine("If it worked, you would see a new created task on aTrigger backend.");
            Console.ReadKey();
        }
    }
}