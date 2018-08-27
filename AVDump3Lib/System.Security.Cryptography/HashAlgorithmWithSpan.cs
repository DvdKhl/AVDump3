using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AVDump3Lib.System.Security.Cryptography {
    public interface IHashAlgorithmWithSpan : IDisposable {
        void Initialize();
        void HashCore(ReadOnlySpan<byte> data);
        byte[] HashFinal();
    }
}
