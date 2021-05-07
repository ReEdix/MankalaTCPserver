namespace TCPserver
{
    public static class Messages
    {
        public static class Server
        {
            public const string OK = "###__OK";
            public const string Cancel = "###__Cancel";
            public const string Disconnect = "###__Disconnect";
            public const string Move = "###__Move";
            public const string Start = "###__Start";
        }

        public class Client
        {
            public const string Host = "###__Host";
            public const string Join = "###__Join";
            public const string Move = "###__Move";
            public const string Exit = "###__Exit";
        }
    }
}
