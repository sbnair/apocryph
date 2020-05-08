using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Runtime.FunctionApp.Ipfs;
﻿using Ipfs;
﻿using Ipfs.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace Apocryph.Runtime.FunctionApp.Ipfs
{
    public static class IpfsRecursiveLoader
    {
        public class State
        {
            public HashSet<Cid> ResolvedHashes { get; set; } = new HashSet<Cid> {};
        }

        [FunctionName(nameof(IpfsRecursiveLoader))]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("ipfsGateway")] string ipfsGateway,
            [PerperStream("hashStream")] IAsyncEnumerable<Cid> hashStream,
            [PerperStream("outputStream")] IAsyncCollector<IHashed<object>> outputStream,
            ILogger logger)
        {
            var ipfs = new IpfsClient(ipfsGateway);
            var state = await context.FetchStateAsync<State>() ?? new State();

            async Task processHash(Cid hash, CancellationToken cancellationToken)
            {
                if (state.ResolvedHashes.Contains(hash))
                {
                    return;
                }

                state.ResolvedHashes.Add(hash);
                var jToken = await ipfs.Dag.GetAsync(hash, cancellationToken);
                var item = IpfsJsonSettings.ObjectFromJToken<object>(jToken);

                // if (item is AgentBlock agentStep)
                // {
                //     await processHash(agentStep.Previous, cancellationToken);
                // }

                var hashed = Hashed.Create(item, hash);

                await outputStream.AddAsync(hashed);
            };

            await hashStream.ForEachAsync(async hash =>
            {
                try
                {
                    // NOTE: Currently blocks other items on the stream and does not process them
                    await processHash(hash, CancellationToken.None);

                    await context.UpdateStateAsync(state);
                }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                }
            }, CancellationToken.None);
        }
    }
}