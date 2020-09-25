/*
 THIS CODE IS BASED ON:
 --------------------------------------------------------------------------------------------------------------
 SmallBlockMemoryStream

 A MemoryStream replacement that avoids using the Large Object Heap
 https://github.com/Aethon/SmallBlockMemoryStream

 Copyright (c) 2014 Brent McCullough.
 --------------------------------------------------------------------------------------------------------------
*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Equivalency;
using NUnit.Framework;
using Zyan.Communication.Toolbox.IO;

namespace Tests
{
    /// <summary>
	/// Test class for SmallBlockMemoryStream
	/// </summary>
	[TestClass, ExcludeFromCodeCoverage]
    public class SmallBlockMemoryStreamTests
    {
        private static readonly int[] NoAllocations = new int[0];

        private static readonly byte[] BaseDataPattern =
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
        };

        private static byte[] MakeTestData(long length)
        {
            var data = new byte[length];

            byte iter = 0;
            for (var i = 0; i < length; )
            {
                for (var mod = 0; mod < BaseDataPattern.Length && i < length; mod++, i++)
                    data[i] = (byte)(BaseDataPattern[mod] + iter);
                iter++; // allow this to roll
            }

            return data;
        }

        private static readonly byte[] TestData = MakeTestData(1000000);
        private const int SmallBlockSize = SmallBlockMemoryStream.MinBlockSize - 1;
        private const int OneBlock = SmallBlockMemoryStream.MinBlockSize;
        private const int BlockPlus = SmallBlockMemoryStream.MinBlockSize + 1;
        private const int LargeBlock = 5000;

        private static EquivalencyAssertionOptions<SmallBlockMemoryStream> EqOpts(
            EquivalencyAssertionOptions<SmallBlockMemoryStream> options)
        {
            return options.ExcludingMissingProperties();
        }

        #region Construction
        [Test]
        public void DefaultCtor_succeeds()
        {
            using (var standard = new MemoryStream())
            using (var subject = new SmallBlockMemoryStream())
                AssertEquivalent(standard, subject);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(500)]
        [TestCase(10000)]
        [TestCase(1000000)]
        public void CtorWithCapacity_succeeds(int capacity)
        {
            using (var standard = new MemoryStream(capacity))
            using (var subject = new SmallBlockMemoryStream(capacity))
                AssertEquivalent(standard, subject);
        }

        [Test]
        public void CtorWithCapacity_WithBadParameter_throws()
        {
            ((Action)(() => new MemoryStream(-1))).ShouldThrow<ArgumentOutOfRangeException>();
            ((Action)(() => new SmallBlockMemoryStream(-1))).ShouldThrow<ArgumentOutOfRangeException>();
        }
        #endregion

        #region Capacity
        [Test]
        public void SetCapacity_WithBadValue_Throws()
        {
            VerifyThrows<ArgumentOutOfRangeException>(s =>
            {
                s.Write(TestData, 0, 100);

                CallSetCapacity(s, 10);
            });
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(SmallBlockMemoryStream.MinBlockSize - 1)]
        [TestCase(SmallBlockMemoryStream.MinBlockSize)]
        [TestCase(SmallBlockMemoryStream.MinBlockSize + 1)]
        [TestCase(5000)]
        [TestCase(SmallBlockMemoryStream.MaxBlockSize - 1)]
        [TestCase(SmallBlockMemoryStream.MaxBlockSize)]
        [TestCase(SmallBlockMemoryStream.MaxBlockSize + 1)]
        [TestCase(1000000)]
        public void SetCapacity_WhenExpanding_Succeeds(int capacity)
        {
            VerifyAction(s => CallSetCapacity(s, capacity));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(SmallBlockMemoryStream.MinBlockSize - 1)]
        [TestCase(SmallBlockMemoryStream.MinBlockSize)]
        [TestCase(SmallBlockMemoryStream.MinBlockSize + 1)]
        [TestCase(5000)]
        [TestCase(SmallBlockMemoryStream.MaxBlockSize - 1)]
        [TestCase(SmallBlockMemoryStream.MaxBlockSize)]
        [TestCase(SmallBlockMemoryStream.MaxBlockSize + 1)]
        [TestCase(1000000)]
        public void SetCapacity_WhenContracting_Succeeds(int capacity)
        {
            VerifyAction(s =>
            {
                CallSetCapacity(s, 2000000);

                CallSetCapacity(s, capacity);
            });
        }

        #endregion

        #region Length
        [Test]
        public void SetLength_withBadValues_Throws()
        {
            VerifyThrows<ArgumentException>(s => s.SetLength(-10));
        }

        [Test]
        public void SetLength_OnANewStream_succeeds()
        {
            VerifyAction(s =>
                s.SetLength(5123)
            );
        }

        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, SmallBlockMemoryStream.MinBlockSize - 1)]
        [TestCase(0, SmallBlockMemoryStream.MinBlockSize)]
        [TestCase(0, SmallBlockMemoryStream.MinBlockSize + 1)]
        [TestCase(0, 5000)]
        [TestCase(0, SmallBlockMemoryStream.MaxBlockSize - 1)]
        [TestCase(0, SmallBlockMemoryStream.MaxBlockSize)]
        [TestCase(0, SmallBlockMemoryStream.MaxBlockSize + 1)]
        [TestCase(0, 5000000)]
        [TestCase(100000, 0)]
        [TestCase(100000, 1)]
        [TestCase(100000, SmallBlockMemoryStream.MinBlockSize - 1)]
        [TestCase(100000, SmallBlockMemoryStream.MinBlockSize)]
        [TestCase(100000, SmallBlockMemoryStream.MinBlockSize + 1)]
        [TestCase(100000, 5000)]
        [TestCase(100000, SmallBlockMemoryStream.MaxBlockSize - 1)]
        [TestCase(100000, SmallBlockMemoryStream.MaxBlockSize)]
        [TestCase(100000, SmallBlockMemoryStream.MaxBlockSize + 1)]
        public void SetLength_succeeds(int from, int to)
        {
            VerifyAction(s =>
            {
                if (from > 0)
                    s.Write(TestData, 0, from);
                s.SetLength(to);
            });
        }

        #endregion

        #region Seek
        [Test]
        public void Seek_withBadParameters_fails()
        {
            var subject = new SmallBlockMemoryStream();
            Action action = () => subject.Seek(0, (SeekOrigin)123);
            action.ShouldThrow<ArgumentException>();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(500)]
        [TestCase(999)]
        [TestCase(1000)]
        [TestCase(1001)]
        public void Seek_FromBeginning_Succeeds(long offset)
        {
            VerifyAction(s =>
            {
                s.Write(TestData, 0, 1000);
                s.Seek(offset, SeekOrigin.Begin);
            });
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-500)]
        [TestCase(-999)]
        [TestCase(-1000)]
        public void Seek_FromEnd_Succeeds(long offset)
        {
            VerifyAction(s =>
            {
                s.Write(TestData, 0, 1000);
                s.Seek(offset, SeekOrigin.End);
            });
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-500)]
        [TestCase(499)]
        [TestCase(500)]
        [TestCase(501)]
        public void Seek_FromMiddle_Succeeds(long offset)
        {
            VerifyAction(s =>
            {
                s.Write(TestData, 0, 1000);
                s.Position = 500;

                s.Seek(offset, SeekOrigin.Current);
            });
        }

        [Test]
        public void Seek_FromBeginning_Throws()
        {
            VerifyThrows<IOException>(s =>
            {
                s.Write(TestData, 0, 1000);
                s.Seek(-1, SeekOrigin.Begin);
            });
        }

        [Test]
        public void Seek_FromEnd_Throws()
        {
            VerifyThrows<IOException>(s =>
            {
                s.Write(TestData, 0, 1000);
                s.Seek(-1001, SeekOrigin.End);
            });
        }

        [Test]
        public void Seek_FromMiddle_Throws()
        {
            VerifyThrows<IOException>(s =>
            {
                s.Write(TestData, 0, 1000);
                s.Position = 500;

                s.Seek(-501, SeekOrigin.Current);
            });
        }
        #endregion

        #region Flush
        [Test]
        public void Flush_doesNotFail()
        {
            new SmallBlockMemoryStream().Flush();
        }
        #endregion

        #region Disposal

        [Test]
        public void Dispose_LeavesSafePropertiesInTheCorrectState()
        {
            var subject = new SmallBlockMemoryStream();

            subject.Dispose();

            subject.ShouldBeEquivalentTo(new
            {
                CanRead = false,
                CanSeek = false,
                CanWrite = false,
            }, EqOpts);
            subject.GetAllocationSizes().ShouldBeEquivalentTo(NoAllocations);
        }

        [Test]
        public void Flush_AfterDispose_DoesNotThrow()
        {
            // Just to mimic the MemoryStream implementation
            var standard = new MemoryStream();
            standard.Dispose();
            standard.Flush();
            var subject = new SmallBlockMemoryStream();
            subject.Dispose();
            subject.Flush();
        }

        [Test]
        public void AfterDispose_UnusableMethodsThrow()
        {
            var subject = new SmallBlockMemoryStream();
            subject.Dispose();

            long dummy;
            var buffer = new byte[1];
            var dummyTarget = new MemoryStream();
            var actions = new Action[]
            {
                () => dummy = subject.Length,
                () => subject.SetLength(0),
                () => dummy = subject.Capacity,
                () => subject.Capacity = 100,
                () => dummy = subject.Position,
                () => subject.Position = 0,
                () => subject.Seek(0, SeekOrigin.Begin),
                () => subject.Write(buffer, 0, 1),
                () => subject.WriteByte(1),
                () => CallWriteTo(subject, dummyTarget),
                () => subject.Read(buffer, 0, 1),
                () => subject.ReadByte()
            };

            foreach (var action in actions)
                action.ShouldThrow<ObjectDisposedException>();
        }
        #endregion

        #region Write
        [Test]
        public void Write_withBadParameters_fails()
        {
            var subject = new SmallBlockMemoryStream();
            var data = MakeTestData(10);

            Action action = () => subject.Write(null, 0, 0);
            action.ShouldThrow<ArgumentException>();

            action = () => subject.Write(data, -10, 0);
            action.ShouldThrow<ArgumentException>();

            action = () => subject.Write(data, 0, -10);
            action.ShouldThrow<ArgumentException>();

            action = () => subject.Write(data, 0, 20);
            action.ShouldThrow<ArgumentException>();
        }

        [TestCase(0, 0)]
        [TestCase(SmallBlockSize, 0)]
        [TestCase(SmallBlockSize, SmallBlockSize)]
        [TestCase(SmallBlockSize, LargeBlock)]
        [TestCase(OneBlock, 0)]
        [TestCase(OneBlock, SmallBlockSize)]
        [TestCase(OneBlock, OneBlock)]
        [TestCase(OneBlock, BlockPlus)]
        [TestCase(BlockPlus, 0)]
        [TestCase(512, 1)] // exercises the doubling algorithm
        [TestCase(SmallBlockSize, 1000000)] // exercises expansion of the block array
        public void Write_succeeds(int first, int second)
        {
            VerifyAction(s =>
            {
                s.Write(TestData, 0, first);
                if (second != 0)
                    s.Write(TestData, 0, second);
            });
        }

        [Test]
        public void Write_AfterPosition_succeeds()
        {
            VerifyAction(s =>
            {
                s.Position = 10;
                s.Write(TestData, 0, 100);
            });
        }

        [Test]
        public void WriteByte_succeeds()
        {
            VerifyAction(s =>
            {
                for (var i = 0; i < BlockPlus; i++)
                    s.WriteByte(TestData[i]);
            });
        }

        [Test]
        public void WriteByte__AfterPosition_succeeds()
        {
            VerifyAction(s =>
            {
                s.Position = 10;
                s.WriteByte(14);
            });
        }
        #endregion

        #region Read
        [Test]
        public void Read_withBadParameters_fails()
        {
            var subject = new SmallBlockMemoryStream();
            var data = new byte[10];

            Action action = () => subject.Read(null, 0, 0);
            action.ShouldThrow<ArgumentException>();

            action = () => subject.Read(data, -10, 0);
            action.ShouldThrow<ArgumentException>();

            action = () => subject.Read(data, 0, -10);
            action.ShouldThrow<ArgumentException>();

            action = () => subject.Read(data, 0, 20);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Read_OnAStreamPartiallySizedBySetLength_succeeds()
        {
            VerifyAction(s =>
            {
                s.Write(TestData, 0, BlockPlus);
                s.SetLength(0);
                s.SetLength(BlockPlus);
            });
        }

        [Test]
        public void Read_OnAStreamSizedBySetLengthToMidBlock_ReturnsClearedData()
        {
            VerifyAction(s =>
            {
                s.Write(TestData, 0, OneBlock);
                s.Write(TestData, 0, OneBlock);
                s.SetLength(BlockPlus);
                s.SetLength(OneBlock * 2);
            });
        }

        [Test]
        public void ReadPastEnd_succeedsWithCorrectReadLength()
        {
            const int dataLength = 100;
            const int readLength = 110;
            var writeData = MakeTestData(dataLength);
            var subject = new SmallBlockMemoryStream();
            subject.Write(writeData, 0, dataLength);
            subject.Position = 0;
            var readData = new byte[readLength];
            var read = subject.Read(readData, 0, readLength);
            Assert.AreEqual(writeData.Length, read);
        }

        [Test]
        public void Read_AfterPositionPastLength_succeds()
        {
            VerifyResultsAreEqual(s =>
            {
                s.Write(TestData, 0, 10);
                s.Position = 20;
                var buffer = new byte[10];
                return s.Read(buffer, 0, 10);
            });
        }

        [Test]
        public void ReadByte_succeeds()
        {
            using (var standard = new MemoryStream())
            using (var subject = new SmallBlockMemoryStream())
            {
                standard.Write(TestData, 0, TestData.Length);
                standard.Position = 0;
                subject.Write(TestData, 0, TestData.Length);
                subject.Position = 0;

                for (var i = 0; i <= TestData.Length; i++)
                    subject.ReadByte().Should().Be(standard.ReadByte());
            }
        }

        [Test]
        public void ReadByte_PastEndOfStream_succeeds()
        {
            using (var standard = new MemoryStream())
            using (var subject = new SmallBlockMemoryStream())
            {
                standard.Write(TestData, 0, TestData.Length);
                subject.Write(TestData, 0, TestData.Length);

                for (var i = 0; i <= TestData.Length; i++)
                    subject.ReadByte().Should().Be(standard.ReadByte());
            }
        }
        #endregion

        #region Position
        [Test]
        public void Position_withBadParameters_fails()
        {
            var subject = new SmallBlockMemoryStream();
            Action action = () => subject.Position = -10;
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void SetPosition_BeyondEndOfStream_succeeds()
        {
            VerifyAction(s =>
            {
                s.Write(TestData, 0, 10);
                s.Position = 50;
            });
        }
        #endregion

        #region WriteTo
        [Test]
        public void WriteTo_WithNoStream_Throws()
        {
            VerifyThrows<ArgumentNullException>(s => CallWriteTo(s, null));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1000000)]
        public void WriteTo_succeeds(int size)
        {
            VerifyResultsAreEqual(s =>
            {
                var target = new MemoryStream(size);
                s.Write(TestData, 0, size);
                CallWriteTo(s, target);
                return target.GetBuffer();
            });
        }
        #endregion

        private static void VerifyThrows<T>(Action<Stream> action) where T : Exception
        {
            using (var standard = new MemoryStream())
            using (var subject = new SmallBlockMemoryStream())
            {
                ((Action)(() => action(standard))).ShouldThrow<T>();
                ((Action)(() => action(subject))).ShouldThrow<T>();
            }
        }

        private static void VerifyAction(Action<Stream> action)
        {
            using (var standard = new MemoryStream())
            using (var subject = new SmallBlockMemoryStream())
            {
                action(standard);
                action(subject);

                AssertEquivalent(standard, subject);
            }
        }

        private static void VerifyResultsAreEquivalent<T>(Func<Stream, T> func)
        {
            using (var standard = new MemoryStream())
            using (var subject = new SmallBlockMemoryStream())
            {
                func(standard).ShouldBeEquivalentTo(func(subject));

                AssertEquivalent(standard, subject);
            }
        }

        private static void VerifyResultsAreEqual<T>(Func<Stream, T> func)
        {
            using (var standard = new MemoryStream())
            using (var subject = new SmallBlockMemoryStream())
            {
                Assert.AreEqual(func(standard), func(subject));

                AssertEquivalent(standard, subject);
            }
        }

        private static void AssertEquivalent(MemoryStream standard, SmallBlockMemoryStream subject)
        {
            // length and position should be identical to standard
            subject.Length.Should().Be(standard.Length);
            subject.Position.Should().Be(standard.Position);

            // allocations should never exceed LOH limit
            var allocationSizes = subject.GetAllocationSizes();
            allocationSizes.Any(x => x > SmallBlockMemoryStream.MaxBlockSize)
                .Should().BeFalse();

            // capacity should match allocations
            var calculatedCapacity = allocationSizes.Sum(x => (long)Math.Max(0, x));
            subject.Capacity.Should().Be(calculatedCapacity);

            // total allocation should be identical to the standard until the LOH limit
            //  is exceeded...
            if (standard.Capacity < SmallBlockMemoryStream.MaxBlockSize)
            {
                calculatedCapacity.Should().Be(standard.Capacity);
            }

            // contents of the stream should be identical to the standard
            Assert.AreEqual(standard, subject);
        }

        // Capacity and WriteTo are not part of any contract common to MS and SBMS => call these from within tests
        //  to homogenize the action
        private static void CallSetCapacity(Stream stream, int capacity)
        {
            var ms = stream as MemoryStream;
            if (ms != null)
            {
                ms.Capacity = capacity;
                return;
            }
            var sbms = stream as SmallBlockMemoryStream;
            if (sbms != null)
            {
                sbms.Capacity = capacity;
                return;
            }
            throw new ArgumentException("must be a MemoryStream or a SmallBlockMemoryStream", "stream");
        }
        private static void CallWriteTo(Stream stream, Stream target)
        {
            var ms = stream as MemoryStream;
            if (ms != null)
            {
                ms.WriteTo(target);
                return;
            }
            var sbms = stream as SmallBlockMemoryStream;
            if (sbms != null)
            {
                sbms.WriteTo(target);
                return;
            }
            throw new ArgumentException("must be a MemoryStream or a SmallBlockMemoryStream", "stream");
        }
    }
}
