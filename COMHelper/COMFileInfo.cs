using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferAssist.COMHelper
{
    public class COMFileInfo
    {
        private readonly Guid guid;
        private readonly string name;
        private readonly string dir;
        private long size;
        private long sizeFull;
        private readonly FileStream fileStream;
        private int fileWritenumber = 0;
        private readonly ConcurrentDictionary<int, byte[]> buffer = new();

        private bool isDone = false;
        public bool IsDone { get { return isDone; } }

        public delegate void DoneEvent(Guid guid);
        public DoneEvent? doneEvent;

        public COMFileInfo(Guid guid, string name, string dir, long size, DoneEvent? doneEvent)
        {
            this.guid = guid;
            this.name = name;
            this.dir = dir;
            this.size = size;
            this.sizeFull = size;
            this.fileStream = new FileStream(Path.Combine(this.dir, this.name), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            this.doneEvent = doneEvent;
        }

        public void FileWrite(int number, byte[] data)
        {
            if (!this.fileStream.CanWrite)
                return;
            if (number == this.fileWritenumber)
            {
                FileWrite(data);
            }
            else if (number > this.fileWritenumber)
            {
                this.buffer.TryAdd(number, data);
            }
            else if (number < this.fileWritenumber)
            {
                this.buffer.TryAdd(number, data);
                FileScanWrite(number);
            }
        }

        private void FileWrite(byte[] data)
        {
            if (this.size < COMDefine.DefaultBufferSize)
            {
                data = data.Take((int)this.size).ToArray();
                this.fileStream.Write(data, 0, data.Length);
                this.size -= data.Length;
                this.fileWritenumber++;
                if (this.size <= 0)
                    FileClose();
            }
            else
            {
                this.fileStream.Write(data, 0, data.Length);
                this.size -= data.Length;
                this.fileWritenumber++;
            }

        }

        private void FileScanWrite(int number)
        {
            if (number <= this.fileWritenumber)
            {
                if (this.buffer.Count > 0)
                {
                    if (this.buffer.TryRemove(number, out byte[]? bufferData))
                    {
                        FileWrite(bufferData);
                        FileScanWrite(number++);
                    }
                }
            }
        }

        public void FileClose()
        {
            this.fileStream.Close();
            this.doneEvent?.Invoke(this.guid);
            this.isDone = true;
        }

        public int FileProgress()
        {
            if (this.isDone)
                return 100;
            else
                return (int)((this.sizeFull - this.size) / this.sizeFull * 100);
        }
    }
}
