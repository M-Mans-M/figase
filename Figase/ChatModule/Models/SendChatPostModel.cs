namespace ChatModule.Models
{
    /// <summary>
    /// Пост
    /// </summary>
    public class SendChatPostModel
    {
        public string Message { get; set; }

        public int FromUserId { get; set; }

        public int ToUserId { get; set; }
    }
}
