using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Agent;
using Apocryph.Runtime.FunctionApp.Consensus;
using Apocryph.Runtime.FunctionApp.Consensus.Core;
using Apocryph.Runtime.FunctionApp.Ipfs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Apocryph.Runtime.FunctionApp.Communication
{
    public static class Runtime
    {
        [FunctionName(nameof(Runtime))]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("self")] ValidatorKey self,
            [PerperStream("inputStream")] IAsyncEnumerable<IHashed<Block>> inputStream,
            [PerperStream("outputStream")] IAsyncCollector<Block> outputStream,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            await inputStream.ForEachAsync(async input =>
            {
                try
                {
                    // var agentContext = await context.CallWorkerAsync<AgentContext<object>>(nameof(RuntimeWorker), new
                    // {
                    //     state = input.Value.State,
                    //     sender = new AgentCapability(input.Value.Sender),
                    //     message = input.Value.Message
                    // }, cancellationToken);
                    //
                    // await outputStream.AddAsync(new AgentOutput
                    // {
                    //     Previous = input.Hash,
                    //     State = agentContext.State,
                    //     Commands = agentContext.Commands
                    // }, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                }
            }, cancellationToken);
        }
    }
}