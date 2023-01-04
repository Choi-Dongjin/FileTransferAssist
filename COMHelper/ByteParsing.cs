using FileTransferAssist.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferAssist.COMHelper
{
    internal class ByteParsing
    {
        public static Guid ParsingClientInit(byte[] readBuffer)
        {
            var byuniqueNumber = readBuffer.Skip(2).Take(16);
            Guid uniqueNumber = new Guid(byuniqueNumber.ToArray());
            return uniqueNumber;
        }

        public static (Guid UniqueNumber, long ByteLength, string FileName, string FileDir) ParsingInit(byte[] readBuffer)
        {

            var byuniqueNumber = readBuffer.Skip(2).Take(16);
            var bybyteLength = readBuffer.Skip(18).Take(8);
            var byfilePathInfo = readBuffer.Skip(26).Take(readBuffer.Length);
            List<string> datas = ParsingGroupData(byfilePathInfo.ToArray());


            Guid uniqueNumber = new Guid(byuniqueNumber.ToArray());
            long byteLength = BitConverter.ToInt64(bybyteLength.ToArray(), 0);
            string fileName = datas[0];
            string fileDir = datas[1];
            return (uniqueNumber, byteLength, fileName, fileDir);
        }

        public static (Guid UniqueNumber, int TransferDataNumber, byte[] data) ParsingFileData(byte[] bytes)
        {
            byte[] byuniqueNumber = new byte[16]; // 작업 고유 번호
            Buffer.BlockCopy(bytes, 1, byuniqueNumber, 0, 16);
            Guid uniqueNumber = new(byuniqueNumber);

            //byte[] bytransferDataNumber = new byte[4]; // 데이터 저장 번호
            //Buffer.BlockCopy(bytes, 16, bytransferDataNumber, 0, 4);
            int transferDataNumber = BitConverter.ToInt32(bytes, 17); // 데이터 저장 번호

            byte[] data = bytes.Skip(21).Take(bytes.Length).ToArray();

            return (uniqueNumber, transferDataNumber, data);
        }

        //public static Guid Parsing

        private static List<string> ParsingGroupData(byte[] readBuffer)
        {
            int[] indexGSs = readBuffer.FindAllIndexof(Common.Communication.GS);
            List<string> dataList = new List<string>();

            for (int i = 0; i < indexGSs.Length - 1; i++)
            {
                int indexGSL = indexGSs[i + 1] - indexGSs[i] - 1; // 수신자 이름 길이 검색
                byte[] byreceiver = new byte[indexGSL]; // 수신자 이름
                Buffer.BlockCopy(readBuffer, indexGSs[i] + 1, byreceiver, 0, indexGSL); // 수신자 이름 복사
                dataList.Add(Encoding.Default.GetString(byreceiver)); // 수신자 이름 string으로 변경
            }
            return dataList;
        }
    }
}
