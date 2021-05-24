using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPserver
{
    class Player
    {
        public string ip;
        public string name;

        public Player(string ip, string name)
        {
            this.ip = ip;
            this.name = name;
        }
    }
}
