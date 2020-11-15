// Sample by: harveytriana@gmail.com
//
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleClient
{
    class Program
    {
        static HubConnection _connection;

        static void Main()
        {
            Console.WriteLine("STREAMING TEST\n");
            Console.WriteLine("Run as Multiple startup projects");
            Console.WriteLine("Press Enter key when server is ready");
            Console.ReadKey();
            Console.Clear();

            var loggerFactory = LoggerFactory.Create(builder => {
                builder.AddConsole();
            });
            ILogger logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("Start Thread");

            var cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => MainAsync(cancellationTokenSource.Token, logger).Wait());

            Console.WriteLine("\nPress any key to end or cancel the thread ...");
            Console.ReadKey();

            // cancel the thread if this is not over
            cancellationTokenSource.Dispose();

            // close signalr client
            _connection.StopAsync().Wait();
        }

        static async Task MainAsync(CancellationToken cancellationToken, ILogger logger)
        {
            var hubUrl = "http://localhost:9000/streamHub";
            try {
                _connection = new HubConnectionBuilder()
                     .WithUrl(hubUrl)
                     .Build();

                await _connection.StartAsync();

                // QUESTION:
                // https://github.com/dotnet/AspNetCore.Docs/issues/20562
                // await? raise exception: 'IAsyncEnumerable<int>' does not contain a definition for 'GetAwaiter'
                // 
                var stream = _connection.StreamAsync<int>("CounterEnumerable", 12, 333, cancellationToken);

                await foreach (var count in stream) {
                    logger.LogInformation($"Received {count}");
                }
                logger.LogInformation("Completed");
            }
            catch (Exception exception) {
                logger.LogError(exception.Message);
            }

        }
    }
}