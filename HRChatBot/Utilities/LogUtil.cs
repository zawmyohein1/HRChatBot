namespace HRChatBot.Utilities
{
    public static class LogUtil
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        static LogUtil()
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
        }

        public static void WriteLog(string message)
        {
            try
            {
                string filePath = Path.Combine(LogDirectory, $"log_{DateTime.UtcNow:yyyyMMdd}.txt");
                string entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(filePath, entry);
            }
            catch
            {
                // Silent catch to avoid breaking the flow
            }
        }
    }
}
