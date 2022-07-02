using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnreadMessagesCounter.Services
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

            kafkaService.Subscribe(requestTopic, (msg, headers) => ProcessMessages(msg, headers));
        }

        private void ProcessMessages(string message, Dictionary<string, string> additionalInfo)
        {
            if (!additionalInfo.TryGetValue("RequestId", out var requestId)) return;

            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<CounterSagaModel>(message);

            switch (model.Command)
            {
                case CounterSageCommands.Increment:
                    incrementCounterAsync(model.FromUserId, model.ToUserId).GetAwaiter().GetResult();
                    break;
                case CounterSageCommands.Reset:
                    resetCounterAsync(model.FromUserId, model.ToUserId).GetAwaiter().GetResult();
                    break;
                default: throw new Exception("Unknown command type");
            }

            kafkaService.ProduceAsync(responseTopic, "OK", new Dictionary<string, string> { { "RequestId", requestId } }).GetAwaiter().GetResult();
        }

        private async Task resetCounterAsync(int fromUserId, int toUserId)
        {
            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            await connection.OpenAsync();

            using (var command = new MySqlCommand($"UPDATE counters SET count = 0 WHERE fromUserId = {fromUserId} AND toUserId = {toUserId}", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {

                    }
                }
            }

            await connection.CloseAsync();
        }

        private async Task incrementCounterAsync(int fromUserId, int toUserId)
        {
            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            await connection.OpenAsync();

            using (var command = new MySqlCommand($"INSERT INTO counters (fromUserId, toUserId, count) VALUES({fromUserId}, {toUserId}, 1) ON DUPLICATE KEY UPDATE count = count + 1", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {

                    }
                }
            }

            await connection.CloseAsync();
        }
    }

    public class CounterSagaModel
    {
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
