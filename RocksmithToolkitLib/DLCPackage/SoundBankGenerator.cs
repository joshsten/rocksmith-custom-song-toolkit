﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RocksmithToolkitLib.Properties;
using RocksmithToolkitLib.Sng;
using MiscUtil.Conversion;
using MiscUtil.IO;

namespace RocksmithToolkitLib.DLCPackage
{
    public static class SoundBankGenerator
    {
        private const string PLAY = "Play_";
        private const string PLAY30SEC = "Play_30Sec_";
        private const string SONG = "Song_";
        private static readonly int[] bnkPCOffsets = { 0x2c, 0x1d, 0x17, 0xfa, 0xc8, 0x14, 0xc };
        private static readonly int[] bnkConsoleOffsets = { 0x7ec, 0x1d, 0x17, 0xfa, 0xc8, 0x14, 0xc };
        
        public static IList<int> GetOffsets(this GamePlatform platform) {
            switch (platform) {
                case GamePlatform.Pc:
                    return bnkPCOffsets;
                case GamePlatform.XBox360:
                    return bnkConsoleOffsets;
                case GamePlatform.PS3:
                    return bnkConsoleOffsets;
                default:
                    throw new InvalidOperationException("Unexpected game platform value");
            }
        }

        private static uint HashString(String str)
        {
            char[] bytes = str.ToLower().ToCharArray();
            uint hash = 2166136261;

            for (var i = 0; i < str.Length; i++) {
                hash *= 16777619;
                hash = hash ^ bytes[i];
            }

            return hash;
        }

        public static string GenerateSoundBank(string dlcName, Stream audioStream, Stream outStream, decimal volume, GamePlatform platform)
        {
            string eventName = PLAY + dlcName;
            string previewName = PLAY30SEC + dlcName;
            string bankName = SONG + dlcName;
            var id = RandomGenerator.NextInt();

            byte[] soundbank = null;
            switch (platform)
            {
                case GamePlatform.Pc:
                    soundbank = Resources.PC_soundbank;
                    break;
                case GamePlatform.XBox360:
                case GamePlatform.PS3:
                    soundbank = Resources.Console_soundbank;
                    break;
                default:
                    throw new InvalidOperationException("Unexpected game platform value");
            }

            var bitConverter = platform == GamePlatform.Pc
                    ? (EndianBitConverter)EndianBitConverter.Little
                    : (EndianBitConverter)EndianBitConverter.Big;

            using (var bankStream = new MemoryStream(soundbank))
            using (var bankReader = new EndianBinaryReader(bitConverter, bankStream))
            {
                var audioReader = new EndianBinaryReader(bitConverter, audioStream);
                var bankWriter = new EndianBinaryWriter(bitConverter, outStream);
                bankWriter.Write(bankReader.ReadBytes(platform.GetOffsets()[0]));
                bankReader.ReadInt32();
                bankWriter.Write(id);
                bankWriter.Write(bankReader.ReadInt32());
                int dataSize = bankReader.ReadInt32();
                bankWriter.Write(dataSize);
                bankWriter.Write(bankReader.ReadInt32());
                bankWriter.Write(bankReader.ReadInt32());
                bankWriter.Write(audioReader.ReadBytes(dataSize));
                bankReader.BaseStream.Seek(dataSize, SeekOrigin.Current);
                bankWriter.Write(bankReader.ReadBytes(platform.GetOffsets()[1]));
                bankWriter.Write(id);
                bankWriter.Write(id);
                bankReader.BaseStream.Seek(8, SeekOrigin.Current);
                bankWriter.Write(bankReader.ReadBytes(platform.GetOffsets()[2]));
                bankWriter.Write((float)volume);
                bankReader.ReadInt32();
                bankWriter.Write(bankReader.ReadBytes(platform.GetOffsets()[3]));
                bankReader.ReadInt32();
                bankWriter.Write(HashString(eventName));
                bankWriter.Write(bankReader.ReadBytes(platform.GetOffsets()[4]));
                bankReader.ReadInt32();
                bankWriter.Write(HashString(previewName));
                bankWriter.Write(bankReader.ReadBytes(platform.GetOffsets()[5]));
                bankWriter.Write(12 + bankName.Length + 1);
                bankReader.ReadInt32();
                bankWriter.Write(bankReader.ReadBytes(platform.GetOffsets()[6]));
                bankWriter.Write((byte)bankName.Length);
                bankWriter.Write(Encoding.ASCII.GetBytes(bankName));
                bankWriter.Flush();
            }
            return id.ToString();
        }
    }
}
