using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Apocryph.FunctionApp.Model;
﻿using Ipfs;
﻿using Ipfs.Http;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace Apocryph.FunctionApp.Ipfs
{
    public static class Loader
    {
        [FunctionName("IpfsLoader")]
        public static async Task Run([Perper(Stream = "IpfsOutput")] IPerperStreamContext context,
            [Perper("ipfsGateway")] string ipfsGateway,
            [Perper("hashStream")] IAsyncEnumerable<Hash> hashStream,
            [Perper("outputStream")] IAsyncCollector<Hashed<object>> outputStream)
        {
            var ipfs = new IpfsClient(ipfsGateway);

            await hashStream.Listen(async hash => {
                // NOTE: Currently blocks other items on the stream and does not process them
                // -- we should at least timeout
                // FIXME: Should use DAG/IPLD API instead
                var block = await ipfs.Block.GetAsync(Cid.Read(hash.Bytes), CancellationToken.None);

                var item = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(block.DataBytes));

                await outputStream.AddAsync(new Hashed<object>(item, hash));
            }, CancellationToken.None);
        }
    }
}