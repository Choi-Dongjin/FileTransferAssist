using FileTransferAssist.COMHelper;
using FileTransferAssist.Utils;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace FileTransferAssist.Client
{
    public class FTClient : IDisposable
    {
        #region IDisposable

        private bool _disposed = false;

        ~FTClient() => Dispose(false);

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
                StopClient();
                // 관리
            }
            // 비관리

            _disposed = true;
        }

        #endregion IDisposable

        private string ip = "";

        public string IP
        { get { return ip; } set { ip = value; } }

        private int port = 0;

        public int Port
        { get { return port; } set { port = value; } }

        private bool isOK = false;

        public bool IsOK
        { get { return isOK; } }

        private Guid guid;

        public Guid Guid
        { get { return guid; } set { guid = value; } }

        private TcpClient? client;

        private byte[] dataBuffer = new byte[COMDefine.DefaultBufferSize];

        internal ConcurrentQueue<byte[]> bytes = new();

        private readonly CancellationTokenSource cts = new();

        public readonly ConcurrentDictionary<Guid, COMFileInfo> COMFileInfo = new ConcurrentDictionary<Guid, COMFileInfo>();

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

        public FTClient(string ip, int port, Guid guid)
        {
            this.ip = ip;
            this.port = port;
            this.guid = guid;
            this.isOK = StartClient();
        }

        /// <summary>
        /// 클라이언트 실행
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool StartClient()
        {
            (string? ip, int? port) = IntegrityFTS.IPPortIntegrityCheck(this.ip, this.port);
            if (!string.IsNullOrEmpty(ip) && port != null)
            {
                this.ip = ip;
                this.port = (int)port;
                try { this.client = new TcpClient(this.ip, this.port); }
                catch { return false; }
            }
            else { return false; }
            return true;
        }

        public bool StartClientDataReceiver()
        {
            if (this.client is null)
                return false;

            Task.Factory.StartNew(() => DataReceived(this.client, this.cts.Token), TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() => Worker(this.cts.Token), TaskCreationOptions.LongRunning);
            return true;
        }

        public void SoftStopClient()
        {
            this.cts.Cancel();
        }

        /// <summary>
        /// 클라이언트 종료
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool StopClient()
        {
            if (!this.cts.IsCancellationRequested)
                this.cts.Cancel();

            if (this.client is null)
                return true;

            if (this.client.Connected)
            {
                try
                {
                    byte[] bytes = ByteCreator.StopClient();
                    this.client.GetStream().Write(bytes, 0, bytes.Length);
                }
                catch { return false; }
            }

            return true;
        }

        public void StopRoRequest()
        {
            this.isClientStopRequest = true;
            if (this.isDone)
                StopClient();
        }

        /// <summary>
        /// 파일 전송 완료
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool FileTransferEnd()
        {
            if (this.client != null && this.client.Connected)
            {
                try
                {
                    byte[] bytes = ByteCreator.FileTransferEnd();
                    this.client.GetStream().Write(bytes, 0, bytes.Length);
                    return true;
                }
                catch { return false; }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 클라이언트 초기 정보 입력
        /// </summary>
        /// <param name="clientGuid"></param>
        public void TrainsClientInfo(Guid clientGuid)
        {
            byte[] sendData = ByteCreator.ClientInit(clientGuid);
            this.client?.GetStream().Write(sendData, 0, sendData.Length);
        }

        /// <summary>
        /// 파일 전송
        /// </summary>
        /// <param name="localFilePath">소스 파일 경로</param>
        /// <param name="remoteFilePath">저장 위치 경로</param>
        /// <param name="modeTest">비교용 Test 파일 생성 여부</param>
        public void TrainsFile(string localFilePath, string remoteFilePath, bool modeTest = false)
        {
            string? fileDir = Path.GetDirectoryName(remoteFilePath);
            string fileName = Path.GetFileName(remoteFilePath);
            if (!string.IsNullOrEmpty(fileDir) && this.client != null)
            {
                Guid guid = Guid.NewGuid();
                FileStream stream = new(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                FileStream? stream1 = null;
                if (modeTest)
                    stream1 = new FileStream(remoteFilePath + ".T", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                long fileLength = stream.Length;

                byte[] sendData = ByteCreator.ControlInit(guid, fileName, fileDir, fileLength);
                //var TsendData = sendData.Skip(2).Take(sendData.Length);
                //(GuidClient Tguid, long TdataLength, string TfileName, string TfileDir) = ByteParsing.ParsingInit(TsendData.ToArray());

                this.client.GetStream().Write(sendData, 0, sendData.Length);

                int number = 0;

                while (fileLength > 0)
                {
                    byte[] buffer = new byte[COMDefine.DefaultBufferSize - 21]; // buffer
                    int lBytes = stream.Read(buffer, 0, COMDefine.DefaultBufferSize - 21);
                    byte[] fileData = new byte[lBytes];
                    Buffer.BlockCopy(buffer, 0, fileData, 0, lBytes);

                    byte[] fileDataClient = ByteCreator.FileTransferData(guid, number, fileData);
                    this.client.GetStream().Write(fileDataClient, 0, fileDataClient.Length);

                    (Guid _, int _, byte[] TData) = ByteParsing.ParsingFileData(fileDataClient);

                    stream1?.Write(TData);

                    fileLength -= lBytes;
                    number++;
                }
                stream.Close();
                stream1?.Close();
                //Thread.Sleep(100);
            }
        }

        private void DataReceived(TcpClient tcpClient, CancellationToken CT)
        {
            int retryCount = 0;
            try
            {
                List<byte> buffer = new List<byte>();
                while (!CT.IsCancellationRequested)
                {
                    try
                    {
                        int iByte = tcpClient.GetStream().Read(this.dataBuffer, 0, COMDefine.DefaultBufferSize);
                        retryCount = 0;
                        buffer.AddRange(this.dataBuffer.Take(iByte));

                        if (buffer.Count == COMDefine.DefaultBufferSize)
                        {
                            this.bytes.Enqueue(buffer.ToArray());
                            buffer.RemoveRange(0, buffer.Count);
                        }
                        else if (buffer.Count > COMDefine.DefaultBufferSize)
                        {
                            this.bytes.Enqueue(buffer.GetRange(0, COMDefine.DefaultBufferSize).ToArray());
                            buffer.RemoveRange(0, COMDefine.DefaultBufferSize);

                            while (buffer.Count / COMDefine.DefaultBufferSize > 0)
                            {
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
                            StopRoRequest();
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void Worker(CancellationToken CT)
        {
            while (true)
            {
                if (CT.IsCancellationRequested)
                    break;
                if (this.bytes.IsEmpty)
                {
                    Thread.Sleep(100);
                    break;
                }
                if (this.bytes.TryDequeue(out byte[]? bytes) || bytes is null)
                    break;

                switch (bytes[0])
                {
                    case Common.Communication.SOH:
                        Communication(bytes);
                        break;

                    case Common.Communication.FS:
                        FileTransfer(bytes);
                        break;

                    case Common.Communication.SO:
                        FileTransferDoneChecker();
                        break;
                }
            }
        }

        private void Communication(byte[] bytes)
        {
            switch (bytes[1])
            {
                case Common.Control.SINIT:
                    CommunicationAddFileInfo(bytes);
                    break;

                case Common.Control.FTS:
                    FileTransferDoneChecker();
                    break;
            }
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

        private void CommunicationAddFileInfo(byte[] bytes)
        {
            (Guid guid, long dataLength, string fileName, string fileDir) = ByteParsing.ParsingInit(bytes);
            FileInfoAdd(guid, fileName, fileDir, dataLength);
        }

        private void FileTransfer(byte[] bytes)
        {
            (Guid guid, int dataNumber, byte[] data) = ByteParsing.ParsingFileData(bytes);
            FileWrite(guid, dataNumber, data);
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