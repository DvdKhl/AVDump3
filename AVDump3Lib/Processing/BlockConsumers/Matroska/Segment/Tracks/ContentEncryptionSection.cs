using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Tracks {
    public class ContentEncryptionSection : Section {
		private EncAlgos? contentEncAlgo;
		private SigAlgos? contentSigAlgo;
		private SigHashAlgos? contentSigHashAlgo;
		private byte[] contentEncKeyId, contentSignature, contentSigKeyId;

		public EncAlgos ContentEncAlgo { get { return contentEncAlgo ?? EncAlgos.SignedOnly; } }
		public SigAlgos ContentSigAlgo { get { return contentSigAlgo ?? SigAlgos.EncryptionOnly; } }
		public SigHashAlgos ContentSigHashAlgo { get { return contentSigHashAlgo ?? SigHashAlgos.EncryptionOnly; } }
		public byte[] ContentEncKeyId { get { return (byte[])contentEncKeyId.Clone(); } }
		public byte[] ContentSignature { get { return (byte[])contentSignature.Clone(); } }
		public byte[] ContentSigKeyId { get { return (byte[])contentSigKeyId.Clone(); } }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ContentEncAlgo.Id) {
				contentEncAlgo = (EncAlgos)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ContentSigAlgo.Id) {
				contentSigAlgo = (SigAlgos)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ContentSigHashAlgo.Id) {
				contentSigHashAlgo = (SigHashAlgos)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ContentEncKeyID.Id) {
				contentEncKeyId = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ContentSignature.Id) {
				contentSignature = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ContentSigKeyID.Id) {
				contentSigKeyId = (byte[])reader.RetrieveValue(elemInfo);
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
