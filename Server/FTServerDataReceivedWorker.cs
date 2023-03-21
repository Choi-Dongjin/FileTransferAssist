using FileTransferAssist.COMHelper;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace FileTransferAssist.Server
{
    internal class FTServerDataReceivedWorker : IDisposable
    {
        #region IDisposable

        private bool _disposed = false;

        ~FTServerDataReceivedWorker() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // 관리
                ServerStop();
            }
            // 비관리

            _disposed = true;
        }

        #endregion IDisposable

        internal ConcurrentQueue<byte[]> bytes = new();

        internal ConcurrentQueue<int> clientNumber = new();

        private CancellationTokenSource CTS = new();

        internal ConcurrentDictionary<int, ClientInfo> userList = new();

        internal FTServerDataReceivedWorker()
        {
            this.CTS.Cancel();
        }

        internal void ClientAdd(TcpClient client, CancellationToken CT)
        {
            int number = CheckandGetUserNumber();
            ClientInfo clientInfo = new(number, client);
            this.userList.TryAdd(number, clientInfo);
            //clientInfo.TcpClient.GetStream().BeginRead(clientInfo.DataBuffer, 0, clientInfo.DataBuffer.Length, new AsyncCallback(DataReceived), clientInfo);
            Task.Factory.StartNew(() => DataReceived(clientInfo, clientInfo.DataReceivedCTS.Token), CT);
        }

        public void ClientDel(int clientNumber)
        {
            if (this.userList.TryRemove(this.userList[clientNumber].Number, out ClientInfo? client))
            {
                client?.StopRoRequest();
                client?.DataReceivedCTS.Cancel();
            }
        }

        internal void ServerStop()
        {
            foreach (var user in this.userList.Values)
            {
                ClientDel(user.Number);
            }
            this.CTS.Cancel();
        }

        private void DataReceived(ClientInfo clientInfo, CancellationToken CT)
        {
            int retryCount = 0;
            try
            {
                List<byte> buffer = new List<byte>();
                while (!CT.IsCancellationRequested)
                {
                    try
                    {
                        int iByte = clientInfo.TcpClient.GetStream().Read(clientInfo.DataBuffer, 0, COMDefine.DefaultBufferSize);
                        retryCount = 0;
                        buffer.AddRange(clientInfo.DataBuffer.Take(iByte));

                        if (buffer.Count == COMDefine.DefaultBufferSize)
                        {
                            this.clientNumber.Enqueue(clientInfo.Number);
                            this.bytes.Enqueue(buffer.ToArray());
                            buffer.RemoveRange(0, buffer.Count);
                        }
                        else if (buffer.Count > COMDefine.DefaultBufferSize)
                        {
                            this.clientNumber.Enqueue(clientInfo.Number);
                            this.bytes.Enqueue(buffer.GetRange(0, COMDefine.DefaultBufferSize).ToArray());
                            buffer.RemoveRange(0, COMDefine.DefaultBufferSize);

                            while (buffer.Count / COMDefine.DefaultBufferSize > 0)
                            {
                                this.clientNumber.Enqueue(clientInfo.Number);
                                this.bytes.Enqueue(buffer.GetRange(0, COMDefine.DefaultBufferSize).ToArray());
                                buffer.RemoveRange(0, COMDefine.DefaultBufferSize);
                            }
                        }
                        else if (buffer.Count < COMDefine.DefaultBufferSize)
                        {
                        }
                    }
                    catch
                    {
                        retryCount++;
                        if (retryCount > 5)
                        {
                            clientInfo.StopRoRequest();
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public int CheckandGetUserNumber()
        {
            if (this.userList.Keys.Count > 0)
            {
                List<int> userNumberList = this.userList.Keys.ToList();
                userNumberList.Sort();
                int maxNumber = userNumberList.Max();

                for (int i = 0; i < maxNumber + 1; i++)
                {
                    if (userNumberList.IndexOf(i) == -1)
                    {
                        return i;
                    }
                }
                return maxNumber + 1;
            }
            else
            {
                return 0;
            }
        }

        public void WorkerStart()
        {
            // 처음 this.CTS를 cancel 하여 꺼놨음
            if (this.CTS.IsCancellationRequested)
            {
                this.CTS = new CancellationTokenSource();
                Task.Factory.StartNew(() => Worker(this.CTS.Token), TaskCreationOptions.LongRunning);
            }
        }

        private void Worker(CancellationToken CT)
        {
            while (!CT.IsCancellationRequested)
            {
                while (!this.bytes.IsEmpty)
                {
                    if (this.bytes.TryDequeue(out byte[]? bytes))
                    {
                        this.clientNumber.TryDequeue(out int clientNumber);
                        if (bytes != null)
                        {
                            //IEnumerable<byte> pushData = bytes.Skip(1).Take(bytes.Length - 1);
                            switch (bytes[0])
                            {
                                case Common.Communication.SOH:
                                    Communication(clientNumber, bytes);
                                    break;

                                case Common.Communication.FS:
                                    FileTransfer(clientNumber, bytes);
                                    break;

                                case Common.Communication.SO:
                                    this.userList[clientNumber].FileTransferDoneChecker();
                                    break;
                            }
                        }
                    }
                }
                Thread.Sleep(100);
            }
        }

        private void Communication(int clientNumber, byte[] bytes)
        {
            switch (bytes[1])
            {
                case Common.Control.CINIT:
                    CommunicationInputClientInfo(clientNumber, bytes);
                    break;

                case Common.Control.SINIT:
                    CommunicationAddFileInfo(clientNumber, bytes);
                    break;

                case Common.Control.FTS:
                    CommunicationFileTransferStop(clientNumber);
                    break;
            }
        }

        private void CommunicationInputClientInfo(int clientNumber, byte[] bytes)
        {
            this.userList[clientNumber].GuidClient = ByteParsing.ParsingClientInit(bytes);
        }

        private void CommunicationAddFileInfo(int clientNumber, byte[] bytes)
        {
            (Guid guid, long dataLength, string fileName, string fileDir) = ByteParsing.ParsingInit(bytes);
            this.userList[clientNumber].FileInfoAdd(guid, fileName, fileDir, dataLength);
        }

        private void CommunicationFileTransferStop(int clientNumber)
        {
            this.userList[clientNumber].FileTransferDoneChecker();
        }

        private void FileTransfer(int clientNumber, byte[] bytes)
        {
            (Guid guid, int dataNumber, byte[] data) = ByteParsing.ParsingFileData(bytes);
            this.userList[clientNumber].FileWrite(guid, dataNumber, data);
        }
    }
}