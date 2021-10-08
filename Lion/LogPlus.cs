using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Lion
{
    public enum LogLevel { DATA, DEBUG, INFO, WARN, ERROR, FATEL }

    public class LogPlus : IDisposable
    {
        private string path;
        private string name;
        private string timeFormatter;
        private ConcurrentQueue<string> logs;
        private bool running;
        private Thread thread;

        public int Sleep = 100;

        #region LogPlus
        public LogPlus(string _path, string _name, string _timeFormatter = "yyyyMMddHH")
        {
            this.path = _path + (_path.EndsWith("/", StringComparison.Ordinal) ? "" : "/");
            this.name = _name;
            this.timeFormatter = _timeFormatter;
            this.logs = new ConcurrentQueue<string>();

            this.running = true;
            this.thread = new Thread(new ThreadStart(this.WriteThread));
            this.thread.Start();
        }
        #endregion

        public void Write(string _log) => this.logs.Enqueue(_log);

        #region WriteThread
        private void WriteThread()
        {
            while (this.running)
            {
                Thread.Sleep(this.Sleep);

                this.WriteToFile();
            }
            while (this.logs.Count > 0)
            {
                this.WriteToFile();
            }
        }
        #endregion

        #region WriteToFile
        private void WriteToFile()
        {
            string _file = "";
            StreamWriter _writer = null;
            while (this.logs.Count > 0)
            {
                if (!this.logs.TryDequeue(out string _log)) { continue; }

                string _filename = $"{this.path}{this.name}-{DateTime.UtcNow.ToString(this.timeFormatter)}.log";
                if (_writer == null)
                {
                    _file = _filename;
                }
                File.AppendAllText(_filename, _log + "\r\n");
            }
        }
        #endregion

        public void Dispose() => this.running = false;
    }
}
