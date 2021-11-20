using AVDump3CL;
using AVDump3Lib.Processing.HashAlgorithms;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml.Linq;
using Xunit;

namespace AVDump3Tests;

public class CLTests {
	[Fact]
	public void WithoutArgs() {
		var management = AVD3CLModule.Create(null);
		var avd3CLModule = management.GetModule<AVD3CLModule>();
		avd3CLModule.HandleArgs(Array.Empty<string>());

		var settingsModule = management.GetModule<AVD3SettingsModule>();
		CLSettingsHandler.PrintHelp(settingsModule.SettingProperties, null, true);
	}

	[Fact]
	public void HashTests() {
		var hashes = new Dictionary<string, IAVDHashAlgorithm[]> {
				{ "ED2K", new IAVDHashAlgorithm[] { new Ed2kHashAlgorithm() , new Ed2kNativeHashAlgorithm() } },
				{ "CRC32C", new IAVDHashAlgorithm[] { new Crc32CIntelHashAlgorithm() } },
				{ "CRC32", new IAVDHashAlgorithm[] { new Crc32HashAlgorithm() , new Crc32NativeHashAlgorithm() } },
				{ "KECCAK-224", new IAVDHashAlgorithm[] { new KeccakNativeHashAlgorithm(224) } },
				{ "KECCAK-256", new IAVDHashAlgorithm[] { new KeccakNativeHashAlgorithm(256) } },
				{ "KECCAK-384", new IAVDHashAlgorithm[] { new KeccakNativeHashAlgorithm(384) } },
				{ "KECCAK-512", new IAVDHashAlgorithm[] { new KeccakNativeHashAlgorithm(512) } },
				{ "SHA3-224", new IAVDHashAlgorithm[] { new SHA3NativeHashAlgorithm(224) } },
				{ "SHA3-256", new IAVDHashAlgorithm[] { new SHA3NativeHashAlgorithm(256) } },
				{ "SHA3-384", new IAVDHashAlgorithm[] { new SHA3NativeHashAlgorithm(384) } },
				{ "SHA3-512", new IAVDHashAlgorithm[] { new SHA3NativeHashAlgorithm(512) } },
				{ "MD4", new IAVDHashAlgorithm[] { new Md4HashAlgorithm(), new Md4NativeHashAlgorithm() } },
				{ "SHA1", new IAVDHashAlgorithm[] { new SHA1NativeHashAlgorithm() } },
				{ "SHA2-256", new IAVDHashAlgorithm[] { new SHA256NativeHashAlgorithm() } },
				{ "Tiger", new IAVDHashAlgorithm[] { new TigerNativeHashAlgorithm() } },
				{ "TTH", new IAVDHashAlgorithm[] { new TigerTreeHashAlgorithm(1), new TigerTreeHashAlgorithm(3) } },
			};

		var sourceDataCheckAlg = SHA256.Create();
		var testVectors = XElement.Load("HashTestVectors.xml");
		foreach(var hashElem in testVectors.Elements("Hash")) {
			var hashName = (string)hashElem.Attribute("name");
			var hashAlgs = hashes[hashName];
			foreach(var testVectorElem in hashElem.Elements("TestVector")) {


				Span<byte> b = new byte[(int)testVectorElem.Attribute("length")];
				var patternName = (string)testVectorElem.Attribute("pattern");
				switch(patternName) {
					case "BinaryZeros": break;
					case "BinaryOnes": for(int i = 0; i < b.Length; i++) b[i] = 0xFF; break;
					case "ASCIIZeros": for(int i = 0; i < b.Length; i++) b[i] = (byte)'0'; break;
					default: break;
				}

				var bHash = sourceDataCheckAlg.ComputeHash(b.ToArray());
				foreach(var hashAlg in hashAlgs) {
					Debug.Print($"{hashName} - {hashAlg.GetType().Name} - {patternName} - {b.Length}");

					hashAlg.Initialize();

					var bytesProcessed = 0L;
					while(bytesProcessed + hashAlg.BlockSize <= b.Length) {
						hashAlg.TransformFullBlocks(b[..hashAlg.BlockSize]);
						bytesProcessed += hashAlg.BlockSize;
					}
					var hash = hashAlg.TransformFinalBlock(b[^(b.Length % hashAlg.BlockSize)..]);
					Assert.True(!b.SequenceEqual(bHash), "Source Data has been corrupted");

					var hashStr = BitConverter.ToString(hash.ToArray()).Replace("-", "").ToLower();
					var expectedHashStr = ((string)testVectorElem.Attribute("hash"))?.ToLower();

					Assert.True(hashStr.Equals(expectedHashStr), $"{hashName} - {hashAlg.GetType().Name} - {patternName} - {b.Length}: {hashStr} != {expectedHashStr}");
					if(hashAlg.AdditionalHashes.IsEmpty) {
						Assert.True(testVectorElem.Attribute("hash2") == null, $"{hashName} - {hashAlg.GetType().Name} - {patternName} - {b.Length}: Additional hashes expected");
					} else {
						hashStr = BitConverter.ToString(hashAlg.AdditionalHashes[0].ToArray()).Replace("-", "").ToLower();
						expectedHashStr = ((string)testVectorElem.Attribute("hash2"))?.ToLower() ?? "<NoAdditionalHash>";
						Assert.True(hashStr.Equals(expectedHashStr), $"{hashName} - {hashAlg.GetType().Name} - {patternName} - {b.Length}: {hashStr} != {expectedHashStr}");
					}

				}
			}
		}
	}
}
