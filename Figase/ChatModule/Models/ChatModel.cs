using ChatModule.Content;
using System.Collections.Generic;

namespace ChatModule.Models
{
    public class ChatModel
    {
        public int TargetUserId { get; set; }
        public List<ChatPost> Posts { get; set; }
    }
}
