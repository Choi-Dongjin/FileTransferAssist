using FileTransferAssist.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FileTransferAssist.COMHelper;

namespace FileTransferAssist.Server
{
    public class FTServer : IDisposable
    {
        #region IDisposable

        private bool _disposed = false;

        ~FTServer() => Dispose(false);

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
                this.listener?.Stop();
                ServerStop();
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

        private TcpListener? listener;

        public TcpListener? Listener
        { get { return listener; } }

        private readonly CancellationTokenSource serverCTS;

        private readonly FTServerDataReceivedWorker ftServerWorker = new();

        public FTServer(int port)
        {
            this.port = port;
            this.serverCTS = new CancellationTokenSource();
            Task.Factory.StartNew(() => ServerRunType1(this.serverCTS.Token));
            FTServerWorkerStart();
        }

        /// <summary>
        /// 서버 워커 시작
        /// </summary>
        public void FTServerWorkerStart()
        {
            this.ftServerWorker.WorkerStart();
        }

        /// <summary>
        /// 서버 시작
        /// </summary>
        /// <param name="CT"></param>
        public async void ServerRunType1(CancellationToken CT)
        {
            (_, int? port) = IntegrityFTS.IPPortIntegrityCheck(this.ip, this.port);
            if (port != null)
            {
                Port = (int)port;
                this.listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
                try
                {
                    this.listener.Start();
                }
                catch (Exception ex)
                {
                    this.listener.Stop();
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("서버 실행 실패 \n 서버 종료 시도");
                    return;
                }
                try
                {
                    while (!CT.IsCancellationRequested)
                    {
                        TcpClient newClient = await this.listener.AcceptTcpClientAsync(CT).AsTask().ConfigureAwait(false);
                        this.ftServerWorker.ClientAdd(newClient, CT);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("서버 실행 종료 \n 서버 종료 시도");
                    return;
                }
            }
        }

        public void ServerStop()
        {
            this.ftServerWorker.Dispose();
            this.serverCTS.Cancel();
        }

        public ClientInfo? GetTransferClientInfo(Guid guid)
        {
            foreach (var client in this.ftServerWorker.userList.Values)
            {
                if (client.GuidClient.Equals(guid))
                    return client;
            }
            return null;
        }

        public void TestFC()
        {
            Console.WriteLine(this.ftServerWorker.bytes.Count);
        }
    }

}
