using System;
using System;
using System.IO;
using UnityEngine;

public class SLDReader
{
    // --- Data Structures as defined before ---
    public struct SLDHeader
    {
        public string fileDescriptor; // "SLDX"
        public ushort version;
        public ushort numFrames;
        public ushort unknown1;
        public ushort unknown2;
        public uint unknown3;
    }

    public struct SLDFrameHeader
    {
        public ushort canvasWidth;
        public ushort canvasHeight;
        public short canvasHotspotX;
        public short canvasHotspotY;
        public byte frameType;  // Bit 0: main graphics, 1: shadow, 2: ???, 3: damage mask, 4: playercolor mask
        public byte unknown1;
        public ushort frameIndex;
    }

    public struct SLDGraphicsHeader
    {
        public ushort offsetX1;
        public ushort offsetY1;
        public ushort offsetX2;
        public ushort offsetY2;
        public byte flag1;
        public byte unknown1;
    }

    public SLDHeader header;
    public SLDFrameHeader frameHeader;
    // We store one texture per frame
    public Texture2D[] frameTextures;

    public SLDReader(string filePath)
    {
        using (FileStream fs = File.OpenRead(filePath))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // --- Read SLD file header (16 bytes)
            byte[] sigBytes = reader.ReadBytes(4);
            header.fileDescriptor = System.Text.Encoding.ASCII.GetString(sigBytes);
            header.version = reader.ReadUInt16();
            header.numFrames = reader.ReadUInt16();
            header.unknown1 = reader.ReadUInt16();
            header.unknown2 = reader.ReadUInt16();
            header.unknown3 = reader.ReadUInt32();
            Debug.Log($"SLD Header: {header.fileDescriptor} version: {header.version} frames: {header.numFrames}");

            frameTextures = new Texture2D[header.numFrames];

            // Loop through all frames.
            for (int f = 0; f < header.numFrames; f++)
            {
                SLDFrameHeader frameHdr = new SLDFrameHeader
                {
                    canvasWidth = reader.ReadUInt16(),
                    canvasHeight = reader.ReadUInt16(),
                    canvasHotspotX = reader.ReadInt16(),
                    canvasHotspotY = reader.ReadInt16(),
                    frameType = reader.ReadByte(),
                    unknown1 = reader.ReadByte(),
                    frameIndex = reader.ReadUInt16()
                };
                Debug.Log($"Frame {frameHdr.frameIndex}: {frameHdr.canvasWidth}x{frameHdr.canvasHeight}");

                // For simplicity, we assume main graphics layer is always present.
                if ((frameHdr.frameType & 0x01) == 0)
                {
                    Debug.LogWarning($"Frame {frameHdr.frameIndex} missing main graphics layer, skipping.");
                    continue;
                }

                // Read layer content length (4 bytes) and compute padded length.
                uint layerContentLength = reader.ReadUInt32();
                uint paddedLayerLength = layerContentLength + ((4 - (layerContentLength % 4)) % 4);

                // Read main graphics layer header (10 bytes)
                SLDGraphicsHeader gfxHdr = new SLDGraphicsHeader
                {
                    offsetX1 = reader.ReadUInt16(),
                    offsetY1 = reader.ReadUInt16(),
                    offsetX2 = reader.ReadUInt16(),
                    offsetY2 = reader.ReadUInt16(),
                    flag1 = reader.ReadByte(),
                    unknown1 = reader.ReadByte()
                };

                int layerWidth = gfxHdr.offsetX2 - gfxHdr.offsetX1;
                int layerHeight = gfxHdr.offsetY2 - gfxHdr.offsetY1;
                Debug.Log($"Main Graphics Layer: {layerWidth}x{layerHeight}");

                // Read command array length (2 bytes)
                ushort commandCount = reader.ReadUInt16();
                (byte skip, byte draw)[] commands = new (byte, byte)[commandCount];
                for (int i = 0; i < commandCount; i++)
                {
                    byte skip = reader.ReadByte();
                    byte draw = reader.ReadByte();
                    commands[i] = (skip, draw);
                }

                // Read the compressed block array.
                int totalBlocks = 0;
                foreach (var cmd in commands)
                    totalBlocks += cmd.draw;
                byte[][] blocks = new byte[totalBlocks][];
                for (int i = 0; i < totalBlocks; i++)
                {
                    blocks[i] = reader.ReadBytes(8);
                }

                // Skip any padding for this layer.
                long bytesReadForLayer = 4 + 10 + 2 + (commandCount * 2) + (totalBlocks * 8);
                long paddingBytes = paddedLayerLength - bytesReadForLayer;
                if (paddingBytes > 0)
                    reader.ReadBytes((int)paddingBytes);

                // Reconstruct the image.
                Color[] pixels = new Color[layerWidth * layerHeight];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(0, 0, 0, 0);

                int currentBlockIndex = 0;
                int currentBlockPosition = 0;
                int blocksPerRow = layerWidth / 4;
                foreach (var cmd in commands)
                {
                    currentBlockPosition += cmd.skip;
                    for (int i = 0; i < cmd.draw; i++)
                    {
                        if (currentBlockIndex >= totalBlocks)
                            break;
                        int blockRow = currentBlockPosition / blocksPerRow;
                        int blockCol = currentBlockPosition % blocksPerRow;
                        Color[] blockPixels = DecompressDXT1Block(blocks[currentBlockIndex]);
                        for (int by = 0; by < 4; by++)
                        {
                            for (int bx = 0; bx < 4; bx++)
                            {
                                int x = blockCol * 4 + bx;
                                int y = blockRow * 4 + by;
                                if (x < layerWidth && y < layerHeight)
                                {
                                    int idx = y * layerWidth + x;
                                    pixels[idx] = blockPixels[by * 4 + bx];
                                }
                            }
                        }
                        currentBlockIndex++;
                        currentBlockPosition++;
                    }
                }

                // Create a texture for this frame.
                Texture2D frameTexture = new Texture2D(layerWidth, layerHeight, TextureFormat.RGBA32, false);
                frameTexture.SetPixels(pixels);
                frameTexture.Apply();
                frameTextures[f] = frameTexture;
            }
        }
    }

    private Color[] DecompressDXT1Block(byte[] blockData)
    {
        Color[] colors = new Color[16];
        ushort color0 = BitConverter.ToUInt16(blockData, 0);
        ushort color1 = BitConverter.ToUInt16(blockData, 2);
        uint indices = BitConverter.ToUInt32(blockData, 4);
        Color[] lookup = new Color[4];
        lookup[0] = Convert565ToColor(color0);
        lookup[1] = Convert565ToColor(color1);
        if (color0 > color1)
        {
            lookup[2] = Color.Lerp(lookup[0], lookup[1], 1f / 3f);
            lookup[3] = Color.Lerp(lookup[0], lookup[1], 2f / 3f);
        }
        else
        {
            lookup[2] = Color.Lerp(lookup[0], lookup[1], 0.5f);
            lookup[3] = new Color(0, 0, 0, 0);
        }
        for (int i = 0; i < 16; i++)
        {
            int index = (int)(indices & 0x03);
            colors[i] = lookup[index];
            indices >>= 2;
        }
        return colors;
    }

    private Color Convert565ToColor(ushort color)
    {
        int r = (color >> 11) & 0x1F;
        int g = (color >> 5) & 0x3F;
        int b = color & 0x1F;
        return new Color(r / 31f, g / 63f, b / 31f, 1f);
    }
}


