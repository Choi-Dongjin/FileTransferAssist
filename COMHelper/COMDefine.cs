using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferAssist.COMHelper
{
    internal struct COMDefine
    {
        public const int DefaultBufferSize = BufferSize.Default;

        public struct BufferSize
        {
            public const int K1 = 1024;
            public const int K2 = 2048;
            public const int K3 = 4096;
            public const int Default = 8192;
            public const int K10 = 10240;
            public const int K100 = 102400;
            public const int M1 = 1048576;
            public const int M10 = 10485760;
            public const int M100 = 104857600;
        }
    }
}
