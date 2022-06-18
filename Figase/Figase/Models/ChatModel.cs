using Figase.Context;
using System.Collections.Generic;

namespace Figase.Models
{
    public class ChatModel
    {
        public int TargetUserId { get; set; }
        public List<ChatPost> Posts { get; set; }
    }
}
