using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Zyan.Communication;
using Zyan.Communication.Protocols.Ipc;
using Zyan.InterLinq;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;
	using ClassInitializeNonStatic = NUnit.Framework.TestFixtureSetUpAttribute;
	using ClassInitialize = DummyAttribute;
	using ClassCleanupNonStatic = NUnit.Framework.TestFixtureTearDownAttribute;
	using ClassCleanup = DummyAttribute;
	using TestContext = System.Object;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for streams.
	/// </summary>
	[TestClass]
	public class StreamTests
	{
		#region Interfaces and components

		interface IStreamService
		{
			Stream OpenRead(string fileName = null);

			Stream Create(string fileName = null);

			Stream CopyData(Stream input);

			void CopyData(Stream input, Stream output);
		}

		class StreamService : IStreamService
		{
			public static byte[] SampleData = new byte[] { 1, 65, 12, 55, 22, 23, 126 };

			public static List<string> TempFileNames = new List<string>();

			public static void DeleteTempFiles()
			{
				foreach (var fn in TempFileNames)
				{
					File.Delete(fn);
				}

				TempFileNames.Clear();
			}

			public Stream OpenRead(string fileName)
			{
				if (string.IsNullOrWhiteSpace(fileName))
				{
					return new MemoryStream(SampleData);
				}

				return File.OpenRead(fileName);
			}

			public Stream Create(string fileName)
			{
				if (string.IsNullOrWhiteSpace(fileName))
				{
					return new MemoryStream();
				}

				TempFileNames.Add(fileName);
				return File.Create(fileName);
			}

			public Stream CopyData(Stream input)
			{
				var output = new MemoryStream();
				input.CopyTo(output);
				output.Position = 0;
				return output;
			}

			public void CopyData(Stream input, Stream output)
			{
				var size = 1000;
				var reader = new BinaryReader(input);
				var writer = new BinaryWriter(output);

				try
				{
					var bytes = reader.ReadBytes(size);

					while (bytes.Length > 0)
					{
						writer.Write(bytes);
						bytes = reader.ReadBytes(size);
					}
				}
				finally
				{
					reader.Close();
					writer.Close();
				}
			}
		}

		#endregion

		#region Initialization and cleanup

		public TestContext TestContext { get; set; }

		static ZyanComponentHost ZyanHost { get; set; }

		static ZyanConnection ZyanConnection { get; set; }

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServer(null);
		}

		[ClassCleanupNonStatic]
		public void Cleanup()
		{
			StopServer();
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			var serverSetup = new IpcBinaryServerProtocolSetup("StreamsTest");
			ZyanHost = new ZyanComponentHost("SampleStreamServer", serverSetup);

			ZyanHost.RegisterComponent<IStreamService, StreamService>();

			var clientSetup = new IpcBinaryClientProtocolSetup();
			ZyanConnection = new ZyanConnection("ipc://StreamsTest/SampleStreamServer", clientSetup);
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
			StreamService.DeleteTempFiles();
		}

		#endregion

		[TestMethod]
		public void ReadRemoteMemoryStream()
		{
			var proxy = ZyanConnection.CreateProxy<IStreamService>();
			var stream = proxy.OpenRead();

			var result = new byte[(int)stream.Length];
			stream.Read(result, 0, result.Length);

			Assert.IsTrue(StreamService.SampleData.SequenceEqual(result));
		}

		[TestMethod]
		public void ReadWriteRemoteMemoryStream()
		{
			var proxy = ZyanConnection.CreateProxy<IStreamService>();
			var stream = proxy.Create();

			// write data to server
			var result = new byte[StreamService.SampleData.Length];
			stream.Write(StreamService.SampleData, 0, result.Length);
			Assert.IsFalse(StreamService.SampleData.SequenceEqual(result));

			// read data from server
			stream.Position = 0;
			stream.Read(result, 0, result.Length);
			Assert.IsTrue(StreamService.SampleData.SequenceEqual(result));
		}

		[TestMethod]
		public void ReadWriteRemoteMemoryStreamViaBinaryReaderAndWriter()
		{
			var proxy = ZyanConnection.CreateProxy<IStreamService>();
			var stream = proxy.OpenRead();

			// read via BinaryReader
			var result = new BinaryReader(stream).ReadBytes((int)stream.Length);
			Assert.IsTrue(StreamService.SampleData.SequenceEqual(result));

			// write via BinaryWriter
			stream = proxy.Create();
			new BinaryWriter(stream).Write(result);

			// read again via BinaryReader
			stream.Position = 0;
			result = new BinaryReader(stream).ReadBytes((int)stream.Length);
			Assert.IsTrue(StreamService.SampleData.SequenceEqual(result));
		}

		[TestMethod]
		public void RemoteCopyMemoryStream()
		{
			var proxy = ZyanConnection.CreateProxy<IStreamService>();
			var stream = proxy.CopyData(new MemoryStream(StreamService.SampleData));

			var result = new byte[(int)stream.Length];
			stream.Read(result, 0, result.Length);

			Assert.IsTrue(StreamService.SampleData.SequenceEqual(result));
		}

		[TestMethod]
		public void CopyRemoteFileStreamToLocalFileStream()
		{
			var inputName = typeof(StreamTests).Assembly.Location;
			var outputName = "TempFile" + Guid.NewGuid();

			var proxy = ZyanConnection.CreateProxy<IStreamService>();
			var input = proxy.OpenRead(inputName);
			var output = File.Create(outputName);

			try
			{
				// copy data
				input.CopyTo(output);
				input.Close();
				output.Close();

				// validate data locally
				var inputData = File.ReadAllBytes(inputName);
				var outputData = File.ReadAllBytes(outputName);
				Assert.IsTrue(inputData.SequenceEqual(outputData));
			}
			finally
			{
				File.Delete(outputName);
			}
		}

		[TestMethod]
		public void CopyLocalFileStreamToRemoteFileStream()
		{
			var inputName = typeof(StreamTests).Assembly.Location;
			var outputName = "TempFile" + Guid.NewGuid();

			var proxy = ZyanConnection.CreateProxy<IStreamService>();
			var input = File.OpenRead(inputName);
			var output = proxy.Create(outputName);

			// copy data
			input.CopyTo(output);
			input.Close();
			output.Close();

			// validate data locally
			var inputData = File.ReadAllBytes(inputName);
			var outputData = File.ReadAllBytes(outputName);
			Assert.IsTrue(inputData.SequenceEqual(outputData));
		}

		[TestMethod]
		public void CopyRemoteFileStreamToRemoteFileStream()
		{
			var inputName = typeof(StreamTests).Assembly.Location;
			var outputName = "TempFile" + Guid.NewGuid();

			var proxy = ZyanConnection.CreateProxy<IStreamService>();
			var input = proxy.OpenRead(inputName);
			var output = proxy.Create(outputName);

			// copy data
			input.CopyTo(output);
			input.Close();
			output.Close();

			// validate data locally
			var inputData = File.ReadAllBytes(inputName);
			var outputData = File.ReadAllBytes(outputName);
			Assert.IsTrue(inputData.SequenceEqual(outputData));
		}

		[TestMethod]
		public void CopyLocalFileStreamsUsingRemoteService()
		{
			var inputName = typeof(StreamTests).Assembly.Location;
			var outputName = "TempFile" + Guid.NewGuid();

			var proxy = ZyanConnection.CreateProxy<IStreamService>();
			var input = File.OpenRead(inputName);
			var output = File.Create(outputName);

			try
			{
				// copy data
				proxy.CopyData(input, output);
				input.Close();
				output.Close();

				// validate data locally
				var inputData = File.ReadAllBytes(inputName);
				var outputData = File.ReadAllBytes(outputName);
				Assert.IsTrue(inputData.SequenceEqual(outputData));
			}
			finally
			{
				File.Delete(outputName);
			}
		}
	}
}
