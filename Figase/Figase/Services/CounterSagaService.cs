using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Figase.Services
{
    public class CounterSagaService
    {
        private readonly KafkaService kafkaService;
        private readonly IServiceProvider serviceProvider;

        private const string requestTopic = "Counters.Request";
        private const string responseTopic = "Counters.Response";

        public CounterSagaService(KafkaService kafkaService, IServiceProvider serviceProvider)
        {
            this.kafkaService = kafkaService;
            this.serviceProvider = serviceProvider;

            kafkaService.Subscribe(responseTopic, (msg, headers) => ProcessMessages(msg, headers));
        }

        private void ProcessMessages(string message, Dictionary<string, string> additionalInfo)
        {
            if (!additionalInfo.TryGetValue("RequestId", out var requestId)) return;

            if (pendingRequests.TryRemove(requestId, out TaskCompletionSource<string> result))
            {
                result.SetResult(message);
            }
        }

        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public async Task<string> TryIncreaseCounterAsync(int fromUserId, int toUserId)
        {
            var request = new CounterSagaModel { RequestId = Guid.NewGuid().ToString(), Command = CounterSageCommands.Increment, FromUserId = fromUserId, ToUserId = toUserId };
            var response = new TaskCompletionSource<string>();
            if (pendingRequests.TryAdd(request.RequestId, response))
            {
                await kafkaService.ProduceAsync(requestTopic, Newtonsoft.Json.JsonConvert.SerializeObject(request), new Dictionary<string, string> { { "RequestId", request.RequestId } });

                // Ожидаем ответ
                if (await Task.WhenAny(response.Task, Task.Delay(10 * 1000)) != response.Task)
                {
                    // Таймаут ожидания ответа от терминала
                    pendingRequests.TryRemove(request.RequestId, out _);
                    return null;
                }

                return response.Task.Result;
            }

            return null;
        }

        public async Task<string> TryResetCounterAsync(int fromUserId, int toUserId)
        {
            var request = new CounterSagaModel { RequestId = Guid.NewGuid().ToString(), Command = CounterSageCommands.Reset, FromUserId = fromUserId, ToUserId = toUserId };
            var response = new TaskCompletionSource<string>();
            if (pendingRequests.TryAdd(request.RequestId, response))
            {
                await kafkaService.ProduceAsync(requestTopic, Newtonsoft.Json.JsonConvert.SerializeObject(request), new Dictionary<string, string> { { "RequestId", request.RequestId } });

                // Ожидаем ответ
                if (await Task.WhenAny(response.Task, Task.Delay(20 * 1000)) != response.Task)
                {
                    // Таймаут ожидания ответа от терминала
                    pendingRequests.TryRemove(request.RequestId, out _);
                    return null;
                }

                return response.Task.Result;
            }

            return null;
        }
    }

    public class CounterSagaModel
    {
        [JsonIgnore]
        public string RequestId { get; set; }
        public CounterSageCommands Command { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
    }

    public enum CounterSageCommands
    {
        Increment = 0,
        Reset
    }
}
