using Figase.Context;
using Figase.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Diagnostics;

namespace Figase.Services
{
    public class MainService
    {
        // Словарь-кеш новостей пользователей на которых подписан другой пользователь
        public readonly Dictionary<int, Dictionary<int, List<PersonPost>>> subsCache = new Dictionary<int, Dictionary<int, List<PersonPost>>>();

        private readonly IHubContext<NewsHub> newsHub;

        public readonly string newPostTopic = "Posts";

        public MainService(KafkaService kafkaService, IHubContext<NewsHub> newsHub)
        {
            this.newsHub = newsHub;

            kafkaService.Subscribe(newPostTopic, processNewPost);
        }

        private void processNewPost(string message)
        {
            var personPost = Newtonsoft.Json.JsonConvert.DeserializeObject<PersonPost>(message);

            foreach (var sub in subsCache)
            {
                if (sub.Value.TryGetValue(personPost.PersonId, out var cachedPosts))
                { 
                    cachedPosts.Add(personPost);

                    if (NewsHub.Connections.TryGetValue(sub.Key, out var personConnectionId))
                        newsHub.Clients.Client(personConnectionId).SendAsync("NewPost", personPost.Id, personPost.PersonId, personPost.Created.ToString("dd.MM.yyyy hh:mm:ss"), personPost.Content).GetAwaiter().GetResult();
                }                
            }

            Debug.WriteLine("KAFKA: " + message);
        }
    }
}
