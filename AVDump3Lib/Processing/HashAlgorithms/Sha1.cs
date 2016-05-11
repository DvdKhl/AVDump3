// Copyright (C) 2009 DvdKhl
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.using System;

using System.Security.Cryptography;

namespace AVDump3Lib.HashAlgorithms {
    /// <summary>Only use this class when the NET framework is not available!</summary>
    public class Sha1 : HashAlgorithm {
        HashAlgorithm hash;

        public Sha1() { hash = new System.Security.Cryptography.SHA1CryptoServiceProvider(); }

        protected override void HashCore(byte[] array, int ibStart, int cbSize) {
            hash.TransformBlock(array, ibStart, cbSize, null, 0);
        }

        protected override byte[] HashFinal() {
            hash.TransformFinalBlock(new byte[0], 0, 0);
            return hash.Hash;
        }

        public override void Initialize() { hash.Initialize(); }
    }
}
