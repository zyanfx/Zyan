/*
 THIS CODE IS COPIED FROM:
 --------------------------------------------------------------------------------------------------------------
 SmallBlockMemoryStream

 A MemoryStream replacement that avoids using the Large Object Heap
 https://github.com/Aethon/SmallBlockMemoryStream

 Copyright (c) 2014 Brent McCullough.
 --------------------------------------------------------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace Zyan.Communication.Toolbox.IO
{
    internal class SmallBlockMemoryStream : Stream
    {
        public const int StartBlockCount = 10;
        public const int MinBlockSize = 256;
        public const int MaxBlockSize = 85000 - 32; // should fly under the LOH

        private static readonly int[] NoAllocations = new int[0];

        private byte[][] _blocks;
        private int _blockCount;

        private long _length;
        private long _capacity;
        private long _position;

        private int _cursorIndex;
        private long _cursorBase;
        private int _cursorOffset;

        private bool _closed;

        public SmallBlockMemoryStream()
        {}

        public SmallBlockMemoryStream(long capacity)
        {
            if (capacity < 0)
                throw __Error.NeedNonNegNumber("capacity");

            if (capacity > 0)
                ExpandCapacity(capacity, true);
        }

        public override bool CanRead
        {
            get { return !_closed; }
        }

        public override bool CanSeek
        {
            get { return !_closed; }
        }

        public override bool CanWrite
        {
            get { return !_closed; }
        }

        public override void Flush()
        {
            // to remain consistent with MemoryStream, this does not check
            //  the open/closed/disposed state of the stream
        }

        public override long Length
        {
            get
            {
                if (_closed)
                    throw __Error.StreamIsClosed();

                return _length;
            }
        }

        public override long Position
        {
            get
            {
                if (_closed)
                    throw __Error.StreamIsClosed();

                return _position;
            }
            set
            {
                if (_closed)
                    throw __Error.StreamIsClosed();
                if (value < 0)
                    throw __Error.NeedNonNegNumber(null);

                _position = value;
            }
        }

        public virtual long Capacity
        {
            get
            {
                if (_closed)
                    throw __Error.StreamIsClosed();

                return _capacity;
            }

            set
            {
                if (_closed)
                    throw __Error.StreamIsClosed();
                if (value < Length)
                    throw __Error.StreamCapacityLessThanLength(null);

                if (value == 0)
                {
                    _blocks = null;
                    _blockCount = 0;
                    _capacity = 0;
                    return;
                }

                var capacity = _capacity;
                if (value > capacity)
                {
                    ExpandCapacity(value, true);
                }
                else if (value < capacity)
                {
                    // truncate to the specified size
                    SetCursor(value);
                    var cursorIndex = _cursorIndex;
                    var cursorOffset = _cursorOffset;
                    var blocks = _blocks;
                    var block = blocks[cursorIndex];
                    if (cursorOffset < block.Length)
                    {
                        var newBlock = new byte[cursorOffset];
                        Buffer.BlockCopy(block, 0, newBlock, 0, newBlock.Length);
                        blocks[cursorIndex] = newBlock;
                    }
                    var blockCount = _blockCount;
                    for (var i = cursorIndex + 1; i < blockCount; i++)
                        blocks[i] = null;
                    _blocks = blocks;
                    _blockCount = cursorIndex + 1;
                    _capacity = value;
                    SetCursor(_position);
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_closed)
                throw __Error.StreamIsClosed();
            if (buffer == null)
                throw __Error.NullArgument("buffer");
            if (offset < 0)
                throw __Error.NeedNonNegNumber("offset");
            if (count < 0)
                throw __Error.NeedNonNegNumber("count");
            if (buffer.Length - offset < count)
                throw __Error.InvalidOffset("offset");

            var position = _position;
            if (count == 0 || position >= _length) return 0;

            var cursorBase = _cursorBase;
            var cursorOffset = _cursorOffset;
            if (position != cursorBase + cursorOffset)
            {
                SetCursor(position);
                cursorBase = _cursorBase;
                cursorOffset = _cursorOffset;
            }
            var cursorIndex = _cursorIndex;

            var read = 0;
            var toRead = _length - cursorBase - cursorOffset;
            if (count < toRead)
                toRead = count;
            while (toRead > 0)
            {
                var block = _blocks[cursorIndex];
                var blockLength = block.Length;
                var available = blockLength - cursorOffset;
                var readCount = (int)(available < toRead ? available : toRead);
                Buffer.BlockCopy(block, cursorOffset, buffer, offset + read, readCount);
                toRead -= readCount;
                read += readCount;
                cursorOffset += readCount;
                if (cursorOffset != blockLength) break;
                cursorIndex++;
                cursorBase += blockLength;
                cursorOffset = 0;
            }

            _cursorIndex = cursorIndex;
            _cursorOffset = cursorOffset;
            _cursorBase = cursorBase;
            _position = cursorBase + cursorOffset;

            return read;
        }

        public override int ReadByte()
        {
            if (_closed)
                throw __Error.StreamIsClosed();

            var position = _position;
            if (position >= _length) return -1;

            var cursorBase = _cursorBase;
            var cursorOffset = _cursorOffset;
            if (position != cursorBase + cursorOffset)
            {
                SetCursor(position);
                cursorBase = _cursorBase;
                cursorOffset = _cursorOffset;
            }
            var cursorIndex = _cursorIndex;

            var block = _blocks[cursorIndex];
            var result = block[cursorOffset++];

            var blockLength = block.Length;
            if (cursorOffset == blockLength)
            {
                _cursorIndex = cursorIndex + 1;
                _cursorBase = cursorBase + blockLength;
                cursorOffset = 0;
            }
            _cursorOffset = cursorOffset;
            _position = _cursorBase + cursorOffset;

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (_closed)
                throw __Error.StreamIsClosed();

            long newPos;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPos = offset;
                    if (newPos < 0)
                        throw __Error.SeekBeforeBegin();
                    break;
                case SeekOrigin.Current:
                    newPos = _position + offset;
                    if (newPos < 0)
                        throw __Error.SeekBeforeBegin();
                    break;
                case SeekOrigin.End:
                    newPos = _length + offset;
                    if (newPos < 0)
                        throw __Error.SeekBeforeBegin();
                    break;
                default:
                    throw __Error.UnknownSeekOrigin(origin, "origin");
            }

            return _position = newPos;
        }

        public override void SetLength(long value)
        {
            if (_closed)
                throw __Error.StreamIsClosed();
            if (value < 0)
                throw __Error.NeedNonNegNumber("value");

            if (value > _capacity)
                ExpandCapacity(value);
            else if (value < _length)
            {
                // zero out the area we are "discarding"
                var index = 0;
                var start = value;
                var count = _length - start;
                do
                {
                    var size = _blocks[index].Length;
                    if (start < size) break;
                    start -= size;
                    index++;
                } while (true);
                do
                {
                    var block = _blocks[index];
                    var available = (int)(block.Length - start);
                    var toClear = (int)(available < count ? available : count);
                    Array.Clear(block, (int)start, toClear);
                    count -= toClear;
                    start = 0;
                    index++;
                } while (count > 0);

                if (value < _position)
                {
                    _position = value;
                    SetCursor(value);
                }
            }
            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_closed)
                throw __Error.StreamIsClosed();
            if (buffer == null)
                throw __Error.NullArgument("buffer");
            if (offset < 0)
                throw __Error.NeedNonNegNumber("offset");
            if (count < 0)
                throw __Error.NeedNonNegNumber("count");
            if (buffer.Length - offset < count)
                throw __Error.InvalidOffset("offset");

            if (count == 0) return;

            var position = _position;
            if (position + count > _capacity)
                ExpandCapacity(position + count);

            var cursorBase = _cursorBase;
            var cursorOffset = _cursorOffset;
            if (position != cursorBase + cursorOffset)
            {
                SetCursor(position);
                cursorBase = _cursorBase;
                cursorOffset = _cursorOffset;
            }

            var cursorIndex = _cursorIndex;
            do
            {
                var block = _blocks[cursorIndex];
                var blockLength = block.Length;
                var writeAvailable = blockLength - cursorOffset;
                var writeCount = writeAvailable < count ? writeAvailable : count;
                Buffer.BlockCopy(buffer, offset, block, cursorOffset, writeCount);
                count -= writeCount;
                offset += writeCount;
                cursorOffset += writeCount;
                if (cursorOffset != blockLength) break;
                cursorIndex++;
                cursorBase += blockLength;
                cursorOffset = 0;
            } while (count > 0);

            _cursorIndex = cursorIndex;
            _cursorOffset = cursorOffset;
            _cursorBase = cursorBase;

            position = cursorBase + cursorOffset;
            if (position > _length)
                _length = position;
            _position = position;
        }

        public override void WriteByte(byte value)
        {
            if (_closed)
                throw __Error.StreamIsClosed();

            var position = _position;
            if (position + 1 > _capacity)
                ExpandCapacity(position + 1);

            var cursorBase = _cursorBase;
            var cursorOffset = _cursorOffset;
            if (position != cursorBase + cursorOffset)
            {
                SetCursor(position);
                cursorBase = _cursorBase;
                cursorOffset = _cursorOffset;
            }

            var cursorIndex = _cursorIndex;

            var block = _blocks[cursorIndex];
            block[cursorOffset++] = value;

            var size = block.Length;
            if (cursorOffset == size)
            {
                cursorIndex++;
                cursorBase += size;
                cursorOffset = 0;
            }

            _cursorIndex = cursorIndex;
            _cursorOffset = cursorOffset;
            _cursorBase = cursorBase;

            position = cursorBase + cursorOffset;
            if (position > _length)
                _length = position;
            _position = position;
        }

        public virtual void WriteTo(Stream stream)
        {
            if (stream == null)
                throw __Error.NullArgument("stream");
            if (_closed)
                throw __Error.StreamIsClosed();
            var toWrite = _length;
            for (var i = 0; toWrite > 0; i++)
            {
                var block = _blocks[i];
                var count = (int)(toWrite > block.Length ? block.Length : toWrite);
                stream.Write(block, 0, count);
                toWrite -= count;
            }
        }

        public int[] GetAllocationSizes()
        {
            if (_blocks == null)
                return NoAllocations;

            var result = new List<int>();
            foreach (var block in _blocks)
                result.Add(block == null ? -1 : block.Length);

            return result.ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closed = true;
                _blocks = null;
            }
            base.Dispose(disposing);
        }

        private void SetCursor(long position)
        {
            var cursorBase = 0;
            var cursorIndex = 0;
            var blocks = _blocks;
            while (cursorIndex < _blockCount)
            {
                var blockLength = blocks[cursorIndex].Length;
                if (position < blockLength) break;
                cursorBase += blockLength;
                cursorIndex++;
                position -= blockLength;
            }
            _cursorIndex = cursorIndex;
            _cursorBase = cursorBase;
            _cursorOffset = (int)position;
        }

        private void ExpandCapacity(long newCapacity, bool allocateExactly = false)
        {
            var capacity = _capacity;

            // determine required allocations based on the MemoryStream algorithm
            var toAllocate = newCapacity - capacity;
            if (!allocateExactly)
            {
                toAllocate = toAllocate > MinBlockSize ? toAllocate : MinBlockSize;
                if (capacity > toAllocate)
                    toAllocate = capacity; // this effects the doubling-algorithm from MemoryStream
            }
            var bigBlocks = 0;
            var tailBlockSize = 0;
            if (toAllocate <= MaxBlockSize)
            {
                tailBlockSize = (int)toAllocate;
            }
            else
            {
                bigBlocks = (int) (toAllocate/MaxBlockSize);
                var extra = (int)(toAllocate - bigBlocks*MaxBlockSize);
                if (allocateExactly)
                    tailBlockSize = extra;
                else if (extra > 0)
                    bigBlocks++;
            }

            // extend the block array as necessary
            var blocks = _blocks;
            var blockCount = _blockCount;
            var totalBlocksRequired = blockCount + bigBlocks + (tailBlockSize > 0 ? 1 : 0);
            if (blocks == null)
            {
                var n = StartBlockCount;
                while (n < totalBlocksRequired)
                    n *= 2;
                blocks = new byte[n][];
                _blocks = blocks;
            }
            else if (totalBlocksRequired > _blocks.Length)
            {
                var n = blocks.Length;
                while (n < totalBlocksRequired)
                    n *= 2;
                var nextblocks = new byte[n][];
                Array.Copy(blocks, nextblocks, blocks.Length);
                blocks = nextblocks;
                _blocks = blocks;
            }

            // create the big blocks
            for (var i = 0; i < bigBlocks; i++)
            {
                blocks[blockCount++] = new byte[MaxBlockSize];
                capacity += MaxBlockSize;
            }
            if (tailBlockSize > 0)
            {
                blocks[blockCount++] = new byte[tailBlockSize];
                capacity += tailBlockSize;
            }

            _blockCount = blockCount;
            _capacity = capacity;
        }
    }
}
