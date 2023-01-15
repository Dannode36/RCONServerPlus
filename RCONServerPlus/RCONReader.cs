﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace RCONServerPlus
{
	internal class RCONReader : IDisposable
	{
		public static readonly RCONReader INSTANCE = new RCONReader();
		private bool isInit = false;
		private BinaryReader reader = null;
		private ConcurrentBag<RCONMessageAnswer> answers = new ConcurrentBag<RCONMessageAnswer>();

		public RCONReader()
		{
			isInit = false;
		}
		public void setup(BinaryReader reader)
		{
			this.reader = reader;
			isInit = true;
			readerThread();
		}
		public RCONMessageAnswer getAnswer(int messageId)
		{
			var matching = answers.Where(n => n.ResponseId == messageId).ToList();
			var data = new List<byte>();
			var dummy = RCONMessageAnswer.EMPTY;
			
			if(matching.Count > 0)
			{
				matching.ForEach(n => { data.AddRange(n.Data); answers.TryTake(out dummy);});
				return new RCONMessageAnswer(true, data.ToArray(), messageId);
			}
			else
			{
				return RCONMessageAnswer.EMPTY;
			}
		}
		private void readerThread()
		{
			Task.Factory.StartNew(() =>
			{
			    while(true)
			    {
			    	if(isInit == false)
			    	{
			    		return;
			    	}
			    	
			    	try
			    	{
			    		var len = reader.ReadInt32();
			    		var reqId = reader.ReadInt32();
			    		var type = reader.ReadInt32();
			    		var data = len > 10 ? this.reader.ReadBytes(len - 10): new byte[] { };
			    		var pad = reader.ReadBytes(2);
			    		var msg = new RCONMessageAnswer(reqId > -1, data, reqId);
			    		answers.Add(msg);
			    	}
			    	catch(EndOfStreamException e)
			    	{
			    		return;
			    	}
			    	catch(ObjectDisposedException e)
			    	{
			    		return;
			    	}
			    	catch
			    	{
			    		return;
			    	}
			    	
			    	Thread.Sleep(1);
			    }
			}, TaskCreationOptions.LongRunning);
		}

		#region IDisposable implementation
		public void Dispose()
		{
			this.isInit = false;
		}
		#endregion
	}
}