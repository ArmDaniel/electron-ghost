using System;

namespace DestinyGhostAssistant.Models
{
    public enum MessageSender
    {
        User,
        Assistant,
        System
    }

    public class ChatMessage
    {
        public string Text { get; set; }
        public MessageSender Sender { get; set; }
        public DateTime Timestamp { get; set; }
        public string SenderDisplay => Sender.ToString();

        public ChatMessage(string text, MessageSender sender, DateTime? timestamp = null)
        {
            Text = text;
            Sender = sender;
            Timestamp = timestamp ?? DateTime.Now;
        }
    }
}
