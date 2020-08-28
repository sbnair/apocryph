using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.Communication;
using Apocryph.Core.Consensus.Serialization;
using Apocryph.Core.Consensus.VirtualNodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Apocryph.Runtime.FunctionApp
{
    public class ReportsStream
    {
        private Dictionary<Node, Dictionary<Type, Report>> _reports = new Dictionary<Node, Dictionary<Type, Report>>();
        private Dictionary<Guid, Node?[]>? _nodes;
        private Dictionary<Guid, Block> _blocks = new Dictionary<Guid, Block>();

        [FunctionName(nameof(ReportsStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("nodes")] Dictionary<Guid, Node?[]> nodes,
            [Perper("chain")] IAsyncEnumerable<Message<(Guid, Node?[])>> chain,
            [Perper("filter")] IAsyncEnumerable<Block> filter,
            [Perper("reports")] IAsyncEnumerable<Report> reports,
            CancellationToken cancellationToken)
        {
            _nodes = nodes;

            await TaskHelper.WhenAllOrFail(
                RunServer(cancellationToken),
                HandleChain(chain, cancellationToken),
                HandleFilter(filter, cancellationToken),
                HandleReports(reports, cancellationToken));
        }

        private async Task HandleFilter(IAsyncEnumerable<Block> filter, CancellationToken cancellationToken)
        {
            await foreach (var block in filter)
            {
                _blocks[block.ChainId] = block;
            }
        }

        private async Task HandleChain(IAsyncEnumerable<Message<(Guid, Node?[])>> chain, CancellationToken cancellationToken)
        {
            await foreach (var message in chain.WithCancellation(cancellationToken))
            {
                var (chainId, nodes) = message.Value;

                _nodes![chainId] = nodes;
            }
        }

        private async Task HandleReports(IAsyncEnumerable<Report> reports, CancellationToken cancellationToken)
        {
            await foreach (var report in reports)
            {
                if (!_reports.ContainsKey(report.Source))
                {
                    _reports[report.Source] = new Dictionary<Type, Report>();
                }
                _reports[report.Source][report.GetType()] = report;
            }
        }

        private Task RunServer(CancellationToken cancellationToken)
        {
            var host = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://localhost:5001");
                webBuilder.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/chain", WrapEndpoint(() => _nodes!.Keys.ToList()));
                        endpoints.MapGet("/chain/{Id:guid}", WrapEndpoint((values) =>
                        {
                            var id = new Guid((string)values["Id"]);
                            return _blocks[id];
                        }));
                        endpoints.MapGet("/chain/{Id:guid}/node", WrapEndpoint((values) =>
                        {
                            var id = new Guid((string)values["Id"]);
                            return _nodes![id];
                        }));
                        endpoints.MapGet("/chain/{Id:guid}/node/{Index:int}", WrapEndpoint((values) =>
                        {
                            var id = new Guid((string)values["Id"]);
                            var index = int.Parse((string)values["Index"]);
                            var node = new Node(id, index);
                            return _reports[node];
                        }));
                        endpoints.MapGet("/node", WrapEndpoint((values) =>
                        {
                            return _reports.Keys.ToList();
                        }));
                    });
                });
            }).Build();

            return host.RunAsync(cancellationToken);
        }

        private RequestDelegate WrapEndpoint(Func<RouteValueDictionary, object> wrapped)
        {
            return (context) => context.Response.WriteJsonAsync(wrapped(context.Request.RouteValues));
        }

        private RequestDelegate WrapEndpoint(Func<object> wrapped)
        {
            return (context) => context.Response.WriteJsonAsync(wrapped());
        }
    }

    internal static class ReportsStreamExtensions
    {

        public static Task WriteJsonAsync(this HttpResponse response, object value)
        {
            var json = JsonSerializer.Serialize(value, ApocryphSerializationOptions.JsonSerializerOptions);
            return response.WriteAsync(json);
        }
    }
}