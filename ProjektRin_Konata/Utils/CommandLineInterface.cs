namespace ProjektRin
{
    public class CommandLineInterface
    {
        public enum LogLevel
        {
            DEBUG,
            INFO,
            WARN,
            ERROR,
            FATAL
        }

        private static CommandLineInterface _instance = new();
        private CommandLineInterface() { }
        public static CommandLineInterface Instance { get { return _instance; } }

        public void Print(string content, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(content);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public void Print(LogLevel level, string tag, string content, ConsoleColor color = ConsoleColor.White)
            => Print($"[{DateTime.Now:s}] [{level,-5}] [{tag}] {content}", color);
        public void Debug(string tag, string content)
            => Print(LogLevel.DEBUG, tag, content, ConsoleColor.Gray);
        public void Info(string tag, string content)
            => Print(LogLevel.INFO, tag, content, ConsoleColor.Green);
        public void Warn(string tag, string content)
            => Print(LogLevel.WARN, tag, content, ConsoleColor.Yellow);
        public void Error(string tag, string content)
            => Print(LogLevel.ERROR, tag, content, ConsoleColor.Red);

    }
}
