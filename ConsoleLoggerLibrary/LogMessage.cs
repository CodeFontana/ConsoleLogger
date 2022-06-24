using Microsoft.Extensions.Logging;

namespace ConsoleLoggerLibrary;

internal class LogMessage
{
    public string TimeStamp { get; init; }
    public LogLevel LogLevel { get; init; }
    public string CategoryName { get; init; }
    public string Header { get; init; }
    public string Message { get; init; }
    public string UnPaddedMessage { get => $"{TimeStamp}|{LogLevelToString(LogLevel)}|{CategoryName}|{Message}"; }
    public string PaddedMessage { get; init; }
    public string FullMessage { get; init; }

    public LogMessage(LogLevel logLevel, string categoryName, string message)
    {
        TimeStamp = DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss");
        CategoryName = categoryName;
        Message = message;
        LogLevel = logLevel;
        Header = $"{TimeStamp}|{LogLevelToString(logLevel)}|{categoryName}|";
        PaddedMessage = PadMessage(Header, Message);
        FullMessage = $"{Header}{PaddedMessage}";
    }

    public static string LogLevelToString(LogLevel logLevel)
    {
        string header = "";

        switch (logLevel)
        {
            case LogLevel.Trace:
                header += "TRCE";
                break;
            case LogLevel.Warning:
                header += "WARN";
                break;
            case LogLevel.Debug:
                header += "DBUG";
                break;
            case LogLevel.Information:
                header += "INFO";
                break;
            case LogLevel.Error:
                header += "ERRR";
                break;
            case LogLevel.Critical:
                header += "CRIT";
                break;
            case LogLevel.None:
                header += "    ";
                break;
        }

        return header;
    }

    private static string PadMessage(string header, string message)
    {
        string output;

        if (message.Contains("\r\n") || message.Contains('\n'))
        {
            string[] splitMsg = message.Replace("\r\n", "\n").Split(new char[] { '\n' });

            for (int i = 1; i < splitMsg.Length; i++)
            {
                splitMsg[i] = new String(' ', header.Length) + splitMsg[i];
            }

            output = string.Join(Environment.NewLine, splitMsg);
        }
        else
        {
            output = message;
        }

        return output;
    }

    public override string ToString()
    {
        return FullMessage;
    }
}
