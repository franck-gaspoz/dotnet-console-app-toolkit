using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotNetConsoleAppToolkit.Console
{
    public class TextWriterWrapper
    {
        #region attributes

        public bool IsRedirected { get; protected set; }
        public bool IsBufferEnabled { get; protected set; }
        public static int InitialBufferCapacity = 16384;
        public object Lock => _textWriter;

        protected TextWriter _textWriter;
        protected TextWriter _redirectedTextWriter;
        protected MemoryStream _buffer = new MemoryStream(InitialBufferCapacity);
        protected TextWriter _bufferWriter;

        #region echo to filestream

        public bool FileEchoDumpDebugInfo = true;
        public bool FileEchoCommands = true;
        public bool FileEchoAutoFlush = true;
        public bool FileEchoAutoLineBreak = true;
        public bool FileEchoEnabled => _echoStreamWriter != null;
        protected StreamWriter _echoStreamWriter;
        protected FileStream _echoFileStream;
                
        #endregion

        #endregion

        #region construction & init

        public TextWriterWrapper()
        {
            _textWriter = new StreamWriter(new MemoryStream());
        }

        public TextWriterWrapper(TextWriter textWriter)
        {
            _textWriter = textWriter;
        }

        #endregion

        #region stream operations

        public void Redirect(TextWriter sw)
        {
            if (sw != null)
            {
                _redirectedTextWriter = _textWriter;
                _textWriter = sw;
                IsRedirected = true;
            }
            else
            {
                _textWriter.Flush();
                _textWriter.Close();
                _textWriter = _redirectedTextWriter;
                _redirectedTextWriter = null;
                IsRedirected = false;
            }
        }

        public void Redirect(string filePath = null)
        {
            if (filePath != null)
            {
                _redirectedTextWriter = _textWriter;
                _textWriter = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write));
                IsRedirected = true;
            }
            else
            {
                _textWriter.Flush();
                _textWriter.Close();
                _textWriter = _redirectedTextWriter;
                _redirectedTextWriter = null;
                IsRedirected = false;
            }
        }

        public void EchoOn(
            string filepath,
            bool autoFlush = true,
            bool autoLineBreak = true,
            bool echoCommands = true,
            bool echoDebugInfo = false)
        {
            if (!string.IsNullOrWhiteSpace(filepath) && _echoFileStream == null)
            {
                FileEchoAutoFlush = autoFlush;
                FileEchoAutoLineBreak = autoLineBreak;
                FileEchoCommands = echoCommands;
                FileEchoDumpDebugInfo = echoDebugInfo;
                _echoFileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write);
                _echoStreamWriter = new StreamWriter(_echoFileStream);
            }
        }

        public void EchoOff()
        {
            if (_echoFileStream != null)
            {
                _echoStreamWriter.Flush();
                _echoStreamWriter.Close();
                _echoFileStream = null;
                _echoStreamWriter = null;
            }
        }

        #endregion

        #region buffering operations

        public virtual void EnableBuffer()
        {
            lock (Lock)
            {
                if (IsBufferEnabled) return;
                if (_bufferWriter==null) _bufferWriter = new StreamWriter(_buffer);
                IsBufferEnabled = true;
            }
        }

        public virtual void CloseBuffer()
        {
            lock (Lock)
            {
                if (!IsBufferEnabled) return;
                _buffer.Seek(0,SeekOrigin.Begin);
                var txt = Encoding.Default.GetString( _buffer.ToArray() );
                _textWriter.Write(txt);
                _buffer.SetLength(0);
                IsBufferEnabled = false;
            }
        }

        #endregion

        #region stream write operations

        /// <summary>
        /// writes a string to the stream
        /// </summary>
        /// <param name="s">string to be written to the stream</param>
        public virtual void Write(string s)
        {
            if (IsBufferEnabled)
            {
                _bufferWriter.Write(s);
            }
            else
            {
                _textWriter.Write(s);
            }
        }

        /// <summary>
        /// writes a string to the stream
        /// </summary>
        /// <param name="s">string to be written to the stream</param>
        public virtual void WriteLine(string s)
        {
            if (IsBufferEnabled)
            {
                _bufferWriter.WriteLine(s);
            }
            else
            {
                _textWriter.WriteLine(s);
            }
        }

        public virtual void FileEcho(
            string s, 
            bool lineBreak = false, 
            [CallerMemberName]string callerMemberName = "", 
            [CallerLineNumber]int callerLineNumber = -1)
        {
            if (!FileEchoEnabled) return;
            if (FileEchoDumpDebugInfo)
                _echoStreamWriter?.Write($"l={s.Length},br={lineBreak} [{callerMemberName}:{callerLineNumber}] :");
            _echoStreamWriter?.Write(s);
            if (lineBreak | FileEchoAutoLineBreak) _echoStreamWriter?.WriteLine(string.Empty);
            if (FileEchoAutoFlush) _echoStreamWriter?.Flush();
        }

        #endregion

        #region lock operations

        public void Locked(Action action)
        {
            lock (Lock)
            {
                action?.Invoke();
            }
        }

        #endregion
    }
}
