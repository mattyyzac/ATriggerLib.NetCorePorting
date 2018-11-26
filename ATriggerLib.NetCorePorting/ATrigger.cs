namespace ATriggerLib
{
    public class ATrigger
    {
        private const string _apiServerDefault = "https://api.atrigger.com/v1/";
        private const int _apiTimeoutDefault = 5000;
        private const bool _debugDefault = false;
        private const bool _asyncDefault = true;

        private static readonly object padlock = new object();

        public static ClientKernel Client { get; private set; }

        public static void Initialize(string key, string secret, bool Async = true, bool Debug = false, int APITimeout = 5000, string APIServer = "https://api.atrigger.com/v1/")
        {
            lock (padlock)
            {
                if (Client == null)
                {
                    Client = new ClientKernel(key, secret, Async, Debug, APITimeout, APIServer);
                }
            }
        }
    }
}