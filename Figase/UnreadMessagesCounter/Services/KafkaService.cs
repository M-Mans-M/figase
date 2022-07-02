using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnreadMessagesCounter.Options;

namespace UnreadMessagesCounter.Services
{
    public class KafkaService
    {
        private readonly ProducerConfig producerConfig;
        private readonly ConsumerConfig consumerConfig;
        private readonly AdminClientConfig adminConfig;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public KafkaService(IOptions<KafkaOptions> optionsWrapper)
        {
            producerConfig = new ProducerConfig
            {
                BootstrapServers = optionsWrapper.Value.Host,
                ClientId = Assembly.GetEntryAssembly().GetName().Name                
            };

            consumerConfig = new ConsumerConfig
            {
                BootstrapServers = optionsWrapper.Value.Host,
                GroupId = "Otus",
                ClientId = Assembly.GetEntryAssembly().GetName().Name,
                AutoOffsetReset = AutoOffsetReset.Latest
            };

            adminConfig = new AdminClientConfig { BootstrapServers = optionsWrapper.Value.Host };
        }

        public async Task ProduceAsync(string topic, string message, Dictionary<string,string> additionalInfo = null)
        {
            ensureTopicExists(topic);

            using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
            {
                Headers headers = null;
                if (additionalInfo != null && additionalInfo.Count > 0)
                {
                    headers = new Headers();
                    foreach (var item in additionalInfo)
                        headers.Add(new Header(item.Key, Encoding.UTF8.GetBytes(item.Value)));
                }

                await producer.ProduceAsync(topic, new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = message, Headers = headers });
            }
        }

        public void Subscribe(string topic, Action<string, Dictionary<string,string>> callback)
        {
            ensureTopicExists(topic);

            Task.Run(() =>
            {
                using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
                {
                    consumer.Subscribe(topic);

                    while (!cts.IsCancellationRequested)
                    {
                        var consumeResult = consumer.Consume(cts.Token);
                        callback.Invoke(consumeResult.Message.Value, consumeResult.Message.Headers.ToDictionary(k => k.Key, v => Encoding.UTF8.GetString(v.GetValueBytes())));
                    }

                    consumer.Close();
                }
            });
        }

        #region topics

        /// <summary>
        /// Проверка на существование топика. Создаем его при отсутствии
        /// </summary>
        /// <param name="topic">Название топика</param>
        private void createTopic(string topic)
        {
            try
            {
                using var adminClient = new AdminClientBuilder(adminConfig)
                        .SetErrorHandler((p, error) => { Console.WriteLine($"[ERROR] Kafka check for topics error: {error.Reason}"); })
                        .Build();

                adminClient.CreateTopicsAsync(new[] { new TopicSpecification { Name = topic, NumPartitions = -1, ReplicationFactor = 1 } }).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Kafka Admin client error: {e.Message}");
            }
        }

        /// <summary>
        /// Названия топиков, имеющихся в кафке
        /// </summary>
        private string[] topicNames;

        /// <summary>
        /// Проверить существование топика (с единоразовым обновлением топиков если не нашли)
        /// </summary>
        /// <param name="topicName"></param>
        /// <returns></returns>
        private bool ensureTopicExists(string topicName, int ttl = 1)
        {
            // Проверим существование желаемых топиков
            var topicExists = topicNames == null ? false : topicNames.Contains(topicName);
            if (!topicExists)
            {
                // Если не нашли, обновляем один раз и ищем ещё раз
                refreshTopicNames();

                if (topicNames == null || !topicNames.Contains(topicName))
                {
                    if (ttl == 0) throw new Exception("TTL expire on topic create");

                    createTopic(topicName);
                    return ensureTopicExists(topicName, --ttl);
                }
            }

            return true;
        }

        /// <summary>
        /// Получение списка названий топиков, имеющихся на кафке.
        /// </summary>
        private void refreshTopicNames()
        {
            try
            {
                var adminClientBuilder = new AdminClientBuilder(adminConfig);

                using var adminClient = adminClientBuilder.SetErrorHandler((p, error) => { Console.WriteLine($"[ERROR] Kafka Admin client error: {error.Reason}"); }).Build();
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                var topics = metadata.Topics.Select(a => a.Topic).ToArray();

                topicNames = topics == null || topics.Length == 0 ? null : topics;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Get topics error: {e.Message}");
            }
        }

        #endregion topics

        ~KafkaService()
        {
            cts?.Cancel();
        }
    }
}
