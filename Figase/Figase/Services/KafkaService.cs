using Confluent.Kafka;
using Figase.Options;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Figase.Services
{
    public class KafkaService
    {
        private readonly ProducerConfig producerConfig;
        private readonly ConsumerConfig consumerConfig;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public KafkaService(IOptions<KafkaOptions> optionsWrapper)
        {
            producerConfig = new ProducerConfig
            {
                BootstrapServers = optionsWrapper.Value.Host,
                ClientId = Dns.GetHostName()
            };

            consumerConfig = new ConsumerConfig
            {
                BootstrapServers = optionsWrapper.Value.Host,
                GroupId = "Otus",
                AutoOffsetReset = AutoOffsetReset.Latest
            };
        }

        public async Task ProduceAsync(string topic, string message)
        {
            using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
            {
                await producer.ProduceAsync(topic, new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = message });
            }
        }

        public void Subscribe(string topic, Action<string> callback)
        {
            Task.Run(() =>
            {
                using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
                {
                    consumer.Subscribe(topic);

                    while (!cts.IsCancellationRequested)
                    {
                        var consumeResult = consumer.Consume(cts.Token);
                        callback.Invoke(consumeResult.Message.Value);
                    }

                    consumer.Close();
                }
            });
        }

        ~KafkaService()
        {
            cts?.Cancel();
        }
    }
}
