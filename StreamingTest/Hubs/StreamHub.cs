//
//
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

//! BASIC SOURCE
// https://docs.microsoft.com/en-us/aspnet/core/signalr/streaming

namespace StreamingTest.Hubs
{
    public class StreamHub : Hub
    {
        private readonly ILogger<StreamHub> _logger;

        public StreamHub(ILogger<StreamHub> logger)
        {
            _logger = logger;
        }

        #region Server-to-client streaming
        // hub method becomes a streaming hub method when it returns IAsyncEnumerable<T>, ChannelReader<T>
        // or async versions
        //
        public ChannelReader<int> CounterChannel(
                   int count,
                   int delay,
                   CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Run ChannelReader<int> CounterChannel(count: {count}, delay: {delay})");

            var channel = Channel.CreateUnbounded<int>();

            Exception localException = null;

            Task.Run(async () => {
                try {
                    for (var i = 0; i < count; i++) {
                        // Use the cancellationToken in other APIs that accept cancellation
                        // tokens so the cancellation can flow down to them.
                        // cancellationToken.ThrowIfCancellationRequested();

                        // ChannelWriter sends data to the client
                        await channel.Writer.WriteAsync(i, cancellationToken);

                        await Task.Delay(delay, cancellationToken);
                    }
                    channel.Writer.Complete();
                }
                catch (Exception exception) {
                    localException = exception;
                }
                finally {
                    channel.Writer.Complete(localException);
                }
            });
            return channel.Reader;
        }

        // Second apprach. IAsyncEnumerable<T> ... Server Application using Async Streams
        //
        public async IAsyncEnumerable<int> CounterEnumerable(
            int count,
            int delay,
            [EnumeratorCancellation]
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Run IAsyncEnumerable<int> CounterEnumerable(count: {count}, delay: {delay})");

            for (int i = 0; i < count; i++) {
                // 
                // Check the cancellation token regularly so that the server will stop
                // producing items if the client disconnects.
                cancellationToken.ThrowIfCancellationRequested();

                yield return i; // T instance;

                // Use the cancellationToken in other APIs that accept cancellation
                // tokens so the cancellation can flow down to them.
                await Task.Delay(delay, cancellationToken);
            }
        }
        #endregion

        #region Client-to-server streaming
        // Receiving Streams on the Server

        // first approach, ChannelReader<T>
        public async Task UploadStreamChannel(ChannelReader<string> stream)
        {
            _logger.LogInformation($"Run UploadStreamChannel(ChannelReader stream: {stream})", true);

            while (await stream.WaitToReadAsync()) {
                while (stream.TryRead(out var item)) {
                    // do something with the stream item
                    _logger.LogInformation($"From client: {item}", true);
                }
            }
        }

        // Second apporach. IAsyncEnumerable<T>
        //
        public async Task UploadStreamEnumerable(IAsyncEnumerable<string> stream)
        {
            _logger.LogInformation($"UploadStreamEnumerable(IAsyncEnumerable stream: {stream})", true);

            await foreach (var item in stream) {
                // do something with the stream item
                _logger.LogInformation($"From client: {item}", true);
            }
        }
        #endregion
    }
}
