using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lion
{
    public delegate void CommandDelegate();

    public class ConsolePlus
    {
        private string pre;
        private DataTable commands;

        public ConsolePlus(string _welcome)
        {
            this.pre = "";
            this.commands = new DataTable();
            this.commands.Columns.Add("Code", typeof(string));
            this.commands.Columns.Add("Title", typeof(string));
            this.commands.Columns.Add("Level", typeof(int));
            this.commands.Columns.Add("Delegate", typeof(object));

            Console.WriteLine(_welcome);
        }

        #region Put
        public void Put(string _code, string _title) { this.Put(_code, _title, null); }
        public void Put(string _code, string _title, CommandDelegate _commandDelegate)
        {
            if (Regex.IsMatch(_code, "^(\\w+(\\.\\w+)*)$"))
            {
                DataRow _row = this.commands.NewRow();
                _row["Code"] = _code;
                _row["Title"] = _title;
                _row["Level"] = _code.Split('.').Length;
                _row["Delegate"] = _commandDelegate;
                this.commands.Rows.Add(_row);
            }
            else
            {
                Console.WriteLine("Error: " + _code);
            }
        }
        #endregion

        #region Run
        public void Run()
        {
            while (true)
            {
                int _level = this.pre == "" ? 1 : (this.pre.Split('.').Length + 1);
                Console.Clear();
                Console.Write("Current menu level " + _level.ToString() + ": ");
                Console.WriteLine(this.pre);
                Console.WriteLine();

                DataView _view = new DataView(this.commands, "Code LIKE '" + this.pre + "%' AND Level=" + _level, "", DataViewRowState.CurrentRows);
                foreach (DataRowView _row in _view)
                {
                    Console.WriteLine(_row["Code"].ToString() + " - " + _row["Title"]);
                }

                Console.WriteLine();
                if (_level == 1)
                {
                    Console.WriteLine("Empty - Exit console.");
                }
                else
                {
                    Console.WriteLine("Empty - Return to up level.");
                }
                Console.WriteLine();
                Console.Write("Choose one: ");

                string _command = Console.ReadLine();
                if (_command.ToLower() == "exit")
                {
                    if (_level == 1)
                    {
                        Console.WriteLine("Bye.");
                        break;
                    }
                    else
                    {
                        this.pre = this.pre.Substring(0, this.pre.LastIndexOf('.'));
                        continue;
                    }
                }
                else
                {
                    string _selected = this.pre + (this.pre == "" ? "" : ".") + _command;
                    DataRow[] _rows = this.commands.Select("Code='" + _selected + "'");
                    if (_rows.Length == 1)
                    {
                        if (_rows[0]["Delegate"] == System.DBNull.Value)
                        {
                            this.pre = _selected;
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("Execute command: " + _selected);
                            CommandDelegate _delegate = (CommandDelegate)_rows[0]["Delegate"];
                            _delegate();

                            this.pre = "";
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Wrong command.");
                    }
                }
            }
        }
        #endregion

        public static void WriteLine(string _value)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " > " + _value);
        }
    }
}