//using System.IO;
//using UnityEngine;
//
//public class SLDReader
//{
//    // --- Data Structures ---
//    // SLD file header: 4s, 4H, I = 4 + 2+2+2+2+4 = 16 bytes
//    public struct SLDHeader
//    {
//        public string fileDescriptor; // Should be "SLDX"
//        public ushort version;        // Expected 4 (0x0004)
//        public ushort numFrames;
//        public ushort unknown1;       // Always 0x0000
//        public ushort unknown2;       // Always 0x0010
//        public uint unknown3;         // Always 0xFF000000
//    }
//
//    // Frame header: 4H, 2B, H = 2+2+2+2+1+1+2 = 12 bytes
//    public struct SLDFrameHeader
//    {
//        public ushort canvasWidth;
//        public ushort canvasHeight;
//        public short canvasHotspotX;
//        public short canvasHotspotY;
//        public byte frameType;  // Bitfield: bit0: main graphics, bit1: shadow, bit2: ???, bit3: damage mask, bit4: playercolor mask
//        public byte unknown1;
//        public ushort frameIndex;
//    }
//
//    // Main Graphics Layer Header: 4H, 2B = 2+2+2+2+1+1 = 10 bytes
//    public struct SLDGraphicsHeader
//    {
//        public ushort offsetX1;
//        public ushort offsetY1;
//        public ushort offsetX2;
//        public ushort offsetY2;
//        public byte flag1;
//        public byte unknown1;
//    }
//
//    public SLDHeader header;
//    public SLDFrameHeader frameHeader;
//    public SLDGraphicsHeader graphicsHeader;
//    public Texture2D texture;
//
//    public SLDReader(string filePath)
//    {
//        using (FileStream fs = File.OpenRead(filePath))
//        using (BinaryReader reader = new BinaryReader(fs))
//        {
//            // === Read SLD File Header ===
//            byte[] sigBytes = reader.ReadBytes(4);
//            header.fileDescriptor = System.Text.Encoding.ASCII.GetString(sigBytes);
//            header.version = reader.ReadUInt16();
//            header.numFrames = reader.ReadUInt16();
//            header.unknown1 = reader.ReadUInt16();
//            header.unknown2 = reader.ReadUInt16();
//            header.unknown3 = reader.ReadUInt32();
//
//            Debug.Log($"SLD Header: {header.fileDescriptor} version: {header.version} frames: {header.numFrames}");
//
//            // === Read First Frame Header ===
//            frameHeader.canvasWidth = reader.ReadUInt16();
//            frameHeader.canvasHeight = reader.ReadUInt16();
//            frameHeader.canvasHotspotX = reader.ReadInt16();
//            frameHeader.canvasHotspotY = reader.ReadInt16();
//            frameHeader.frameType = reader.ReadByte();
//            frameHeader.unknown1 = reader.ReadByte();
//            frameHeader.frameIndex = reader.ReadUInt16();
//
//            Debug.Log($"Frame {frameHeader.frameIndex}: {frameHeader.canvasWidth}x{frameHeader.canvasHeight}");
//
//            // --- Process layers in fixed order ---
//            // The specification defines up to 5 layers in order:
//            // 0: Main Graphics, 1: Shadow, 2: ???, 3: Damage Mask, 4: Playercolor Mask.
//            // Bits are stored in reverse order (LSB = main graphics).
//            // For this example, we only process the Main Graphics layer (bit0).
//            if ((frameHeader.frameType & 0x01) == 0)
//            {
//                throw new Exception("Frame does not contain a main graphics layer.");
//            }
//
//            // === Read the main graphics layer ===
//            // First, each layer starts with a 4-byte content length.
//            uint layerContentLength = reader.ReadUInt32();
//            // Calculate padded length: padded to multiple of 4.
//            uint paddedLayerLength = layerContentLength + ((4 - (layerContentLength % 4)) % 4);
//
//            // Now read the main graphics layer header (10 bytes)
//            graphicsHeader.offsetX1 = reader.ReadUInt16();
//            graphicsHeader.offsetY1 = reader.ReadUInt16();
//            graphicsHeader.offsetX2 = reader.ReadUInt16();
//            graphicsHeader.offsetY2 = reader.ReadUInt16();
//            graphicsHeader.flag1 = reader.ReadByte();
//            graphicsHeader.unknown1 = reader.ReadByte();
//
//            int layerWidth = graphicsHeader.offsetX2 - graphicsHeader.offsetX1;
//            int layerHeight = graphicsHeader.offsetY2 - graphicsHeader.offsetY1;
//            Debug.Log($"Main Graphics Layer: {layerWidth}x{layerHeight}");
//
//            // === Read Command Array ===
//            // The next 2 bytes give the command array length (number of commands)
//            ushort commandCount = reader.ReadUInt16();
//            Debug.Log($"Command Array Length: {commandCount}");
//
//            (byte skip, byte draw)[] commands = new (byte, byte)[commandCount];
//            for (int i = 0; i < commandCount; i++)
//            {
//                byte skip = reader.ReadByte();
//                byte draw = reader.ReadByte();
//                commands[i] = (skip, draw);
//            }
//
//            // === Read Compressed Block Array ===
//            int totalBlocks = 0;
//            foreach (var cmd in commands)
//            {
//                totalBlocks += cmd.draw;
//            }
//            byte[][] blocks = new byte[totalBlocks][];
//            for (int i = 0; i < totalBlocks; i++)
//            {
//                blocks[i] = reader.ReadBytes(8); // DXT1 block: 8 bytes each
//            }
//
//            // --- Skip any padding left in this layer
//            long bytesReadForLayer = 4 /*content length*/ + 10 /*layer header*/ + 2 /*command count*/ + (commandCount * 2) + (totalBlocks * 8);
//            long paddingBytes = paddedLayerLength - bytesReadForLayer;
//            if (paddingBytes > 0)
//                reader.ReadBytes((int)paddingBytes);
//
//            // === Reconstruct the layer image ===
//            int blocksPerRow = layerWidth / 4;
//            int blocksPerColumn = layerHeight / 4;
//            Color[] pixels = new Color[layerWidth * layerHeight];
//            for (int i = 0; i < pixels.Length; i++)
//            {
//                pixels[i] = new Color(0, 0, 0, 0);
//            }
//            int currentBlockIndex = 0;
//            int currentBlockPosition = 0;
//            foreach (var cmd in commands)
//            {
//                currentBlockPosition += cmd.skip;
//                for (int i = 0; i < cmd.draw; i++)
//                {
//                    if (currentBlockIndex >= totalBlocks)
//                        break;
//                    int blockRow = currentBlockPosition / blocksPerRow;
//                    int blockCol = currentBlockPosition % blocksPerRow;
//                    Color[] blockPixels = DecompressDXT1Block(blocks[currentBlockIndex]);
//                    for (int by = 0; by < 4; by++)
//                    {
//                        for (int bx = 0; bx < 4; bx++)
//                        {
//                            int x = blockCol * 4 + bx;
//                            int y = blockRow * 4 + by;
//                            if (x < layerWidth && y < layerHeight)
//                            {
//                                int pixelIndex = y * layerWidth + x;
//                                pixels[pixelIndex] = blockPixels[by * 4 + bx];
//                            }
//                        }
//                    }
//                    currentBlockIndex++;
//                    currentBlockPosition++;
//                }
//            }
//
//            // Create texture from decompressed pixel data.
//            texture = new Texture2D(layerWidth, layerHeight, TextureFormat.RGBA32, false);
//            texture.SetPixels(pixels);
//            texture.Apply();
//        }
//    }
//
//    // Decompress a single 8-byte DXT1 block into 4x4 pixels.
//    private Color[] DecompressDXT1Block(byte[] blockData)
//    {
//        Color[] colors = new Color[16];
//        ushort color0 = BitConverter.ToUInt16(blockData, 0);
//        ushort color1 = BitConverter.ToUInt16(blockData, 2);
//        uint pixelIndices = BitConverter.ToUInt32(blockData, 4);
//
//        Color[] lookup = new Color[4];
//        lookup[0] = Convert565ToColor(color0);
//        lookup[1] = Convert565ToColor(color1);
//
//        if (color0 > color1)
//        {
//            // 4-color block: two interpolated colors
//            lookup[2] = Color.Lerp(lookup[0], lookup[1], 1f / 3f);
//            lookup[3] = Color.Lerp(lookup[0], lookup[1], 2f / 3f);
//        }
//        else
//        {
//            // 3-color block: one interpolated color, and transparent color
//            lookup[2] = Color.Lerp(lookup[0], lookup[1], 0.5f);
//            lookup[3] = new Color(0, 0, 0, 0);
//        }
//
//        for (int i = 0; i < 16; i++)
//        {
//            int index = (int)(pixelIndices & 0x03);
//            colors[i] = lookup[index];
//            pixelIndices >>= 2;
//        }
//        return colors;
//    }
//
//    // Convert a 16-bit 565 color to a normalized Unity Color.
//    private Color Convert565ToColor(ushort color)
//    {
//        int r = (color >> 11) & 0x1F;
//        int g = (color >> 5) & 0x3F;
//        int b = color & 0x1F;
//        return new Color(r / 31f, g / 63f, b / 31f, 1f);
//    }
//}
