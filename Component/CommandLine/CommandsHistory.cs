using DotNetConsoleSdk.Component.CommandLine.Commands.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using static DotNetConsoleSdk.DotNetConsole;

namespace DotNetConsoleSdk.Component.CommandLine
{
    public class CommandsHistory
    {
        public const string CommandsHistoryFilename = "history.txt";

        #region attributes

        readonly List<string> _history = new List<string>();
        int _historyIndex = -1;
        public List<string> History => new List<string>(_history);
        readonly string UserProfileFolder;

        #endregion

        public CommandsHistory(string userProfileFolder)
        {
            UserProfileFolder = userProfileFolder;
            try
            {
                var lines = File.ReadAllLines(UserCommandsHistoryFilePath.FullName);
                foreach (var line in lines) HistoryAppend(line);
            } catch (Exception initHistoryError)
            {
                Errorln("failed to load and initialize commands history: "+initHistoryError.Message);
            }
        }

        public FilePath UserCommandsHistoryFilePath
        {
            get
            {
                var userPath = UserProfileFolder;
                return new FilePath(Path.Combine(userPath, CommandsHistoryFilename));
            }
        }

        #region history operations

        public string GetBackwardHistory()
        {
            if (_historyIndex < 0)
                _historyIndex = _history.Count + 1;
            if (_historyIndex >= 1)
                _historyIndex--;
            return (_historyIndex < 0 || _history.Count == 0 || _historyIndex >= _history.Count) ? null : _history[_historyIndex];
        }

        public string GetForwardHistory()
        {
            if (_historyIndex < 0 || _historyIndex >= _history.Count)
                _historyIndex = _history.Count;
            if (_historyIndex < _history.Count - 1) _historyIndex++;

            return (_historyIndex < 0 || _history.Count == 0 || _historyIndex >= _history.Count) ? null : _history[_historyIndex];
        }

        public bool HistoryContains(string s) => _history.Contains(s);

        public void HistoryAppend(string s)
        {
            _history.Add(s);
            _historyIndex = _history.Count - 1;
        }

        public void ClearHistory() => _history.Clear();

        #endregion
    }
}
