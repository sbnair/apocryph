using System;

namespace Apocryph.Agent
{
    [Obsolete]
    public class AgentCommands
    {
        public string Origin;

        public object State { get; set; }
        public AgentCommand[] Commands { get; set; }
    }
}