﻿// <copyright file="ResourceParser.cs" company="Windower Team">
// Copyright © 2013-2014 Windower Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
// </copyright>

namespace ResourceExtractor
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class ResourceParser
    {
        private static dynamic model;

        public static void Initialize(dynamic model)
        {
            ResourceParser.model = model;
        }

        private enum StringIndex
        {
            Name = 0,
            EnglishArticle = 1,
            EnglishLogSingular = 2,
            EnglishLogPlural = 3,
            EnglishDescription = 4,
            JapaneseDescription = 1,
        }

        private enum BlockType
        {
            ContainerEnd = 0x00,
            ContainerBegin = 0x01,
            SpellData = 0x49,
            AbilityData = 0x53,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            private int id;
            private int size;
            private long padding;

            public int ID
            {
                get { return id; }
            }

            public int Size
            {
                get { return (int) (((uint) size >> 3) & ~0xF) - 16; }
            }

            public BlockType Type
            {
                get { return (BlockType) (size & 0x7F); }
            }
        }

        public static void ParseMainStream(Stream stream)
        {
            Header header = stream.Read<Header>();
            long block = stream.Position;

            if (header.Type != BlockType.ContainerBegin)
            {
                throw new InvalidDataException();
            }

            stream.Position = block + header.Size;

            while (header.Type != BlockType.ContainerEnd)
            {
                header = stream.Read<Header>();
                block = stream.Position;

                LoadStreamItem(stream, header);
                stream.Position = block + header.Size;
            }
        }

        private static void LoadStreamItem(Stream stream, Header header)
        {
            switch (header.Type)
            {
                case BlockType.ContainerEnd:
                    break;
                case BlockType.ContainerBegin:
                    stream.Position -= Marshal.SizeOf(typeof(Header));
                    ParseMainStream(stream);
                    break;
                case BlockType.SpellData:
                    ResourceParser.ParseSpells(stream, header.Size);
                    break;
                case BlockType.AbilityData:
                    ResourceParser.ParseActions(stream, header.Size);
                    break;
                default:
                    Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Unknown [{0:X2}]", (int)header.Type));
                    break;
            }
        }

        private static ModelObject ParseAction(BinaryReader reader)
        {
            dynamic action = new ModelObject();

            action.id = reader.ReadInt16();
            
            action.type = (AbilityType)reader.ReadByte();
            action.element = reader.ReadByte() & 0x07;
            action.icon_id = reader.ReadInt16();
            action.mp_cost = reader.ReadInt16();
            action.recast_id = reader.ReadInt16();
            action.targets = reader.ReadInt16();
            action.tp_cost = reader.ReadInt16();
            reader.ReadBytes(0x01);     // Unknown 0E - 0E, 0x12 for Ready, 0x16 for Ward, 0x17 for Effusion, 0x00 for every other valid ability
            action.monster_level = reader.ReadSByte();
            action.range = reader.ReadSByte() % 0xF;

            // Derived data
            action.prefix = ((AbilityType)action.type).Prefix();

            if (action.tp_cost == -1)
            {
                action.tp_cost = 0;
            }

            return action;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static void ParseActions(Stream stream, int length)
        {
            IDictionary<short, object> recasts = new Dictionary<short, object>();

            var data = new byte[0x30];
            for (var i = 0; i < length / data.Length; ++i)
            {
                stream.Read(data, 0, data.Length);
                byte b2 = data[2];
                byte b11 = data[11];
                byte b12 = data[12];

                data.Decode();

                data[2] = b2;
                data[11] = b11;
                data[12] = b12;

                using (MemoryStream mstream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(mstream, Encoding.ASCII, true))
                {
                    dynamic action = ParseAction(reader);
                    if (action == null)
                    {
                        continue;
                    }

                    model.actions.Add(action);

                    // Job ability
                    if (action.id >= 0x0200 && action.id < 0x0600)
                    {
                        // Add to recast dictionary
                        dynamic recast = new ModelObject();
                        recast.id = action.recast_id;
                        recast.action_id = action.id - 0x200;
                        recasts[recast.id] = recast;
                    }
                }
            }

            // Remove default value
            recasts.Remove(0);
            model.ability_recasts.AddRange(recasts.Select(kvp => kvp.Value));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static void ParseSpells(Stream stream, int length)
        {
            var data = new byte[0x64];
            for (var i = 0; i < length / data.Length; ++i)
            {
                stream.Read(data, 0, data.Length);
                byte b2 = data[2];
                byte b11 = data[11];
                byte b12 = data[12];

                data.Decode();

                data[2] = b2;
                data[11] = b11;
                data[12] = b12;

                bool valid = true;

                // Check if spell is usable by any job.
                for (int j = 0; j < 0x30; ++j)
                {
                    valid |= data[14 + j] != 0xFF;
                }

                // Invalid spell
                if (!valid)
                {
                    continue;
                }

                using (MemoryStream mstream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(mstream, Encoding.ASCII, true))
                {
                    dynamic spell = ParseSpell(reader);
                    if (spell == null)
                    {
                        continue;
                    }

                    model.spells.Add(spell);
                }
            }
            model.spells.Remove(0);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static ModelObject ParseSpell(BinaryReader reader)
        {
            dynamic spell = new ModelObject();

            spell.id = reader.ReadInt16();
            if (spell.id == 0)
            {
                return null;
            }

            spell.type = (MagicType)reader.ReadInt16();
            spell.element = reader.ReadByte();
            reader.ReadBytes(0x01);                 // Unknown 05 - 05, possibly just padding or element being a short
            spell.targets = reader.ReadUInt16();
            spell.skill = reader.ReadInt16();
            spell.mp_cost = reader.ReadInt16();
            spell.cast_time = reader.ReadByte() / 4.0;
            spell.recast = reader.ReadByte() / 4.0;
            spell.levels = new Dictionary<int, int>();
            for (var job = 0; job < 0x18; ++job)
            {
                var level = reader.ReadInt16();
                if (level != -1)
                {
                    spell.levels[job] = level;
                }
            }

            // Currently the last job slot just mirrors the White Mage's level
            // This may need to be removed at some point, if they expand on jobs more
            spell.levels.Remove(0x17);

            reader.ReadBytes(0x02);

            // SE changed spell recast times in memory to be indexed by spell ID, not recast ID
            //spell.recast_id = reader.ReadInt16();
            spell.recast_id = spell.id;
            spell.icon_id_nq = reader.ReadInt16();
            spell.icon_id = reader.ReadInt16();
            spell.requirements = reader.ReadByte();
            spell.range = reader.ReadSByte() % 0xF;

            // Derived data
            spell.prefix = ((MagicType)spell.type).Prefix();

            return spell;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static void ParseItems(Stream stream, Stream streamja)
        {
            byte[] data = new byte[0x200];
            byte[] dataja = new byte[0x200];
            int count = (int)(stream.Length / 0xC00);
            for (int i = 0; i < count; i++)
            {
                stream.Position = streamja.Position = i * 0xC00;

                stream.Read(data, 0, data.Length);
                streamja.Read(dataja, 0, dataja.Length);

                data.RotateRight(5);
                dataja.RotateRight(5);

                using (Stream stringstream = new MemoryStream(data))
                using (Stream stringstreamja = new MemoryStream(dataja))
                using (BinaryReader reader = new BinaryReader(stringstream, Encoding.ASCII, true))
                using (BinaryReader readerja = new BinaryReader(stringstreamja, Encoding.ASCII, true))
                {
                    // The english and japanese stream contain the same main item data, so only one stream is needed here
                    // The only difference is in the string handling, hence readerja is still needed later
                    dynamic item = ParseItem(reader);
                    if (item == null)
                    {
                        continue;
                    }

                    // Advance the japanese stream's position to the same as the english one for string parsing
                    stringstreamja.Position = stringstream.Position;

                    // Parse strings
                    if (item.id >= 0xF000 && item.id < 0xF200)
                    {
                        item.id -= 0xF000;

                        ParseBasicStrings(reader, item, Languages.English);
                        ParseBasicStrings(readerja, item, Languages.Japanese);

                        model.monstrosity.Add(item);
                    }
                    else
                    {
                        ParseFullStrings(reader, item, Languages.English);
                        ParseFullStrings(readerja, item, Languages.Japanese);

                        model.items.Add(item);
                    }
                }
            }
        }

        private static ModelObject ParseItem(BinaryReader reader)
        {
            dynamic item = new ModelObject();

            item.id = reader.ReadUInt16();
            if (item.id == 0x0000)
            {
                return null;
            }

            reader.ReadBytes(0x02);         // Unknown 02 - 03 (possibly for future expansion of ID)

            if (item.id >= 0xF000 && item.id < 0xF200)
            {
                ParseMonstrosityItem(reader, item);
            }
            else if (item.id == 0xFFFF)
            {
                ParseGilItem(reader, item);
            }
            else
            {
                ParseRealItem(reader, item);
            }

            return item;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static void ParseRealItem(BinaryReader reader, dynamic item)
        {
            item.flags = reader.ReadUInt16();
            item.stack = reader.ReadUInt16();
            item.type = reader.ReadUInt16();
            reader.ReadBytes(0x02);     // AH sorting value
            item.targets = reader.ReadUInt16();

            if (item.id <= 0x0FFF)
            {
                ParseGeneralItem(reader, item);
            }
            else if (item.id < 0x2000)
            {
                ParseUsableItem(reader, item);
            }
            else if (item.id < 0x2200)
            {
                ParseAutomatonItem(reader, item);
            }
            else if (item.id < 0x2800)
            {
                ParseGeneralItem(reader, item);
            }
            else if (item.id < 0x4000)
            {
                ParseArmorItem(reader, item);
            }
            else if (item.id < 0x6400)
            {
                ParseWeaponItem(reader, item);
            }
            else if (item.id < 0x7000)
            {
                ParseArmorItem(reader, item);
            }
            else if (item.id < 0x7400)
            {
                ParseMazeItem(reader, item);
            }
        }

        private static void ParseGilItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0C);             // Unknown 04 - 0F

            item.category = "Gil";
        }

        private static void ParseGeneralItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 0E - 17
            
            item.category = "General";
        }

        private static void ParseWeaponItem(BinaryReader reader, dynamic item)
        {
            item.level = reader.ReadUInt16();
            item.slots = reader.ReadUInt16();
            item.races = reader.ReadUInt16();
            item.jobs = reader.ReadUInt32();

            reader.ReadBytes(0x04);             // Unknown 18 - 1B
            item.damage = reader.ReadUInt16();
            item.delay = reader.ReadInt16();
            reader.ReadBytes(0x02);             // Weapon DPS
            item.skill = reader.ReadByte();
            
            reader.ReadBytes(0x05);             // Unknown 23 - 27
                                                // POLUtils claims that 0x23 is jug size, but seems incorrect
            var max_charges = reader.ReadByte();
            if (max_charges > 0)
            {
                item.max_charges = max_charges;
            }
            var cast_time = reader.ReadByte() / 4.0;
            if (cast_time > 0)
            {
                item.cast_time = cast_time;
            }
            var cast_delay = reader.ReadUInt16();
            if (cast_delay > 0)
            {
                item.cast_delay = cast_delay;
            }
            var recast_delay = reader.ReadUInt32();
            if (recast_delay > 0)
            {
                item.recast_delay = recast_delay;
            }
            reader.ReadBytes(0x02);             // Unknown 30 - 31
            var item_level = reader.ReadUInt16();
            if (item_level > 0)
            {
                item.item_level = item_level;
            }

            reader.ReadBytes(0x04);             // Unknown 34 - 37

            item.category = "Weapon";
        }

        private static void ParseArmorItem(BinaryReader reader, dynamic item)
        {
            item.level = reader.ReadUInt16();
            item.slots = reader.ReadUInt16();
            item.races = reader.ReadUInt16();
            item.jobs = reader.ReadUInt32();
            var superior_level = reader.ReadByte();
            if (superior_level > 0)
            {
                item.superior_level = superior_level;
            }
            reader.ReadByte();                  // Unknown 17

            var shield_size = reader.ReadUInt16();
            if (shield_size > 0)
            {
                item.shield_size = shield_size;
            }
            var max_charges = reader.ReadByte();
            if (max_charges > 0)
            {
                item.max_charges = max_charges;
            }

            var cast_time = reader.ReadByte() / 4.0;
            if (cast_time > 0)
            {
                item.cast_time = cast_time;
            }
            var cast_delay = reader.ReadUInt16();
            if (cast_delay > 0)
            {
                item.cast_delay = cast_delay;
            }
            var recast_delay = reader.ReadUInt32();
            if (recast_delay > 0)
            {
                item.recast_delay = recast_delay;
            }
            reader.ReadBytes(0x02);             // Unknown 24 - 25
            var item_level = reader.ReadUInt16();
            if (item_level > 0)
            {
                item.item_level = item_level;
            }

            reader.ReadBytes(0x04);             // Unknown 28 - 2B

            item.category = "Armor";
        }

        private static void ParseUsableItem(BinaryReader reader, dynamic item)
        {
            item.cast_time = reader.ReadUInt16() / 4.0;
            reader.ReadBytes(0x08);             // Unknown 10 - 17
            reader.ReadInt32();                 // Not entirely known 18-1B, https://gist.github.com/z16/b1678ffefc4a6e9d52d6

            item.category = "Usable";
        }

        private static void ParseAutomatonItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 0E - 17

            item.category = "Automaton";
        }

        private static void ParseMazeItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x46);             // Unknown 0E - 53

            item.category = "Maze";
        }

        private static void ParseMonstrosityItem(BinaryReader reader, dynamic item)
        {
            item.tp_moves = new Dictionary<ushort, sbyte>();
            reader.ReadBytes(0x2C);             // Unknown 04 - 2F
            for (var i = 0x00; i < 0x10; ++i)
            {
                var move = reader.ReadUInt16();
                var level = reader.ReadSByte();
                if (level != 0 && level != -1 && !item.tp_moves.ContainsKey(move))
                {
                    item.tp_moves.Add(move, level);
                }

                reader.ReadByte();              // Unknown byte, possibly padding, or level being a short
            }
        }

        private static void ParseBasicStrings(BinaryReader reader, dynamic item, Languages language)
        {
            // This can potentially be used to disambiguate between languages as well, as their string counts are unique
            reader.ReadUInt32(); // String count

            switch (language)
            {
            case Languages.English:
                item.en = DecodeEntry(reader, StringIndex.Name);
                break;

            case Languages.Japanese:
                item.ja = DecodeEntry(reader, StringIndex.Name);
                break;
            }
        }

        private static void ParseFullStrings(BinaryReader reader, dynamic item, Languages language)
        {
            ParseBasicStrings(reader, item, language);

            switch (language)
            {
            case Languages.English:
                item.enl = DecodeEntry(reader, StringIndex.EnglishLogSingular);
                item.endesc = DecodeEntry(reader, StringIndex.EnglishDescription);
                break;

            case Languages.Japanese:
                item.jal = DecodeEntry(reader, StringIndex.Name);
                item.jadesc = DecodeEntry(reader, StringIndex.JapaneseDescription);
                break;
            }
        }

        private static object DecodeEntry(BinaryReader reader, StringIndex index)
        {
            Stream stream = reader.BaseStream;
            long origin = stream.Position;
            reader.ReadBytes(8 * (int)index);
            int dataoffset = reader.ReadInt32();
            int datatype = reader.ReadInt32();
            stream.Position = origin;

            reader.ReadBytes(dataoffset);

            switch (datatype)
            {
                case 0:
                    reader.ReadBytes(0x18);
                    long dataorigin = stream.Position;
                    int length;

                    while (stream.Position != stream.Length && reader.ReadByte() != 0)
                    {
                    }

                    length = (int)(stream.Position - dataorigin) - 1;
                    stream.Position = dataorigin;

                    var res = ShiftJISFF11Encoding.ShiftJISFF11.GetString(reader.ReadBytes(length), 0, length);
                    stream.Position = origin;
                    return res;

                case 1:
                    return reader.ReadInt32();
            }

            return null;
        }
    }
}
