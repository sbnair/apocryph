using System;

namespace Apocryph.Agents.Testbed.Api
{
    public class AgentCommand
    {
        public AgentCommandType CommandType { get; set; }

        public string AgentId { get; set; }
        public string Agent { get; set; }

        public AgentCapability Receiver { get; set; }

        public object Message { get; set; }
        public object State { get; set; }
    }

    public enum AgentCommandType
    {
        CreateAgent,
        SendMessage,
    }
}