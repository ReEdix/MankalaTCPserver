namespace TCPserver
{
    class Match
    {
        public string playerWhite;
        public string playerBlack;

        public Match(string playerWhite)
        {
            this.playerWhite = playerWhite;
            this.playerBlack = null;
        }
    }
}
