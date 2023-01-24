﻿using Microsoft.Xna.Framework.Content;

namespace FEZRepacker.Converter.XNB
{
    internal class XnbCompressor
    {
        public static Stream Decompress(Stream xnbStream)
        {
            var decompressedStream = new MemoryStream();

            if (!XnbHeader.TryRead(xnbStream, out var header) || (header.Flags & XnbFlags.Compressed) == 0)
            {
                xnbStream.Position = 0;
                xnbStream.CopyTo(decompressedStream);
            }
            else
            {
                using var xnbReader = new BinaryReader(xnbStream);
                LzxDecoder decoder = new LzxDecoder(16);

                int compressedSize = xnbReader.ReadInt32();
                int decompressedSize = xnbReader.ReadInt32();

                long startPos = xnbStream.Position;
                long pos = startPos;

                while (pos - startPos < compressedSize)
                {
                    // all of these shorts are big endian
                    int flag = xnbStream.ReadByte();
                    int frameSize, blockSize;
                    if (flag == 0xFF)
                    {
                        frameSize = (xnbStream.ReadByte() << 8) | xnbStream.ReadByte();
                        blockSize = (xnbStream.ReadByte() << 8) | xnbStream.ReadByte();
                        pos += 5;
                    }
                    else
                    {
                        frameSize = 0x8000;
                        blockSize = (flag << 8) | xnbStream.ReadByte();
                        pos += 2;
                    }


                    if (blockSize == 0 || frameSize == 0) break;

                    decoder.Decompress(xnbStream, blockSize, decompressedStream, frameSize);
                    pos += blockSize;

                    xnbStream.Position = pos;
                }

                if (decompressedStream.Position != decompressedSize)
                {
                    throw new Exception("XNBDecompressor failed!");
                }
            }

            decompressedStream.Position = 0;
            return decompressedStream;
        }

        public static Stream Compress(Stream xnbStream)
        {
            throw new NotImplementedException();
        }
    }
}