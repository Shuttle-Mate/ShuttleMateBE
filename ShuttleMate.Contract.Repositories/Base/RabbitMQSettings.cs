namespace ShuttleMate.Contract.Repositories.Base
{
    public class RabbitMQSettings
    {
        public string? HostName { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public int Port { get; set; }
        public QueueChannel? QueueChannel { get; set; }
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(HostName))
            {
                throw new ArgumentException("HostName cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(UserName))
            {
                throw new ArgumentException("UserName cannot be null or empty.");
            }
            if (Port <= 0)
            {
                throw new ArgumentException("Port must be greater than 0.");
            }
            QueueChannel?.IsValid();
            return true;
        }
    }
    public class QueueChannel
    { 
        public string? QaQueue { get; set; }
        public string? TaskProductQueue { get; set; }
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(QaQueue))
            {
                throw new ArgumentException("QaQueue cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(TaskProductQueue))
            {
                throw new ArgumentException("TaskProductQueue cannot be null or empty.");
            }
            return true;
        }
    }
}
