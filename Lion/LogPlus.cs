using System;
namespace Lion
{
    public enum LogPlusMode { JSON, TEXT }
    public enum LogPlusType { FATAL, ERROR, WARN, INFO, DEBUG }

    public class LogPlus
    {
        public static LogPlusMode Mode = LogPlusMode.TEXT;

        public static void Debug(string _text,string source){
            
        }

        private static void WriteText(LogPlusType _type){
            
        }
    }
}
