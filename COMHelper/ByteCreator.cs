using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferAssist.COMHelper
{
    internal class ByteCreator
    {
        public static byte[] ClientInit(Guid uniqueNumber)
        {
            byte[] byuniqueNumber = uniqueNumber.ToByteArray();
            return ClientInit(byuniqueNumber);
        }

        public static byte[] ClientInit(byte[] uniqueNumber)
        {
            byte[] sendData = new byte[COMDefine.DefaultBufferSize];
            sendData[0] = Common.Communication.SOH;
            sendData[1] = Common.Control.CINIT;
            Buffer.BlockCopy(uniqueNumber, 0, sendData, 2, 16); // 작업 고유 번호
            return sendData;
        }

        public static byte[] ControlInit(Guid uniqueNumber, string fileName, string fileDir, long dataLength)
        {
            byte[] byuniqueNumber = uniqueNumber.ToByteArray();
            byte[] byfileName = Encoding.Default.GetBytes(fileName);
            byte[] byfileDir = Encoding.Default.GetBytes(fileDir);
            byte[] bydataLenght = BitConverter.GetBytes(dataLength);
            return ControlInit(byuniqueNumber, bydataLenght, byfileName, byfileDir);
        }

        public static byte[] ControlInit(byte[] uniqueNumber, byte[] dataLength, byte[] fileName, byte[] fileDir)
        {
            byte[] sendData = new byte[COMDefine.DefaultBufferSize];
            sendData[0] = Common.Communication.SOH;
            sendData[1] = Common.Control.SINIT;
            Buffer.BlockCopy(uniqueNumber, 0, sendData, 2, 16); // 작업 고유 번호
            Buffer.BlockCopy(dataLength, 0, sendData, 18, 8); // 전송 데이터 길이
            sendData[26] = Common.Communication.GS;
            Buffer.BlockCopy(fileName, 0, sendData, 27, fileName.Length); // 파일 이름
            sendData[27 + fileName.Length] = Common.Communication.GS;
            Buffer.BlockCopy(fileDir, 0, sendData, 28 + fileName.Length, fileDir.Length); // 파일 이름
            sendData[28 + fileName.Length + fileDir.Length] = Common.Communication.GS;
            sendData[29 + fileName.Length + fileDir.Length] = Common.Communication.EOT;
            return sendData;
        }

        public static byte[] FileTransferData(Guid uniqueNumber, int transferDataNumber, byte[] data)
        {
            byte[] byuniqueNumber = uniqueNumber.ToByteArray();
            byte[] bytransferDataNumber = BitConverter.GetBytes(transferDataNumber);
            return FileTransferData(byuniqueNumber, bytransferDataNumber, data);
        }

        public static byte[] FileTransferData(byte[] uniqueNumber, byte[] transferDataNumber, byte[] data)
        {
            byte[] sendData = new byte[COMDefine.DefaultBufferSize];
            sendData[0] = Common.Communication.FS;
            Buffer.BlockCopy(uniqueNumber, 0, sendData, 1, 16); // 작업 고유 번호
            Buffer.BlockCopy(transferDataNumber, 0, sendData, 17, 4); // 파일 데이터 번호
            Buffer.BlockCopy(data, 0, sendData, 21, data.Length); // 데이터
            return sendData;
        }

        public static byte[] FileTransferEnd()
        {
            byte[] sendData = new byte[COMDefine.DefaultBufferSize];
            sendData[0] = Common.Communication.SOH;
            sendData[1] = Common.Control.FTS;
            return sendData;
        }

        public static byte[] StopClient()
        {
            byte[] sendData = new byte[COMDefine.DefaultBufferSize];
            sendData[0] = Common.Communication.SO;
            return sendData;
        }

        public static byte[] ReguserClient(Guid uniqueNumber)
        {
            byte[] byuniqueNumber = uniqueNumber.ToByteArray();
            return ReguserClient(byuniqueNumber);
        }

        public static byte[] ReguserClient(byte[] uniqueNumber)
        {
            byte[] sendData = new byte[COMDefine.DefaultBufferSize];
            sendData[0] = Common.Communication.SOH;
            sendData[1] = Common.Communication.ACK;
            Buffer.BlockCopy(uniqueNumber, 0, sendData, 2, 16); // 작업 고유 번호
            return sendData;
        }

        public static byte[]? CreateGroupData(List<string?> groupData)
        {
            byte[]? byusers = null;
            IEnumerable<string?> groupDatavar = (IEnumerable<string?>)groupData;

            foreach (string? user in groupDatavar)
            {
                byte[]? byuser;
                if (string.IsNullOrEmpty(user))
                {
                    byuser = Encoding.Default.GetBytes("");
                }
                else
                {
                    byuser = Encoding.Default.GetBytes(user);
                }
                if (byusers != null)
                {
                    byte[] oldbyusers = byusers;
                    byusers = new byte[oldbyusers.Length + byuser.Length + 1];
                    Buffer.BlockCopy(oldbyusers, 0, byusers, 0, oldbyusers.Length);
                    Buffer.BlockCopy(byuser, 0, byusers, oldbyusers.Length, byuser.Length);
                    byusers[^1] = Common.Communication.GS;
                }
                else
                {
                    byusers = new byte[2 + byuser.Length];
                    byusers[0] = Common.Communication.GS;
                    Buffer.BlockCopy(byuser, 0, byusers, 1, byuser.Length);
                    byusers[1 + byuser.Length] = Common.Communication.GS;
                }
            }
            return byusers;
        }
    }
}
