using System.Net;

namespace FileTransferAssist.Utils
{
    internal class IntegrityFTS
    {
        /// <summary>
        /// IP, Port 정합성 확인
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="serverPort"></param>
        /// <returns>true if the value parameter is string; otherwise, false.</returns>
        public static (string? IP, int? Port) IPPortIntegrityCheck(string? ip, int? port)
        {
            // 포트 확인
            if (port != null)
            {
                // 0 ~ 1023 : well-known port번호
                // 1024 ~ 49151 : 등록된 포트 (registered port) 서버 영역
                //49152 ~ 65535  : 동적 포트(dynamic port)
                if (!(port >= 1024 && port <= 49151))
                    port = null;
            }
            else
            {
                port = 10000;
            }

            // IP 확인
            if (string.IsNullOrEmpty(ip) && !IPAddress.TryParse(ip, out IPAddress _))
                ip = null;

            return ValueTuple.Create(ip, port);
        }
    }
}