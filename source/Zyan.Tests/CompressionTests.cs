using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Zyan.Communication.Toolbox.Compression;
using Zyan.Communication.ChannelSinks.Compression;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;
	using ClassInitializeNonStatic = NUnit.Framework.OneTimeSetUpAttribute;
	using ClassInitialize = DummyAttribute;
	using ClassCleanupNonStatic = NUnit.Framework.OneTimeTearDownAttribute;
	using ClassCleanup = DummyAttribute;
	using TestContext = System.Object;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassCleanupNonStatic = DummyAttribute;
	using ClassInitializeNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for compression-related classes.
	///</summary>
	[TestClass]
	public class CompressionTests
	{
		private static byte[] SampleData { get; } = CreateSampleData();

		private static byte[] CreateSampleData()
		{
			// compressible data
			using (var ms = new MemoryStream())
			{
				new BinaryFormatter().Serialize(ms, SampleEntity.GetSampleEntities());
				return ms.ToArray();
			}
		}

		[TestMethod]
		public void CompressionHelper_CompressesData()
		{
			// test all available compression methods
			foreach (var level in new[] { CompressionMethod.None, CompressionMethod.LZF, CompressionMethod.DeflateStream })
			{
				var inputStream = new MemoryStream(SampleData);
				var outputStream = CompressionHelper.Compress(inputStream, level);

				Assert.IsNotNull(outputStream);
				Assert.IsTrue(outputStream.Length > 0);
				Assert.IsTrue(outputStream.Length <= inputStream.Length);

				if (level == CompressionMethod.None)
				{
					Assert.AreSame(inputStream, outputStream);
				}
			}
		}

		[TestMethod]
		public void CompressionHelper_DecompressesData()
		{
			// test all available compression methods
			foreach (var level in new[] { CompressionMethod.None, CompressionMethod.LZF, CompressionMethod.DeflateStream })
			{
				var source = SampleData;
				var compressed = CompressionHelper.Compress(new MemoryStream(source), level);
				var decompressed = CompressionHelper.Decompress(compressed, level);
				var destination = new byte[(int)decompressed.Length];
				decompressed.Read(destination, 0, destination.Length);

				Assert.AreEqual(source.Length, destination.Length);
				Assert.IsTrue(Enumerable.SequenceEqual(source, destination));

				if (level == CompressionMethod.None)
				{
					Assert.AreSame(compressed, decompressed);
				}
			}
		}
	}
}
