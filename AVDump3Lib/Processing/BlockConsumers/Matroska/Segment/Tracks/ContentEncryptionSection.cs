using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks {
	public class ContentEncryptionSection : Section {
		private EncAlgos? contentEncAlgo;
		private SigAlgos? contentSigAlgo;
		private SigHashAlgos? contentSigHashAlgo;
		private byte[] contentEncKeyId, contentSignature, contentSigKeyId;

		public EncAlgos ContentEncAlgo => contentEncAlgo ?? EncAlgos.SignedOnly;
		public SigAlgos ContentSigAlgo => contentSigAlgo ?? SigAlgos.EncryptionOnly;
		public SigHashAlgos ContentSigHashAlgo => contentSigHashAlgo ?? SigHashAlgos.EncryptionOnly;
		public byte[] ContentEncKeyId => (byte[])contentEncKeyId.Clone();
		public byte[] ContentSignature => (byte[])contentSignature.Clone();
		public byte[] ContentSigKeyId => (byte[])contentSigKeyId.Clone();

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.ContentEncAlgo) {
				contentEncAlgo = (EncAlgos)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ContentSigAlgo) {
				contentSigAlgo = (SigAlgos)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ContentSigHashAlgo) {
				contentSigHashAlgo = (SigHashAlgos)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ContentEncKeyID) {
				contentEncKeyId = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ContentSignature) {
				contentSignature = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ContentSigKeyID) {
				contentSigKeyId = (byte[])reader.RetrieveValue();
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("ContentEncAlgo", ContentEncAlgo);
			yield return CreatePair("ContentSigAlgo", ContentSigAlgo);
			yield return CreatePair("ContentSigHashAlgo", ContentSigHashAlgo);
			yield return CreatePair("ContentEncKeyId", ContentEncKeyId);
			yield return CreatePair("ContentSignature", ContentSignature);
			yield return CreatePair("ContentSigKeyId", ContentSigKeyId);
		}


		public enum EncAlgos { SignedOnly = 0, DES = 1, TrippleDES = 2, TwoFish = 3, BlowFish = 4, AES = 5 }
		public enum SigAlgos { EncryptionOnly = 0, RSA = 1 }
		public enum SigHashAlgos { EncryptionOnly = 0, SHA1_160 = 1, MD5 = 2 }
	}
}
