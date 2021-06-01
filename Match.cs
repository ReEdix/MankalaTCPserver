namespace TCPserver
{
    class Match
    {
        public Player playerWhite;
        public Player playerBlack;

        public Match(Player playerWhite)
        {
            this.playerWhite = playerWhite;
            this.playerBlack = null;
        }
    }
}
