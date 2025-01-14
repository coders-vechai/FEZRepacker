﻿using FEZRepacker.Converter.FileSystem;
using FEZRepacker.Converter.Helpers;
using FEZRepacker.Converter.XNB.Formats;

namespace FEZRepacker.Converter.XNB
{
    public class XnbDeconverter
    {
        public XnbFormatConverter? FormatConverter;
        public bool Converted { get; private set; }

        public XnbDeconverter()
        {
            Converted = false;
        }

        public Stream Deconvert(FileBundle fileBundle)
        {
            FormatConverter = XnbFormatList.FindByExtension(fileBundle.MainExtension);

            var output = new MemoryStream();

            if (FormatConverter == null)
            {
                if(fileBundle.Count > 0)
                {
                    fileBundle[0].Data.CopyTo(output);
                    output.Seek(0, SeekOrigin.Begin);
                }
                Converted = false;
                return output;
            }

            XnbHeader.Default.Write(output);

            using var xnbContentWriter = new BinaryWriter(new MemoryStream());

            // write list of XNB types
            xnbContentWriter.Write7BitEncodedInt(FormatConverter.PublicContentTypes.Count);
            foreach (var type in FormatConverter.PublicContentTypes)
            {
                xnbContentWriter.Write(type.Name.GetDisplayName(false)); // name
                xnbContentWriter.Write(0); // version
            }

            // number of shared resources (0 for FEZ assets)
            xnbContentWriter.Write7BitEncodedInt(0);

            // main resource id (in my system, first one is always the primary one)
            xnbContentWriter.Write7BitEncodedInt(1);

            // convert actual data
            FormatConverter.WriteXnbContent(fileBundle, xnbContentWriter);

            // copy length of the file (including header block of 10 bytes) and data into output stream
            new BinaryWriter(output).Write((int)xnbContentWriter.BaseStream.Length + 10);
            xnbContentWriter.BaseStream.Position = 0;
            xnbContentWriter.BaseStream.CopyTo(output);
            Converted = true;
            output.Position = 0;
            return output;
        }
    }
}
