using ChatModule.Content;
using ChatModule.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatModule.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> logger;
        private readonly IServiceProvider serviceProvider;

        public ChatController(ILogger<ChatController> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        [HttpGet]
        [Route("history")]
        [AllowAnonymous]
        public async Task<IActionResult> GetChatHistory(int fromUserId, int toUserId)
        {
            if (fromUserId == 0 || toUserId == 0) return BadRequest();

            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            await connection.OpenAsync();

            List<ChatPost> chatPosts = new List<ChatPost>();
            using (var command = new MySqlCommand($"SELECT * FROM chatposts WHERE (FromUserId = {fromUserId} AND ToUserId = {toUserId}) OR (FromUserId = {toUserId} AND ToUserId = {fromUserId})", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var chatPost = fetchChatPost(reader);
                        if (chatPost != null) chatPosts.Add(chatPost);
                    }
                };
            }
            
            await connection.CloseAsync();

            return Ok(new ChatModel() { Posts = chatPosts, TargetUserId = toUserId });
        }

        [HttpPost]
        [Route("post")]
        [AllowAnonymous]
        public async Task<IActionResult> SendChatMessage(SendChatPostModel model)
        {
            if (model?.FromUserId == 0 ) return BadRequest("From user");
            if (model?.ToUserId == 0) return BadRequest("To user");

            var connection = serviceProvider.GetService(typeof(MySqlConnection)) as MySqlConnection;
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();

            var post = new ChatPost
            {
                FromUserId = model.FromUserId,
                ToUserId = model.ToUserId,
                Content = model.Message,
                Created = DateTime.Now
            };

            using (var command = new MySqlCommand($"INSERT INTO chatposts(FromUserId, ToUserId, Created, Content) VALUES({post.FromUserId}, {post.ToUserId}, '{post.Created.ToString("yyyy-MM-dd HH:mm:ss")}', '{post.Content.Replace("'", "''")}');", connection))
            using (var reader = await command.ExecuteReaderAsync())
                await reader.ReadAsync();

            /*nt addedId = 0;

            MySqlTransaction tran = null;
            try
            {
                tran = connection.BeginTransaction();

                using (var command = new MySqlCommand($"INSERT INTO chatposts(FromUserId, ToUserId, Created, Content) VALUES({post.FromUserId}, {post.ToUserId}, '{post.Created.ToString("yyyy-MM-dd HH:mm:ss")}', '{post.Content.Replace("'", "''")}');", connection, tran))
                using (var reader = await command.ExecuteReaderAsync())
                    await reader.ReadAsync();

                using (var command = new MySqlCommand($"SELECT LAST_INSERT_ID();", connection, tran))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            addedId = GetValue<int>(reader.GetValue(0));
                        }
                    }
                }

                tran.Commit();
            }
            catch (Exception)
            {
                tran?.Rollback();
            }


            post.Id = addedId;
            return RedirectToAction("Chat", new { targetUserId = model.TargetUserId });
            */

            await connection.CloseAsync();

            return Ok();
        }

        #region DatabaseMapping

        /// <summary>
        /// Маппинг прочитанной строки из БД в модель сообщения чата
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private ChatPost fetchChatPost(MySqlDataReader reader)
        {
            var charPost = new ChatPost();
            charPost.Id = GetValue<int>(reader.GetValue(0));
            charPost.FromUserId = GetValue<int>(reader.GetValue(1));
            charPost.ToUserId = GetValue<int>(reader.GetValue(2));
            charPost.Created = GetValue<DateTime>(reader.GetValue(3));
            charPost.Content = GetValue<string>(reader.GetValue(4));

            return charPost;
        }

        private T GetValue<T>(object value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }

        #endregion DatabaseMapping
    }
}
