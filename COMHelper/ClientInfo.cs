using System.Collections.Concurrent;
using System.Net.Sockets;

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

        public Guid GuidClient
        { get { return guidClient; } set { guidClient = value; } }

        private CancellationTokenSource dataReceivedCTS = new CancellationTokenSource();

        public CancellationTokenSource DataReceivedCTS
        { get { return dataReceivedCTS; } }

        private ConcurrentDictionary<Guid, COMFileInfo> comFileInfo = new ConcurrentDictionary<Guid, COMFileInfo>();

        public ConcurrentDictionary<Guid, COMFileInfo> COMFileInfo
        { get { return comFileInfo; } }

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

        public bool IsDone
        { get { return isDone; } }

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
            if (!string.IsNullOrEmpty(fileDir) && this.TcpClient != null)
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

                this.TcpClient.GetStream().Write(sendData, 0, sendData.Length);

                int number = 0;

                while (fileLength > 0)
                {
                    byte[] buffer = new byte[COMDefine.DefaultBufferSize - 21]; // buffer
                    int lBytes = stream.Read(buffer, 0, COMDefine.DefaultBufferSize - 21);
                    byte[] fileData = new byte[lBytes];
                    Buffer.BlockCopy(buffer, 0, fileData, 0, lBytes);

                    byte[] fileDataClient = ByteCreator.FileTransferData(guid, number, fileData);
                    this.TcpClient.GetStream().Write(fileDataClient, 0, fileDataClient.Length);

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
    }
}