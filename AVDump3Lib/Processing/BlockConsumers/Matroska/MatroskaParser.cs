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

using System;
using System.IO;
using System.Threading;
using CSEBML;
using CSEBML.DataSource;
using CSEBML.DocTypes;
using CSEBML.DocTypes.Matroska;
using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers.Matroska.EbmlHeader;
using AVDump3Lib.Processing.StreamConsumer;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska {
	public class MatroskaParser : BlockConsumer {
		public MatroskaFile Info { get; private set; }

		public MatroskaParser(string name, IBlockStreamReader reader) : base(name, reader) { }


		protected override void DoWork(CancellationToken ct) {
            IEBMLDataSource dataSrc = null; //new EBMLBlockDataSource(Reader);
			using(var cts = new CancellationTokenSource())
			using(ct.Register(() => cts.Cancel())) {
				var matroskaDocType = new MatroskaDocType(MatroskaVersion.V3);
				var reader = new EBMLReader(dataSrc, matroskaDocType);

				//var updateTask = Task.Factory.StartNew(() => {
				//	long oldProcessedBytes = 0;
				//	int timerRes = 40, ttl = 10000, ticks = ttl / timerRes;
				//	while(IsRunning) {
				//		ProcessedBytes = dataSrc.Position; //TODO: Check for dispose
				//
				//		Thread.Sleep(timerRes); ticks--;
				//		if(oldProcessedBytes != ProcessedBytes) ticks = ttl / timerRes; else if(ticks == 0) cts.Cancel();
				//		oldProcessedBytes = ProcessedBytes;
				//	}
				//}, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);


				var matroskaFile = new MatroskaFile(dataSrc.Length);

				try {
					matroskaFile.Parse(reader, cts.Token);

					if(matroskaFile.Segment != null) {
						Info = matroskaFile;
					}
				} catch(Exception ex) {
					//TODO: Only ignore excpetion when it is not a matroska file
				}

			}
		}
	}
}