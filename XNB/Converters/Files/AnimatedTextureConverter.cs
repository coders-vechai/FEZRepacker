﻿using FEZRepacker.Definitions.FezEngine.Content;
using FEZRepacker.XNB.Types;
using FEZRepacker.XNB.Types.System;
using FEZRepacker.XNB.Types.XNA;
using FEZRepacker.Definitions.MicrosoftXna;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FEZRepacker.XNB.Converters.Files
{
    class AnimatedTextureConverter : XNBContentConverter
    {
        public override XNBContentType[] TypesFactory => new XNBContentType[]
        {
            new GenericContentType<AnimatedTexture>(this),
            new ByteArrayContentType(this),
            new ListContentType<FrameContent>(this),
            new GenericContentType<FrameContent>(this),
            new TimeSpanContentType(this),
            new RectangleContentType(this)
        };
        public override string FileFormat => "gif";

        public override void FromBinary(BinaryReader xnbReader, BinaryWriter outWriter)
        {
            AnimatedTexture txt = (AnimatedTexture)Types[0].Read(xnbReader);

            Texture2D atlas = new Texture2D();
            atlas.Format = SurfaceFormat.Color;
            atlas.MipmapLevels = 1;
            atlas.Width = txt.AtlasWidth;
            atlas.Height = txt.AtlasHeight;
            atlas.TextureData = txt.TextureData;

            using var image = TextureConverter.ImageFromTexture2D(atlas);

            using Image<Rgba32> animation = new(txt.FrameWidth, txt.FrameHeight, Color.Transparent);
            animation.Metadata.GetGifMetadata().ColorTableMode = GifColorTableMode.Local;
            animation.Metadata.GetGifMetadata().RepeatCount = 0;

            for (int i = 0; i < txt.Frames.Count; i++)
            {
                var frame = txt.Frames[i];
                var rect = frame.Rectangle;

                using var frameImg = image.Clone(i =>
                    i.Crop(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height))
                );

                var metadata = frameImg.Frames.RootFrame.Metadata.GetGifMetadata();
                metadata.FrameDelay = frame.Duration.Milliseconds / 10;
                metadata.DisposalMethod = GifDisposalMethod.RestoreToPrevious;

                animation.Frames.AddFrame(frameImg.Frames.RootFrame);
            }

            animation.Frames.RemoveFrame(0);

            animation.Save(outWriter.BaseStream, new GifEncoder { ColorTableMode = GifColorTableMode.Local});
        }

        public override void ToBinary(BinaryReader inReader, BinaryWriter xnbWriter)
        {
            AnimatedTexture animatedTexture = new AnimatedTexture();

            using var importedAnim = Image.Load<Rgba32>(inReader.BaseStream);

            //// calculate texture atlas size (least size when using powers of two)
            //// why powers of two you may ask? idk, original textures seem to do that,
            //// gpu like powers of two, so why not

            int frameWidth = importedAnim.Width;
            int frameHeight = importedAnim.Height;
            int frameCount = importedAnim.Frames.Count;

            int atlasWidth = 0;
            int atlasHeight = 0;
            int atlasArea = Int32.MaxValue;

            for (int i = 1; i <= frameCount; i++)
            {
                //calculating minimum size
                int newAtlasWidth = (frameWidth + 1) * i;
                int newAtlasHeight = (frameHeight + 1) * (int)(Math.Ceiling(frameCount / (float)i));

                // rounding the size up to nearest power of two
                newAtlasWidth = (int)Math.Pow(2, Math.Ceiling(Math.Log2(newAtlasWidth)));
                newAtlasHeight = (int)Math.Pow(2, Math.Ceiling(Math.Log2(newAtlasHeight)));

                if (newAtlasWidth > newAtlasHeight && atlasWidth > 0 && atlasHeight > 0) break;

                int newArea = newAtlasWidth * newAtlasHeight;
                if (newArea <= atlasArea)
                {
                    atlasArea = newArea;
                    atlasWidth = newAtlasWidth;
                    atlasHeight = newAtlasHeight;
                }
            }

            // constructing the animated texture along with atlas
            using Image<Rgba32> atlasImage = new(atlasWidth, atlasHeight, Color.Transparent);

            int framePosX = 0;
            int framePosY = 0;
            for(int i=0; i < frameCount; i++)
            {
                var frameImg = importedAnim.Frames[i];

                frameImg.ProcessPixelRows(atlasImage.Frames.RootFrame, (animAccessor, atlasAccessor) =>
                {
                    for(int y = 0; y < frameHeight; y++)
                    {
                        animAccessor.GetRowSpan(y).CopyTo(atlasAccessor.GetRowSpan(framePosY + y).Slice(framePosX));
                    }
                });

                FrameContent frame = new FrameContent();
                frame.Duration = TimeSpan.FromMilliseconds(frameImg.Metadata.GetGifMetadata().FrameDelay * 10);
                frame.Rectangle = new System.Drawing.Rectangle(framePosX, framePosY, frameWidth, frameHeight);
                animatedTexture.Frames.Add(frame);

                framePosX += frameWidth + 1; // pixel padding between sprites
                if (framePosX > atlasWidth - frameWidth)
                {
                    framePosX = 0;
                    framePosY += frameHeight + 1;
                }
            }

            var atlas = TextureConverter.ImageToTexture2D(atlasImage);
            animatedTexture.AtlasWidth = atlas.Width;
            animatedTexture.AtlasHeight = atlas.Height;
            animatedTexture.TextureData = atlas.TextureData;

            animatedTexture.FrameWidth = frameWidth;
            animatedTexture.FrameHeight = frameHeight;

            PrimaryType.Write(animatedTexture, xnbWriter);
        }
    }
}
