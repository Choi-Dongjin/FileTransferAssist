using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferAssist.COMHelper
{
    internal class Common
    {
        /// <summary>
        /// 통신 관련 명령어
        /// </summary>
        public struct Communication
        {
            /// <summary>
            /// [SOH] : 헤딩 시작 > 통신 시작 > bytes(1)
            /// </summary>
            public const byte SOH = 1;

            /// <summary>
            /// [STX] : 텍스트 시작 > > bytes(2)
            /// </summary>
            public const byte STX = 2;

            /// <summary>
            /// [ETX] :  텍스트 종료 > > bytes(3)
            /// </summary>
            public const byte ETX = 3;

            /// <summary>
            /// [EOT] : 전송 종료 > > bytes(4)
            /// </summary>
            public const byte EOT = 4;

            /// <summary>
            /// [ACK] : 인정 > 사용자 등록 > bytes(6)
            /// </summary>
            public const byte ACK = 6;

            /// <summary>
            /// [GS] : 그룹 구분 > 사용자 구분 > bytes(29)
            /// </summary>
            public const byte GS = 29;

            /// <summary>
            /// [DC1] : 장치 제어 1 > > bytes(11)
            /// </summary>
            public const byte DC1 = 11;

            /// <summary>
            /// [DC2] : 장치 제어 2 > > bytes(12)
            /// </summary>
            public const byte DC2 = 12;

            /// <summary>
            /// [DC3] : 장치 제어 3 > > bytes(13)
            /// </summary>
            public const byte DC3 = 13;

            /// <summary>
            /// [DC4] : 장치 제어 4 > > bytes(14)
            /// </summary>
            public const byte DC4 = 14;

            /// <summary>
            /// [ETB] : 전송 블록의 끝 > 여러 용도 > byte(17)
            /// </summary>
            public const byte ETB = 17;

            /// <summary>
            /// [SO] : 탈퇴 > > byte(14)
            /// </summary>
            public const byte SO = 14;

            /// <summary>
            /// [FS] : 파일 분리 자 > > byte(28)
            /// </summary>
            public const byte FS = 24;
        }

        /// <summary>
        /// 제어 명령어
        /// </summary>
        public struct Control
        {
            /// <summary>
            /// NONE : 명령어 없음 > bytes(0)
            /// </summary>
            public const byte NO = 0;

            /// <summary>
            /// SINIT : 전송 데이터 정보 입력 > byte(1)
            /// </summary>
            public const byte SINIT = 1;

            /// <summary>
            /// RINIT : 수신 데이터 정보 입력 > byte[2]
            /// </summary>
            public const byte RINIT = 2;

            /// <summary>
            /// [FTS] : 파일 전송 완료 > byte[3]
            /// </summary>
            public const byte FTS = 3;

            /// <summary>
            /// [INIT] : 클라이언트 정보 매칭 > byte[4]
            /// </summary>
            public const byte CINIT = 4;
        }
    }
}
