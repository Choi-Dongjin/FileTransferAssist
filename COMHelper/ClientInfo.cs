using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferAssist.COMHelper
{
    public class ClientInfo
    {
        public ClientInfo(int number, TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            this.number = number;
            this.bynumber = BitConverter.GetBytes(number);
        }

        private TcpClient tcpClient;

        public TcpClient TcpClient
        {
            get { return tcpClient; }
            set { tcpClient = value; }
        }

        private Guid guidClient;

        public Guid GuidClient { get { return guidClient; } set { guidClient = value; } }

        public CancellationTokenSource dataReceivedCTS = new CancellationTokenSource();

        public readonly ConcurrentDictionary<Guid, COMFileInfo> COMFileInfo = new ConcurrentDictionary<Guid, COMFileInfo>();

        private int number;

        public int Number
        { get { return number; } set { number = value; } }

        private byte[] bynumber;

        public byte[] ByNumber
        { get { return bynumber; } set { bynumber = value; } }

        private byte[] dataBuffer = new byte[COMDefine.DefaultBufferSize];

        public byte[] DataBuffer
        {
            get { return dataBuffer; }
            set { dataBuffer = value; }
        }

        /// <summary>
        /// 파일 데이터 전송 완료 확인
        /// </summary>
        private bool isFileTransferDone = false;

        /// <summary>
        /// 클라이언트 정지 의뢰
        /// </summary>
        private bool isClientStopRequest = false;

        /// <summary>
        /// 종료 확인
        /// </summary>
        private bool isDone = false;

        public bool IsDone { get { return isDone; } }

        private void StopClient()
        {
            try
            {
                this.tcpClient?.Close();
                this.tcpClient?.Dispose();
                this.dataBuffer.Clone();
            }
            catch { }
        }

        public void StopRoRequest()
        {
            this.isClientStopRequest = true;
            if (this.isDone)
                StopClient();
        }

        internal void DoneChecker(Guid guid)
        {
            if (this.isFileTransferDone)
            {
                this.isDone = true;
                if (this.isClientStopRequest)
                {
                    StopClient();
                }
            }
            this.COMFileInfo.TryRemove(guid, out _);
        }

        internal void FileTransferDoneChecker()
        {
            this.isFileTransferDone = true;
            if (this.COMFileInfo.IsEmpty)
            {
                this.isDone = true;
            }
        }

        public void FileInfoAdd(Guid guid, string name, string dir, long size)
        {
            COMFileInfo comFileInfo = new(guid, name, dir, size, DoneChecker);
            this.COMFileInfo.TryAdd(guid, comFileInfo);
        }

        public void FileInfoAdd(Guid guid, COMFileInfo comFileInfo)
        {
            this.COMFileInfo.TryAdd(guid, comFileInfo);
        }

        public void FileWrite(Guid guid, int number, byte[] bytes)
        {
            this.COMFileInfo.TryGetValue(guid, out COMFileInfo? comFileInfo);
            comFileInfo?.FileWrite(number, bytes);
        }
    }
}
