using FileTransferAssist.COMHelper;
using FileTransferAssist.Utils;
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
                // 관리
                client?.Close();
                client?.Dispose();
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

        /// <summary>
        /// 클라이언트 종료
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool StopClient()
        {
            if (this.client != null && this.client.Connected)
            {
                try
                {
                    byte[] bytes = ByteCreator.StopClient();
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
    }
}