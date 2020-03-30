using Apocryph.FunctionApp.Agent;
using System;
using Wetonomy.State.TokenActionAgents;
using Wetonomy.TokenActionAgents.Messages;
using Wetonomy.TokenActionAgents.Publications;
using Wetonomy.TokenManager.Messages;

namespace Wetonomy.TokenActionAgents
{
    class TokenSplitterAgent<T> where T: IEquatable<T>
    {
        public class TokenSolitterState : RecipientState<T>
        {}
        public static void Run(IAgentContext<TokenSolitterState> context, string sender, object message)
        {

            if (message is AbstractTriggerer msg && context.State.TriggererToAction.ContainsKey((sender, message.GetType())))
            {
                var result = RecipientState<T>.TriggerCheck(context.State, sender, msg);

                foreach (TransferTokenMessage<T> action in result)
                {
                    context.SendMessage(null, action, null);
                }

                return;
            }

            switch (message)
            {
                case AddRecipientMessage<T> addMessage:
                    if (context.State.AddRecipient(addMessage.Recipient))
                    {
                        context.MakePublication(new RecipientAddedPublication<T>(addMessage.Recipient));
                    }
                    break;

                case RemoveRecipientMessage<T> removeMessage:
                    if (context.State.RemoveRecipient(removeMessage.Recipient))
                    {
                        context.MakePublication(new RecipientRemovedPublication<T>(removeMessage.Recipient));
                    }
                    break;
            }
        }
    }
}
