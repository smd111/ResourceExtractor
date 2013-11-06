﻿// <copyright file="FF11ShiftJISDecoder.cs" company="Windower Team">
// Copyright © 2013 Windower Team
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

// TODO: This class was ported from Java, and is in need of a major cleanup.

namespace ResourceExtractor
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    internal static class FF11ShiftJISDecoder
    {
        public static string Decode(byte[] data, int offset, int length)
        {
            StringBuilder result = new StringBuilder(data.Length);
            char last = '\0';
            int i = offset;
            int end = offset + length;
            while (i < end)
            {
                byte high = data[i++];

                /*
                 * Wrap any raw slotdata so that it is easily identifiable. Each byte
                 * of slotdata will be mapped so that byte XX becomes U+E0XX
                 */
                last = DecodeData(last, high);
                if (last != '\uFFFD')
                {
                    result.Append(last);
                    continue;
                }

                /*
                 * Handle Auto-Translation autotranslator. These are 6 byte sequences
                 * starting with 0xFD followed by 4 bytes of raw slotdata and ending
                 * with 0xFD. They will be decoded into 5 characters: A marker
                 * character U+E121 followed by 4 encoded slotdata characters.
                 */
                if (high == 0xFD)
                {
                    if (end - i >= 5 && (data[i + 4] & 0xFF) == 0xFD)
                    {
                        result.Append(last = '\uE120');
                        result.Append((char)(0xE000 | data[i++]));
                        result.Append((char)(0xE000 | data[i++]));
                        result.Append((char)(0xE000 | data[i++]));
                        result.Append((char)(0xE000 | data[i++]));
                        i++;
                    }
                    else
                    {
                        result.Append('\uFFFD');
                    }

                    continue;
                }

                /* Try to Decode a double-byte character. */
                if (i < end)
                {
                    last = DecodeDouble(high, data[i]);
                    if (last != '\uFFFD')
                    {
                        result.Append(last);
                        i++;
                        continue;
                    }
                }

                /* Try to Decode a single-byte character. */
                last = DecodeSingle(high);
                if (last != '\uFFFD')
                {
                    result.Append(last);
                    continue;
                }

                result.Append(last);
            }

            return result.ToString();
        }

        private static char DecodeSingle(byte data)
        {
            if (data <= 0x7E)
            {
                if (data >= 0x20 || data == 0)
                {
                    return (char)data;
                }
                else if (data == 0x07)
                {
                    return '\n';
                }

                return (char)(0xE100 | data);
            }

            if (data >= 0xA1 && data <= 0xDF)
            {
                return (char)(0xFEC0 + data);
            }

            return '\uFFFD';
        }

        private static char DecodeDouble(byte high, byte low)
        {
            switch (high)
            {
                case 0x1E: return (char)(0xF000 | (0xFF & low));
                case 0xEF: return (char)(0xF100 | (0xFF & low));
                case 0x7F: return (char)(0xE200 | (0xFF & low));
                default:
                    if (high < 0x81 || (high > 0x9F && high < 0xE0) || high > 0xFC || low < 0x40 || low == 0x7F || low > 0xFC)
                    {
                        return '\uFFFD';
                    }

                    if (high > 0x9F)
                    {
                        return Table[high - 0xC1, low - 0x40];
                    }

                    return Table[high - 0x81, low - 0x40];
            }
        }

        private static char DecodeData(char last, byte data)
        {
            switch (last)
            {
                case '\uE10A':
                case '\uE10C':
                case '\uE119':
                case '\uE11A':
                case '\uE11C':
                case '\uE11F':
                case '\uE28D':
                case '\uE28E':
                case '\uE292':
                case '\uE2B1':
                    return (char)(0xE000 | data);
            }

            return '\uFFFD';
        }

        [SuppressMessage("Microsoft.Performance", "CA1814")]
        private static readonly char[,] Table =
        {
            // === PLANE 81 ===
            {
                '\u3000', '\u3001', '\u3002', '\uFF0C', '\uFF0E', '\u30FB', '\uFF1A', '\uFF1B',
                '\uFF1F', '\uFF01', '\u309B', '\u309C', '\u00B4', '\uFF40', '\u00A8', '\uFF3E',
                '\uFFE3', '\uFF3F', '\u30FD', '\u30FE', '\u309D', '\u309E', '\u3003', '\u4EDD',
                '\u3005', '\u3006', '\u3007', '\u30FC', '\u2015', '\u2010', '\uFF0F', '\uFF3C',
                '\uFF5E', '\u2225', '\uFF5C', '\u2026', '\u2025', '\u2018', '\u2019', '\u201C',
                '\u201D', '\uFF08', '\uFF09', '\u3014', '\u3015', '\uFF3B', '\uFF3D', '\uFF5B',
                '\uFF5D', '\u3008', '\u3009', '\u300A', '\u300B', '\u300C', '\u300D', '\u300E',
                '\u300F', '\u3010', '\u3011', '\uFF0B', '\uFF0D', '\u00B1', '\u00D7', '\uFFFD',
                '\u00F7', '\uFF1D', '\u2260', '\uFF1C', '\uFF1E', '\u2266', '\u2267', '\u221E',
                '\u2234', '\u2642', '\u2640', '\u00B0', '\u2032', '\u2033', '\u2103', '\uFFE5',
                '\uFF04', '\uFFE0', '\uFFE1', '\uFF05', '\uFF03', '\uFF06', '\uFF0A', '\uFF20',
                '\u00A7', '\u2606', '\u2605', '\u25CB', '\u25CF', '\u25CE', '\u25C7', '\u25C6',
                '\u25A1', '\u25A0', '\u25B3', '\u25B2', '\u25BD', '\u25BC', '\u203B', '\u3012',
                '\u2192', '\u2190', '\u2191', '\u2193', '\u3013', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\u2208', '\u220B', '\u2286', '\u2287', '\u2282', '\u2283', '\u222A', '\u2229',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\u2227', '\u2228', '\uFFE2', '\u21D2', '\u21D4', '\u2200', '\u2203', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\u2220', '\u22A5', '\u2312', '\u2202', '\u2207', '\u2261',
                '\u2252', '\u226A', '\u226B', '\u221A', '\u223D', '\u221D', '\u2235', '\u222B',
                '\u222C', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\u212B', '\u2030', '\u266F', '\u266D', '\u266A', '\u2020', '\u2021', '\u00B6',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\u25EF', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 82 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFF10',
                '\uFF11', '\uFF12', '\uFF13', '\uFF14', '\uFF15', '\uFF16', '\uFF17', '\uFF18',
                '\uFF19', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFF21', '\uFF22', '\uFF23', '\uFF24', '\uFF25', '\uFF26', '\uFF27', '\uFF28',
                '\uFF29', '\uFF2A', '\uFF2B', '\uFF2C', '\uFF2D', '\uFF2E', '\uFF2F', '\uFF30',
                '\uFF31', '\uFF32', '\uFF33', '\uFF34', '\uFF35', '\uFF36', '\uFF37', '\uFF38',
                '\uFF39', '\uFF3A', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFF41', '\uFF42', '\uFF43', '\uFF44', '\uFF45', '\uFF46', '\uFF47',
                '\uFF48', '\uFF49', '\uFF4A', '\uFF4B', '\uFF4C', '\uFF4D', '\uFF4E', '\uFF4F',
                '\uFF50', '\uFF51', '\uFF52', '\uFF53', '\uFF54', '\uFF55', '\uFF56', '\uFF57',
                '\uFF58', '\uFF59', '\uFF5A', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\u3041',
                '\u3042', '\u3043', '\u3044', '\u3045', '\u3046', '\u3047', '\u3048', '\u3049',
                '\u304A', '\u304B', '\u304C', '\u304D', '\u304E', '\u304F', '\u3050', '\u3051',
                '\u3052', '\u3053', '\u3054', '\u3055', '\u3056', '\u3057', '\u3058', '\u3059',
                '\u305A', '\u305B', '\u305C', '\u305D', '\u305E', '\u305F', '\u3060', '\u3061',
                '\u3062', '\u3063', '\u3064', '\u3065', '\u3066', '\u3067', '\u3068', '\u3069',
                '\u306A', '\u306B', '\u306C', '\u306D', '\u306E', '\u306F', '\u3070', '\u3071',
                '\u3072', '\u3073', '\u3074', '\u3075', '\u3076', '\u3077', '\u3078', '\u3079',
                '\u307A', '\u307B', '\u307C', '\u307D', '\u307E', '\u307F', '\u3080', '\u3081',
                '\u3082', '\u3083', '\u3084', '\u3085', '\u3086', '\u3087', '\u3088', '\u3089',
                '\u308A', '\u308B', '\u308C', '\u308D', '\u308E', '\u308F', '\u3090', '\u3091',
                '\u3092', '\u3093', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 83 ===
            {
                '\u30A1', '\u30A2', '\u30A3', '\u30A4', '\u30A5', '\u30A6', '\u30A7', '\u30A8',
                '\u30A9', '\u30AA', '\u30AB', '\u30AC', '\u30AD', '\u30AE', '\u30AF', '\u30B0',
                '\u30B1', '\u30B2', '\u30B3', '\u30B4', '\u30B5', '\u30B6', '\u30B7', '\u30B8',
                '\u30B9', '\u30BA', '\u30BB', '\u30BC', '\u30BD', '\u30BE', '\u30BF', '\u30C0',
                '\u30C1', '\u30C2', '\u30C3', '\u30C4', '\u30C5', '\u30C6', '\u30C7', '\u30C8',
                '\u30C9', '\u30CA', '\u30CB', '\u30CC', '\u30CD', '\u30CE', '\u30CF', '\u30D0',
                '\u30D1', '\u30D2', '\u30D3', '\u30D4', '\u30D5', '\u30D6', '\u30D7', '\u30D8',
                '\u30D9', '\u30DA', '\u30DB', '\u30DC', '\u30DD', '\u30DE', '\u30DF', '\uFFFD',
                '\u30E0', '\u30E1', '\u30E2', '\u30E3', '\u30E4', '\u30E5', '\u30E6', '\u30E7',
                '\u30E8', '\u30E9', '\u30EA', '\u30EB', '\u30EC', '\u30ED', '\u30EE', '\u30EF',
                '\u30F0', '\u30F1', '\u30F2', '\u30F3', '\u30F4', '\u30F5', '\u30F6', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\u0391',
                '\u0392', '\u0393', '\u0394', '\u0395', '\u0396', '\u0397', '\u0398', '\u0399',
                '\u039A', '\u039B', '\u039C', '\u039D', '\u039E', '\u039F', '\u03A0', '\u03A1',
                '\u03A3', '\u03A4', '\u03A5', '\u03A6', '\u03A7', '\u03A8', '\u03A9', '\uFFFD',
                '\u03FF', '\u03FF', '\u03FF', '\u03FF', '\u03FF', '\u03FF', '\u03FF', '\u03B1',
                '\u03B2', '\u03B3', '\u03B4', '\u03B5', '\u03B6', '\u03B7', '\u03B8', '\u03B9',
                '\u03BA', '\u03BB', '\u03BC', '\u03BD', '\u03BE', '\u03BF', '\u03C0', '\u03C1',
                '\u03C3', '\u03C4', '\u03C5', '\u03C6', '\u03C7', '\u03C8', '\u03C9', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 84 ===
            {
                '\u0410', '\u0411', '\u0412', '\u0413', '\u0414', '\u0415', '\u0401', '\u0416',
                '\u0417', '\u0418', '\u0419', '\u041A', '\u041B', '\u041C', '\u041D', '\u041E',
                '\u041F', '\u0420', '\u0421', '\u0422', '\u0423', '\u0424', '\u0425', '\u0426',
                '\u0427', '\u0428', '\u0429', '\u042A', '\u042B', '\u042C', '\u042D', '\u042E',
                '\u042F', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\u0430', '\u0431', '\u0432', '\u0433', '\u0434', '\u0435', '\u0451', '\u0436',
                '\u0437', '\u0438', '\u0439', '\u043A', '\u043B', '\u043C', '\u043D', '\uFFFD',
                '\u043E', '\u043F', '\u0440', '\u0441', '\u0442', '\u0443', '\u0444', '\u0445',
                '\u0446', '\u0447', '\u0448', '\u0449', '\u040A', '\u044B', '\u044C', '\u044D',
                '\u044E', '\u044F', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\u2500',
                '\u2502', '\u250C', '\u2510', '\u2518', '\u2514', '\u251C', '\u252C', '\u2524',
                '\u2534', '\u253C', '\u2501', '\u2503', '\u250F', '\u2513', '\u251B', '\u2517',
                '\u2523', '\u2533', '\u252B', '\u253B', '\u254B', '\u2520', '\u252F', '\u2528',
                '\u2537', '\u253F', '\u251D', '\u2530', '\u2525', '\u2538', '\u2542', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 85 ===
            {
                '\u20AC', '\uFFFD', '\u201A', '\u0192', '\u201E', '\u2026', '\u2020', '\u2021',
                '\u02C6', '\u2030', '\u0160', '\u203A', '\u0152', '\uFFFD', '\u017D', '\uFFFD',
                '\uFFFD', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014',
                '\u02DC', '\u2122', '\u0161', '\u203A', '\u0153', '\uFFFD', '\u017E', '\u0178',
                '\u00A0', '\u00A1', '\u00A2', '\u00A3', '\u00A4', '\u00A5', '\u00A6', '\u00A7',
                '\u00A8', '\u00A9', '\u00AA', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u00AF',
                '\u00B0', '\u00B1', '\u00B2', '\u00B3', '\u00B4', '\u00B5', '\u00B6', '\u00B7',
                '\u00B8', '\u00B9', '\u00BA', '\u00BB', '\u00BC', '\u00BD', '\u00BE', '\u00BF',
                '\uFF61', '\uFF62', '\uFF63', '\uFF64', '\uFF65', '\uFF66', '\uFF67', '\uFF68',
                '\uFF69', '\uFF6A', '\uFF6B', '\uFF6C', '\uFF6D', '\uFF6E', '\uFF6F', '\uFF70',
                '\uFF71', '\uFF72', '\uFF73', '\uFF74', '\uFF75', '\uFF76', '\uFF77', '\uFF78',
                '\uFF79', '\uFF7A', '\uFF7B', '\uFF7C', '\uFF7D', '\uFF7E', '\uFF7F', '\u00C0',
                '\u00C1', '\u00C2', '\u00C3', '\u00C4', '\u00C5', '\u00C6', '\u00C7', '\u00C8',
                '\u00C9', '\u00CA', '\u00CB', '\u00CC', '\u00CD', '\u00CE', '\u00CF', '\u00D0',
                '\u00D1', '\u00D2', '\u00D3', '\u00D4', '\u00D5', '\u00D6', '\u00D7', '\u00D8',
                '\u00D9', '\u00DA', '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF', '\u00E0',
                '\u00E1', '\u00E2', '\u00E3', '\u00E4', '\u00E5', '\u00E6', '\u00E7', '\u00E8',
                '\u00E9', '\u00EA', '\u00EB', '\u00EC', '\u00ED', '\u00EE', '\u00EF', '\u00F0',
                '\u00F1', '\u00F2', '\u00F3', '\u00F4', '\u00F5', '\u00F6', '\u00F7', '\u00F8',
                '\u00F9', '\u00FA', '\u00FB', '\u00FC', '\u00FD', '\u00FE', '\u00FF', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 86 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 87 ===
            {
                '\u2460', '\u2461', '\u2462', '\u2463', '\u2464', '\u2465', '\u2466', '\u2467',
                '\u2468', '\u2469', '\u246A', '\u246B', '\u246C', '\u246D', '\u246E', '\u246F',
                '\u2470', '\u2471', '\u2472', '\u2473', '\u2160', '\u2161', '\u2162', '\u2163',
                '\u2164', '\u2165', '\u2166', '\u2167', '\u2168', '\u2169', '\uFFFD', '\u3349',
                '\u3314', '\u3322', '\u334D', '\u3318', '\u3327', '\u3303', '\u3336', '\u3351',
                '\u3357', '\u330D', '\u3326', '\u3323', '\u332B', '\u334A', '\u333B', '\u339C',
                '\u339D', '\u339E', '\u338E', '\u338F', '\u33C4', '\u33A1', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\u337B', '\uFFFD',
                '\u301D', '\u301F', '\u2116', '\u33CD', '\u2121', '\u32A4', '\u32A5', '\u32A6',
                '\u32A7', '\u32A8', '\u3231', '\u3232', '\u3239', '\u337E', '\u337D', '\u337C',
                '\u2252', '\u2261', '\u222B', '\u222E', '\u2211', '\u221A', '\u22A5', '\u2220',
                '\u221F', '\u22BF', '\u2235', '\u2229', '\u222A', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014', '\u02DC',
                '\u2122', '\u0161', '\u203A', '\u0153', '\uFFFD', '\u017E', '\u0178', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 88 ===
            {
                '\u00C0', '\u00C1', '\u00C2', '\u00C3', '\u00C4', '\u00C5', '\u00C6', '\u00C7',
                '\u00C8', '\u00C9', '\u00CA', '\u00CB', '\u00CC', '\u00CD', '\u00CE', '\u00CF',
                '\u00D0', '\u00D1', '\u00D2', '\u00D3', '\u00D4', '\u00D5', '\u00D6', '\u00D7',
                '\u00D8', '\u00D9', '\u00DA', '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF',
                '\u00E0', '\u00E1', '\u00E2', '\u00E3', '\u00E4', '\u00E5', '\u00E6', '\u00E7',
                '\u00E8', '\u00E9', '\u00EA', '\u00EB', '\u00EC', '\u00ED', '\u00EE', '\u00EF',
                '\u00F0', '\u00F1', '\u00F2', '\u00F3', '\u00F4', '\u00F5', '\u00F6', '\u00F7',
                '\u00F8', '\u00F9', '\u00FA', '\u00FB', '\u00FC', '\u00FD', '\u00FE', '\u00FF',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\u4E9C',
                '\u5516', '\u5A03', '\u963F', '\u54C0', '\u611B', '\u6328', '\u59F6', '\u9022',
                '\u8475', '\u831C', '\u7A50', '\u60AA', '\u63E1', '\u6E25', '\u65ED', '\u8466',
                '\u82A6', '\u9BF5', '\u6893', '\u5727', '\u65A1', '\u6271', '\u5B9B', '\u59D0',
                '\u867B', '\u98F4', '\u7D62', '\u7DBE', '\u9B8E', '\u6216', '\u7C9F', '\u88B7',
                '\u5B89', '\u5EB5', '\u6309', '\u6697', '\u6848', '\u95C7', '\u978D', '\u674F',
                '\u4EE5', '\u4F0A', '\u4F4D', '\u4F9D', '\u5049', '\u56F2', '\u5937', '\u59D4',
                '\u5A01', '\u5C09', '\u60DF', '\u610F', '\u6170', '\u6613', '\u6905', '\u70BA',
                '\u754F', '\u7570', '\u79FB', '\u7DAD', '\u7DEF', '\u80C3', '\u840E', '\u8863',
                '\u8B02', '\u9055', '\u907A', '\u533B', '\u4E95', '\u4EA5', '\u57DF', '\u80B2',
                '\u90C1', '\u78EF', '\u4E00', '\u58F1', '\u6EA2', '\u9038', '\u7A32', '\u8328',
                '\u828B', '\u9C2F', '\u5141', '\u5370', '\u54BD', '\u54E1', '\u56E0', '\u90FB',
                '\u5F15', '\u98F2', '\u6DEB', '\u80E4', '\u852D', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 89 ===
            {
                '\u9662', '\u9670', '\u96A0', '\u97FB', '\u540B', '\u53F3', '\u5B87', '\u70CF',
                '\u7FBD', '\u8FC2', '\u96E8', '\u536F', '\u9D5C', '\u7ABA', '\u4E11', '\u7893',
                '\u81FC', '\u6E26', '\u5618', '\u5504', '\u6B1D', '\u851A', '\u9C3B', '\u59E5',
                '\u53A9', '\u6D66', '\u74DC', '\u958F', '\u5642', '\u4E91', '\u904B', '\u96F2',
                '\u834F', '\u990C', '\u53E1', '\u55B6', '\u5B30', '\u5F71', '\u6620', '\u66F3',
                '\u6804', '\u6C38', '\u6CF3', '\u6D29', '\u745B', '\u76C8', '\u7A4E', '\u9834',
                '\u82F1', '\u885B', '\u8A60', '\u92ED', '\u6DB2', '\u75AB', '\u76CA', '\u99C5',
                '\u60A6', '\u8B01', '\u8D8A', '\u95B2', '\u698E', '\u53AD', '\u5186', '\uFFFD',
                '\u5712', '\u5830', '\u5944', '\u5BB4', '\u5EF6', '\u6028', '\u63A9', '\u63F4',
                '\u6CBF', '\u6F14', '\u708E', '\u7114', '\u7159', '\u71D5', '\u733F', '\u7E01',
                '\u8276', '\u82D1', '\u8597', '\u9060', '\u925B', '\u9D1B', '\u5869', '\u65BC',
                '\u6C5A', '\u7525', '\u51F9', '\u592E', '\u5965', '\u5F80', '\u5FDC', '\u62BC',
                '\u65FA', '\u6A2A', '\u6B27', '\u6BB4', '\u738B', '\u7FC1', '\u8956', '\u9D2C',
                '\u9D0E', '\u9EC4', '\u5CA1', '\u6C96', '\u837B', '\u5104', '\u4B5C', '\u61B6',
                '\u81C6', '\u6876', '\u7261', '\u4E59', '\u4FFA', '\u5378', '\u6069', '\u6E29',
                '\u7A4F', '\u97F3', '\u4E0B', '\u5316', '\u4EEE', '\u4F55', '\u4F3D', '\u4FA1',
                '\u4F73', '\u52A0', '\u53EF', '\u5609', '\u590F', '\u5AC1', '\u5BB6', '\u5BE1',
                '\u79D1', '\u6687', '\u679C', '\u67B6', '\u6B4C', '\u6CB3', '\u706B', '\u73C2',
                '\u798D', '\u79BE', '\u7A3C', '\u7B87', '\u82B1', '\u82DB', '\u8304', '\u8377',
                '\u83EF', '\u83D3', '\u8766', '\u8AB2', '\u5629', '\u8CA8', '\u8FE6', '\u904E',
                '\u971E', '\u868A', '\u4FC4', '\u5CE8', '\u6211', '\u7259', '\u753B', '\u81E5',
                '\u82BD', '\u86FE', '\u8CC0', '\u96C5', '\u9913', '\u99D5', '\u4ECB', '\u4F1A',
                '\u89E3', '\u56DE', '\u584A', '\u58CA', '\u5EFB', '\u5FEB', '\u602A', '\u6094',
                '\u6062', '\u61D0', '\u6212', '\u62D0', '\u6539', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 8A ===
            {
                '\u9B41', '\u6666', '\u68B0', '\u6D77', '\u7070', '\u754C', '\u7686', '\u7D75',
                '\u82A5', '\u87F9', '\u958B', '\u968E', '\u8C9D', '\u51F1', '\u52BE', '\u5916',
                '\u54B3', '\u5BB3', '\u5D16', '\u6168', '\u6982', '\u6DAF', '\u788D', '\u84CB',
                '\u8857', '\u8A72', '\u93A7', '\u9AB8', '\u6D6C', '\u99A8', '\u86D9', '\u57A3',
                '\u67FF', '\u86CE', '\u920E', '\u5283', '\u5687', '\u5404', '\u5ED3', '\u62E1',
                '\u64B9', '\u683C', '\u6838', '\u6BBB', '\u7372', '\u78BA', '\u7A6B', '\u899A',
                '\u89D2', '\u8D6B', '\u8F03', '\u90ED', '\u95A3', '\u9694', '\u9769', '\u5B66',
                '\u5CB3', '\u697D', '\u984D', '\u984E', '\u639B', '\u7B20', '\u6A2B', '\uFFFD',
                '\u6A7F', '\u68B6', '\u9C0D', '\u6F5F', '\u5272', '\u559D', '\u6070', '\u62EC',
                '\u6D3B', '\u6E07', '\u6ED1', '\u845B', '\u8910', '\u8F44', '\u4E14', '\u9C39',
                '\u53F6', '\u691B', '\u6A3A', '\u9784', '\u682A', '\u515C', '\u7AC3', '\u84B2',
                '\u91DC', '\u938C', '\u565B', '\u9D28', '\u6822', '\u9805', '\u8431', '\u7CA5',
                '\u5208', '\u82C5', '\u74E6', '\u4E7E', '\u4F83', '\u51A0', '\u5BD2', '\u520A',
                '\u52D8', '\u52E7', '\u5DFB', '\u559A', '\u582A', '\u59E6', '\u5B8C', '\u5B98',
                '\u5BDB', '\u5E72', '\u5E79', '\u60A3', '\u611F', '\u6163', '\u61BE', '\u63DB',
                '\u6562', '\u67D1', '\u6853', '\u68FA', '\u6B3E', '\u6B53', '\u6C57', '\u6F22',
                '\u6F97', '\u6F45', '\u74B0', '\u7518', '\u76E3', '\u770B', '\u7AFF', '\u7BA1',
                '\u7C21', '\u7DE9', '\u7F36', '\u7FF0', '\u809D', '\u8266', '\u839E', '\u89B3',
                '\u8ACC', '\u8CAB', '\u9084', '\u9451', '\u9593', '\u9591', '\u95A2', '\u9665',
                '\u97D3', '\u9928', '\u8218', '\u4E38', '\u542B', '\u5CB8', '\u5DCC', '\u73A9',
                '\u764C', '\u773C', '\u5CA9', '\u7FEB', '\u8D0B', '\u96C1', '\u9811', '\u9854',
                '\u9858', '\u4F01', '\u4F0E', '\u5371', '\u559C', '\u5668', '\u57FA', '\u5947',
                '\u5B09', '\u5BC4', '\u5C90', '\u5E0C', '\u5E7E', '\u5FCC', '\u63EE', '\u673A',
                '\u65D7', '\u65E2', '\u671F', '\u68CB', '\u68C4', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 8B ===
            {
                '\u6A5F', '\u5E30', '\u6BC5', '\u6C17', '\u6C7D', '\u757F', '\u7948', '\u5B63',
                '\u7A00', '\u7D00', '\u5FBD', '\u898F', '\u8A18', '\u8CB4', '\u8D77', '\u8ECC',
                '\u8F1D', '\u98E2', '\u9A0E', '\u9B3C', '\u4E80', '\u507D', '\u5100', '\u5993',
                '\u5B9C', '\u622F', '\u6280', '\u64EC', '\u6B3A', '\u72A0', '\u7591', '\u7947',
                '\u7FA9', '\u87FB', '\u8ABC', '\u8B70', '\u63AC', '\u83CA', '\u97A0', '\u5409',
                '\u5403', '\u55AB', '\u6854', '\u6A58', '\u8A70', '\u7827', '\u6775', '\u9ECD',
                '\u5374', '\u5BA2', '\u811A', '\u8650', '\u9006', '\u4E18', '\u5E45', '\u4EC7',
                '\u4F11', '\u53CA', '\u5438', '\u5BAE', '\u5F13', '\u6025', '\u6551', '\uFFFD',
                '\u673D', '\u6C42', '\u6C72', '\u6CE3', '\u7078', '\u7403', '\u7A76', '\u7AAE',
                '\u7B08', '\u7D1A', '\u7CFE', '\u7D66', '\u65E7', '\u725B', '\u53BB', '\u5C45',
                '\u5DE8', '\u62D2', '\u62E0', '\u6319', '\u6E20', '\u865A', '\u8A31', '\u8DDD',
                '\u92F8', '\u6F01', '\u79A6', '\u9B5A', '\u4EA8', '\u4EAB', '\u4EAC', '\u4F9B',
                '\u4FA0', '\u50D1', '\u5147', '\u7AF6', '\u5171', '\u51F6', '\u5354', '\u5321',
                '\u537F', '\u53EB', '\u55AC', '\u5883', '\u5CE1', '\u5F37', '\u5F4A', '\u602F',
                '\u6050', '\u606D', '\u631F', '\u6559', '\u6A4B', '\u6CC1', '\u72C2', '\u72ED',
                '\u77EF', '\u80F8', '\u8105', '\u8208', '\u854E', '\u90F7', '\u93E1', '\u97FF',
                '\u9957', '\u9A5A', '\u4EF0', '\u51DD', '\u5C2D', '\u6681', '\u696D', '\u5C40',
                '\u66F2', '\u6975', '\u7389', '\u6850', '\u7C81', '\u50C5', '\u52E4', '\u5747',
                '\u5DFE', '\u9326', '\u65A4', '\u6B23', '\u6B3D', '\u7434', '\u7981', '\u79BD',
                '\u7B4B', '\u7DCA', '\u82B9', '\u83CC', '\u887F', '\u895F', '\u8B39', '\u8FD1',
                '\u91D1', '\u541F', '\u9280', '\u4E5D', '\u5036', '\u53E5', '\u533A', '\u72D7',
                '\u7396', '\u77E9', '\u82E6', '\u8EAF', '\u99C6', '\u99C8', '\u99D2', '\u5177',
                '\u611A', '\u865E', '\u55B0', '\u7A7A', '\u5076', '\u5BD3', '\u9047', '\u9685',
                '\u4E32', '\u6ADB', '\u91E7', '\u5C51', '\u5C48', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 8C ===
            {
                '\u6398', '\u7A9F', '\u6C93', '\u9774', '\u8F61', '\u7AAA', '\u718A', '\u9688',
                '\u7C82', '\u6817', '\u7E70', '\u6851', '\u936C', '\u52F2', '\u541B', '\u85AB',
                '\u8A13', '\u7FA4', '\u8ECD', '\u90E1', '\u5366', '\u8888', '\u7941', '\u4FC2',
                '\u50BE', '\u5211', '\u5144', '\u5553', '\u572D', '\u73EA', '\u578B', '\u5951',
                '\u5F62', '\u5F84', '\u6075', '\u6176', '\u6167', '\u61A9', '\u63B2', '\u643A',
                '\u656C', '\u666F', '\u6842', '\u6E13', '\u7566', '\u7A3D', '\u7CFB', '\u7D4C',
                '\u7D99', '\u7E4B', '\u7F6B', '\u800E', '\u834A', '\u86CD', '\u8A08', '\u8A63',
                '\u8B66', '\u8EFD', '\u981A', '\u9D8F', '\u82B8', '\u8FCE', '\u9BE8', '\uFFFD',
                '\u5287', '\u621F', '\u6483', '\u6FC0', '\u9699', '\u6841', '\u5091', '\u6B20',
                '\u6C7A', '\u6F54', '\u7A74', '\u7D50', '\u8840', '\u8A23', '\u6708', '\u4EF6',
                '\u5039', '\u5026', '\u5065', '\u517C', '\u5238', '\u5263', '\u55A7', '\u570F',
                '\u5805', '\u5ACC', '\u5EFA', '\u61B2', '\u61F8', '\u62F3', '\u7263', '\u691C',
                '\u6A29', '\u727D', '\u72AC', '\u732E', '\u7814', '\u786F', '\u7D79', '\u770C',
                '\u80A9', '\u898B', '\u8B19', '\u8CE2', '\u8ED2', '\u9063', '\u9375', '\u967A',
                '\u9855', '\u9A13', '\u9E78', '\u5143', '\u539F', '\u53B3', '\u5E7B', '\u5F26',
                '\u6E1B', '\u6E90', '\u7384', '\u73FE', '\u7D43', '\u8237', '\u8A00', '\u8AFA',
                '\u9650', '\u4E4E', '\u500B', '\u53E4', '\u547C', '\u56FA', '\u59D1', '\u5B64',
                '\u5DF1', '\u5EAB', '\u5F27', '\u6238', '\u6545', '\u67AF', '\u6E56', '\u72D0',
                '\u7CCA', '\u88B4', '\u80A1', '\u80E1', '\u83F0', '\u864E', '\u8A87', '\u8DE8',
                '\u9237', '\u96C7', '\u9867', '\u9F13', '\u4E94', '\u4E92', '\u4F0D', '\u5348',
                '\u5449', '\u543E', '\u5A2F', '\u5F8C', '\u5FA1', '\u609F', '\u68A7', '\u6A8E',
                '\u745A', '\u7881', '\u8A9E', '\u8AA4', '\u8B77', '\u9190', '\u4E5E', '\u9BC9',
                '\u4EA4', '\u4F7C', '\u4FAF', '\u5019', '\u5016', '\u5149', '\u516C', '\u529F',
                '\u52B9', '\u52FE', '\u539A', '\u53E3', '\u5411', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 8D ===
            {
                '\u540E', '\u5589', '\u5751', '\u57A2', '\u597D', '\u5B54', '\u5B5D', '\u5B8F',
                '\u5DE5', '\u5DE7', '\u5DF7', '\u5E78', '\u5E83', '\u5E9A', '\u5EB7', '\u5F18',
                '\u6052', '\u614C', '\u6297', '\u62D8', '\u63A7', '\u653B', '\u6602', '\u6643',
                '\u66F4', '\u676D', '\u6821', '\u6897', '\u69CB', '\u6C5F', '\u6D2A', '\u6D69',
                '\u6E2F', '\u6E9D', '\u7532', '\u7687', '\u786C', '\u7A3F', '\u7CE0', '\u7D05',
                '\u7D18', '\u7D5E', '\u7DB1', '\u8015', '\u8003', '\u80AF', '\u80B1', '\u8154',
                '\u818F', '\u822A', '\u8352', '\u884C', '\u8861', '\u8B1B', '\u8CA2', '\u8CFC',
                '\u90CA', '\u9175', '\u9271', '\u783F', '\u92FC', '\u95A4', '\u964D', '\uFFFD',
                '\u9805', '\u9999', '\u9AD8', '\u9D3B', '\u525B', '\u52AB', '\u53F7', '\u5408',
                '\u58D5', '\u62F7', '\u6FE0', '\u8C6A', '\u8F5F', '\u9EB9', '\u514B', '\u523B',
                '\u544A', '\u56FD', '\u7A40', '\u9177', '\u9D60', '\u9ED2', '\u7344', '\u6F09',
                '\u8170', '\u7511', '\u5FFD', '\u60DA', '\u9AA8', '\u72DB', '\u8FBC', '\u6B64',
                '\u9803', '\u4ECA', '\u56F0', '\u5764', '\u58BE', '\u5A5A', '\u6068', '\u61C7',
                '\u660F', '\u6606', '\u6839', '\u68B1', '\u6DF7', '\u75D5', '\u7D3A', '\u826E',
                '\u9B42', '\u4E9B', '\u4F50', '\u53C9', '\u5506', '\u5D6F', '\u5DE6', '\u5DEE',
                '\u67FB', '\u6C99', '\u7473', '\u7802', '\u8A50', '\u9396', '\u88DF', '\u5750',
                '\u5EA7', '\u632B', '\u50B5', '\u50AC', '\u518D', '\u6700', '\u54C9', '\u585E',
                '\u59BB', '\u5BB0', '\u5F69', '\u624D', '\u63A1', '\u683D', '\u6B73', '\u6E08',
                '\u707D', '\u91C7', '\u7280', '\u7815', '\u7826', '\u796D', '\u658E', '\u7D30',
                '\u83DC', '\u88C1', '\u8F09', '\u969B', '\u5264', '\u5728', '\u6750', '\u7F6A',
                '\u8CA1', '\u51B4', '\u5742', '\u962A', '\u583A', '\u698A', '\u80B4', '\u54B2',
                '\u5D0E', '\u57FC', '\u7895', '\u9DFA', '\u4F5C', '\u524A', '\u548B', '\u643E',
                '\u6628', '\u6714', '\u67F5', '\u7A84', '\u7B56', '\u7D22', '\u932F', '\u685C',
                '\u9BAD', '\u7B39', '\u5319', '\u518A', '\u5237', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 8E ===
            {
                '\u5BDF', '\u62F6', '\u64A4', '\u64E6', '\u672D', '\u6BBA', '\u85A9', '\u96D1',
                '\u7690', '\u9BD6', '\u634C', '\u9306', '\u9BAB', '\u76BF', '\u6652', '\u4E09',
                '\u5098', '\u53C2', '\u5C71', '\u60E8', '\u6492', '\u6563', '\u685F', '\u71E6',
                '\u73CA', '\u7523', '\u7B97', '\u7E82', '\u8695', '\u8B83', '\u8CDB', '\u9178',
                '\u9910', '\u65AC', '\u66AB', '\u6B8B', '\u4ED5', '\u4ED4', '\u4F3A', '\u4F7F',
                '\u523A', '\u53F8', '\u53F2', '\u55E3', '\u56DB', '\u58EB', '\u59CB', '\u59C9',
                '\u59FF', '\u5B50', '\u5C4D', '\u5E02', '\u5E2B', '\u5FD7', '\u601D', '\u6307',
                '\u652F', '\u5B5C', '\u65AF', '\u65BD', '\u65E8', '\u679D', '\u6B62', '\uFFFD',
                '\u6B7B', '\u6C0F', '\u7345', '\u7949', '\u79C1', '\u7CF8', '\u7D19', '\u7D2B',
                '\u80A2', '\u8102', '\u81F3', '\u8996', '\u8A5E', '\u8A69', '\u8A66', '\u8A8C',
                '\u8AEE', '\u8CC7', '\u8CDC', '\u96CC', '\u98FC', '\u6B6F', '\u4E8B', '\u4F3C',
                '\u4F8D', '\u5150', '\u5B57', '\u5BFA', '\u6148', '\u6301', '\u6642', '\u6B21',
                '\u6ECB', '\u6CBB', '\u723E', '\u74BD', '\u75D4', '\u78C1', '\u793A', '\u800C',
                '\u8033', '\u81EA', '\u8494', '\u8F9E', '\u6C50', '\u9E7F', '\u5F0F', '\u8B58',
                '\u9D2B', '\u7AFA', '\u8EF8', '\u5B8D', '\u96EB', '\u4E03', '\u53F1', '\u57F7',
                '\u5931', '\u5AC9', '\u5BA4', '\u6089', '\u6E7F', '\u6F06', '\u75BE', '\u8CEA',
                '\u5B9F', '\u8500', '\u7BE0', '\u5072', '\u67F4', '\u829D', '\u5C61', '\u854A',
                '\u7E1E', '\u820E', '\u5199', '\u5C04', '\u6368', '\u8D66', '\u659C', '\u716E',
                '\u793E', '\u7D17', '\u8005', '\u8B1D', '\u8ECA', '\u906E', '\u86C7', '\u90AA',
                '\u501F', '\u52FA', '\u5C3A', '\u6753', '\u707C', '\u7235', '\u914C', '\u91C8',
                '\u932B', '\u82E5', '\u5BC2', '\u5F31', '\u60F9', '\u4E3B', '\u53D6', '\u5B88',
                '\u624B', '\u6731', '\u6B8A', '\u72E9', '\u73E0', '\u7A2E', '\u816B', '\u8DA3',
                '\u9152', '\u9996', '\u5112', '\u53D7', '\u546A', '\u5BFF', '\u6388', '\u6A39',
                '\u7DAC', '\u9700', '\u56DA', '\u53CE', '\u5468', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 8F ===
            {
                '\u5B97', '\u5C31', '\u5DDE', '\u4FEE', '\u6101', '\u62FE', '\u6D32', '\u79C0',
                '\u79CB', '\u7D42', '\u7E4D', '\u7FD2', '\u81ED', '\u821F', '\u8490', '\u8846',
                '\u8972', '\u8B90', '\u8E74', '\u8F2F', '\u9031', '\u914B', '\u916C', '\u96C6',
                '\u919C', '\u4EC0', '\u4F4F', '\u5145', '\u5341', '\u5F93', '\u620E', '\u67D4',
                '\u6C41', '\u6E0B', '\u7363', '\u7E26', '\u91CD', '\u9283', '\u53D4', '\u5919',
                '\u5BBF', '\u6DD1', '\u795D', '\u7E2E', '\u7C9B', '\u587E', '\u719F', '\u51FA',
                '\u8853', '\u8FF0', '\u4FCA', '\u5CFB', '\u6625', '\u77AC', '\u7AE3', '\u821C',
                '\u99FF', '\u51C6', '\u5FAA', '\u65EC', '\u696F', '\u6B89', '\u6DF3', '\uFFFD',
                '\u6E96', '\u6F64', '\u76FE', '\u7D14', '\u5DE1', '\u9075', '\u9187', '\u9806',
                '\u51E6', '\u521D', '\u6240', '\u6691', '\u66D9', '\u6E1A', '\u5EB6', '\u7DD2',
                '\u7F72', '\u66F8', '\u85AF', '\u85F7', '\u8AF8', '\u52A9', '\u53D9', '\u5973',
                '\u5E8F', '\u5F90', '\u6055', '\u92E4', '\u9664', '\u50B7', '\u511F', '\u52DD',
                '\u5320', '\u5347', '\u53EC', '\u54E8', '\u5546', '\u5531', '\u5617', '\u5968',
                '\u59BE', '\u5A3C', '\u5BB5', '\u5C06', '\u5C0F', '\u5C11', '\u5C1A', '\u5E84',
                '\u5E8A', '\u5EE0', '\u5F70', '\u627F', '\u6284', '\u62DB', '\u638C', '\u6377',
                '\u6607', '\u660C', '\u662D', '\u6676', '\u677E', '\u68A2', '\u6A1F', '\u6A35',
                '\u6CBC', '\u6D88', '\u6E09', '\u6E58', '\u713C', '\u7126', '\u7167', '\u75C7',
                '\u7701', '\u785D', '\u7901', '\u7965', '\u79F0', '\u7AE0', '\u7B11', '\u7CA7',
                '\u7D39', '\u8096', '\u83D6', '\u848B', '\u8549', '\u885D', '\u88F3', '\u8A1F',
                '\u8A3C', '\u8A51', '\u8A73', '\u8C61', '\u8CDE', '\u91A4', '\u9266', '\u937E',
                '\u9418', '\u969C', '\u9798', '\u4E0A', '\u4E08', '\u4E1E', '\u4E57', '\u5197',
                '\u5270', '\u57CE', '\u5834', '\u58CC', '\u5B22', '\u5E38', '\u60C5', '\u64FE',
                '\u6761', '\u6756', '\u6D44', '\u72B6', '\u7573', '\u7A63', '\u84B8', '\u8B72',
                '\u91B8', '\u9320', '\u5631', '\u57F4', '\u98FE', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 90 ===
            {
                '\u62ED', '\u690D', '\u6B96', '\u71ED', '\u7E54', '\u8077', '\u8272', '\u89E6',
                '\u98DF', '\u8755', '\u8FB1', '\u5C3B', '\u4F38', '\u4FE1', '\u4FB5', '\u5507',
                '\u5A20', '\u5BDD', '\u5BE9', '\u5FC3', '\u614E', '\u632F', '\u65B0', '\u664B',
                '\u68EE', '\u699B', '\u6D78', '\u6DF1', '\u7533', '\u75B9', '\u771F', '\u795E',
                '\u79E6', '\u7D33', '\u81E3', '\u82AF', '\u85AA', '\u89AA', '\u8A3A', '\u8EAB',
                '\u60B2', '\u9032', '\u91DD', '\u9707', '\u4EBA', '\u4EC1', '\u5203', '\u5875',
                '\u58EC', '\u5C0B', '\u751A', '\u5C3D', '\u814E', '\u8A0A', '\u8FC5', '\u9663',
                '\u976D', '\u7B25', '\u8ACF', '\u9808', '\u9162', '\u56F3', '\u53A8', '\uFFFD',
                '\u9017', '\u5439', '\u5782', '\u5E25', '\u63A8', '\u6C34', '\u708A', '\u7761',
                '\u7C8B', '\u7FE0', '\u8870', '\u9042', '\u9154', '\u9310', '\u9318', '\u968F',
                '\u745E', '\u9AC4', '\u5D07', '\u5D69', '\u6570', '\u67A2', '\u8DA8', '\u96DB',
                '\u636E', '\u6749', '\u6919', '\u83C5', '\u9817', '\u96C0', '\u88FE', '\u6F84',
                '\u647A', '\u5BF8', '\u4E16', '\u702C', '\u755D', '\u662F', '\u51C4', '\u5236',
                '\u52E2', '\u59D3', '\u5F81', '\u6027', '\u6210', '\u653F', '\u6574', '\u661F',
                '\u6674', '\u68F2', '\u6816', '\u6B63', '\u6E05', '\u7272', '\u751F', '\u76DB',
                '\u7CBE', '\u8056', '\u58F0', '\u88FD', '\u897F', '\u8AA0', '\u8A93', '\u8ACB',
                '\u901D', '\u9192', '\u9752', '\u9759', '\u6589', '\u7A0E', '\u8106', '\u96BB',
                '\u5E2D', '\u60DC', '\u621A', '\u65A5', '\u6614', '\u6790', '\u77F3', '\u7A4D',
                '\u7C4D', '\u7E3E', '\u810A', '\u8CAC', '\u8D64', '\u8DE1', '\u8E5F', '\u78A9',
                '\u5207', '\u62D9', '\u63A5', '\u6442', '\u6298', '\u8A2D', '\u7A83', '\u7BC0',
                '\u8AAC', '\u96EA', '\u7D76', '\u820C', '\u8749', '\u4ED9', '\u5148', '\u5343',
                '\u6053', '\u5BA3', '\u5C02', '\u5C16', '\u5DDD', '\u6226', '\u6247', '\u64B0',
                '\u6813', '\u6834', '\u6CC9', '\u6D45', '\u6D17', '\u67D3', '\u6F5C', '\u714E',
                '\u717D', '\u65CB', '\u7A7F', '\u7BAD', '\u7DDA', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 91 ===
            {
                '\u7E4A', '\u7FA8', '\u817A', '\u821B', '\u8239', '\u85A6', '\u8A6E', '\u8CCE',
                '\u8DF5', '\u9078', '\u9077', '\u92AD', '\u9291', '\u9583', '\u9BAE', '\u524D',
                '\u5584', '\u6F38', '\u7136', '\u5168', '\u7985', '\u7E55', '\u81B3', '\u7CCE',
                '\u564C', '\u5851', '\u5CA8', '\u63AA', '\u66FE', '\u66FD', '\u695A', '\u72D9',
                '\u758F', '\u758E', '\u790E', '\u7956', '\u79DF', '\u7C97', '\u7D20', '\u7D44',
                '\u8607', '\u8A34', '\u963B', '\u9061', '\u9F20', '\u50E7', '\u5275', '\u53CC',
                '\u53E2', '\u5009', '\u55AA', '\u58EE', '\u594F', '\u723D', '\u5B8B', '\u5C64',
                '\u531D', '\u60E3', '\u60F3', '\u635C', '\u6383', '\u633F', '\u63BB', '\uFFFD',
                '\u64CD', '\u65E9', '\u66F9', '\u5DE3', '\u69CD', '\u69FD', '\u6F15', '\u71E5',
                '\u4E89', '\u75E9', '\u76F8', '\u7A93', '\u7CDF', '\u7DCF', '\u7D9C', '\u8061',
                '\u8349', '\u8358', '\u846C', '\u84BC', '\u85FB', '\u88C5', '\u8D70', '\u9001',
                '\u906D', '\u9397', '\u971C', '\u9A12', '\u50CF', '\u5897', '\u618E', '\u81D3',
                '\u8535', '\u8D08', '\u9020', '\u4FC3', '\u5074', '\u5247', '\u5373', '\u606F',
                '\u6349', '\u675F', '\u6E2C', '\u8DB3', '\u901F', '\u4FD7', '\u5C5E', '\u8CCA',
                '\u65CF', '\u7D9A', '\u5352', '\u8896', '\u5176', '\u63C3', '\u5B58', '\u5B6B',
                '\u5C0A', '\u640D', '\u6751', '\u905C', '\u4ED6', '\u591A', '\u592A', '\u6C70',
                '\u8A51', '\u553E', '\u5815', '\u59A5', '\u60F0', '\u6253', '\u67C1', '\u8235',
                '\u6955', '\u9640', '\u99C4', '\u9A28', '\u4F53', '\u5806', '\u5BFE', '\u8010',
                '\u5CB1', '\u5E2F', '\u5F85', '\u6020', '\u614B', '\u6234', '\u66FF', '\u6CF0',
                '\u6EDE', '\u80CE', '\u817F', '\u82D4', '\u888B', '\u8CB8', '\u9000', '\u902E',
                '\u968A', '\u9EDB', '\u9BDB', '\u4EE3', '\u53F0', '\u5927', '\u7B2C', '\u918D',
                '\u984C', '\u9DF9', '\u6EDD', '\u7027', '\u5353', '\u5544', '\u5B85', '\u6258',
                '\u629E', '\u62D3', '\u6CA2', '\u6FEF', '\u7422', '\u8A17', '\u9438', '\u6FC1',
                '\u8AFE', '\u8338', '\u51E7', '\u86F8', '\u53EA', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 92 ===
            {
                '\u53E9', '\u4F46', '\u9054', '\u8FB0', '\u596A', '\u8131', '\u5DFD', '\u7AEA',
                '\u8FBF', '\u68DA', '\u8C37', '\u72F8', '\u9C48', '\u6A3D', '\u8AB0', '\u4E39',
                '\u5358', '\u5606', '\u5766', '\u62C5', '\u63A2', '\u65E6', '\u6B4E', '\u6DE1',
                '\u6E5B', '\u70AD', '\u77ED', '\u7AEF', '\u7BAA', '\u7DBB', '\u803D', '\u80C6',
                '\u86CB', '\u8A95', '\u935B', '\u56E3', '\u58C7', '\u5F3E', '\u65AD', '\u6696',
                '\u6A80', '\u6BB5', '\u7537', '\u8AC7', '\u5024', '\u77E5', '\u5730', '\u5F1B',
                '\u6065', '\u667A', '\u6C60', '\u75F4', '\u7A1A', '\u7F6E', '\u81F4', '\u8718',
                '\u9045', '\u99B3', '\u7BC9', '\u755C', '\u7AF9', '\u7B51', '\u84C4', '\uFFFD',
                '\u9010', '\u79E9', '\u7A92', '\u8336', '\u5AE1', '\u7740', '\u4E2D', '\u4EF2',
                '\u5B99', '\u5FE0', '\u62BD', '\u663C', '\u67F1', '\u6CE8', '\u866B', '\u8877',
                '\u8A3B', '\u914E', '\u92F3', '\u99D0', '\u6A17', '\u7026', '\u732A', '\u82E7',
                '\u8457', '\u8CAF', '\u4E01', '\u5146', '\u51CB', '\u558B', '\u5BF5', '\u5E16',
                '\u5E33', '\u5E81', '\u5F14', '\u5F35', '\u5F6B', '\u5FB4', '\u61F2', '\u6311',
                '\u66A2', '\u671D', '\u6F6E', '\u7252', '\u753A', '\u773A', '\u8074', '\u8139',
                '\u8178', '\u8776', '\u8ABF', '\u8ADC', '\u8D85', '\u8DF3', '\u929A', '\u9577',
                '\u9802', '\u9CE5', '\u52C5', '\u6357', '\u76F4', '\u6715', '\u6C88', '\u73CD',
                '\u8CC3', '\u93AE', '\u9673', '\u6D25', '\u589C', '\u690E', '\u69CC', '\u8FFD',
                '\u939A', '\u75DB', '\u901A', '\u585A', '\u6802', '\u63B4', '\u69FB', '\u4F43',
                '\u6F2C', '\u67D8', '\u8FBB', '\u8526', '\u7DB4', '\u9354', '\u693F', '\u6F70',
                '\u576A', '\u58F7', '\u5B2C', '\u7D2C', '\u722A', '\u540A', '\u91E3', '\u9DB4',
                '\u4EAD', '\u4F4E', '\u505C', '\u5075', '\u5243', '\u8C9E', '\u5448', '\u5824',
                '\u5B9A', '\u5E1D', '\u5E95', '\u5EAD', '\u5EF7', '\u5F1F', '\u608C', '\u62B5',
                '\u633A', '\u63D0', '\u68AF', '\u6C40', '\u7887', '\u798E', '\u7A0B', '\u7DE0',
                '\u8247', '\u8A02', '\u8AE6', '\u8E44', '\u9013', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 93 ===
            {
                '\u90B8', '\u912D', '\u91D8', '\u9F0E', '\u6CE5', '\u6458', '\u64E2', '\u6575',
                '\u6EF4', '\u7684', '\u7B1B', '\u9069', '\u93D1', '\u6EBA', '\u54F2', '\u5FB9',
                '\u64A4', '\u8F4D', '\u8FED', '\u9244', '\u5178', '\u586B', '\u5929', '\u5C55',
                '\u5E97', '\u6DFB', '\u7E8F', '\u751C', '\u8CBC', '\u8EE2', '\u985B', '\u70B9',
                '\u4F1D', '\u6BBF', '\u6FB1', '\u7530', '\u96FB', '\u514E', '\u5410', '\u5835',
                '\u5857', '\u59AC', '\u5C60', '\u5F92', '\u6597', '\u675C', '\u6E21', '\u767B',
                '\u83DF', '\u8CED', '\u9014', '\u90FD', '\u934D', '\u7825', '\u783A', '\u52AA',
                '\u5EA6', '\u571F', '\u5974', '\u6012', '\u5012', '\u515A', '\u51AC', '\uFFFD',
                '\u51CD', '\u5200', '\u5510', '\u5854', '\u5858', '\u5957', '\u5B95', '\u5CF6',
                '\u5D8B', '\u60BC', '\u6295', '\u642D', '\u6771', '\u6843', '\u68BC', '\u68DF',
                '\u76D7', '\u6DD8', '\u6E6F', '\u6D9B', '\u706F', '\u71C8', '\u5F53', '\u75D8',
                '\u7977', '\u7B49', '\u7B54', '\u7B52', '\u7CD6', '\u7D71', '\u5230', '\u8463',
                '\u8569', '\u85E4', '\u8A0E', '\u8B04', '\u8C46', '\u8E0F', '\u9003', '\u900F',
                '\u9419', '\u9676', '\u982D', '\u9A30', '\u95D8', '\u50CD', '\u52D5', '\u540C',
                '\u5802', '\u5C0E', '\u61A7', '\u649E', '\u6D1E', '\u77B3', '\u7AE5', '\u80F4',
                '\u8404', '\u9053', '\u9285', '\u5CE0', '\u9D07', '\u533F', '\u5F97', '\u5FB3',
                '\u6D9C', '\u7279', '\u7763', '\u79BF', '\u7BE4', '\u6BD2', '\u72EC', '\u8AAD',
                '\u6803', '\u6A61', '\u51F8', '\u7A81', '\u6934', '\u5C4A', '\u9CF6', '\u82EB',
                '\u5BC5', '\u9149', '\u701E', '\u5678', '\u5C6F', '\u60C7', '\u6566', '\u6C8C',
                '\u8C5A', '\u9041', '\u9813', '\u5451', '\u66C7', '\u920D', '\u5948', '\u90A3',
                '\u5185', '\u4E4D', '\u51EA', '\u8599', '\u8B0E', '\u7058', '\u637A', '\u934B',
                '\u6962', '\u99B4', '\u7E04', '\u7577', '\u5357', '\u6960', '\u8EDF', '\u96E3',
                '\u6C5D', '\u4E8C', '\u5C3C', '\u5F10', '\u8FE9', '\u5302', '\u8CD1', '\u8089',
                '\u8679', '\u5EFF', '\u65E5', '\u4E73', '\u5165', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 94 ===
            {
                '\u5982', '\u5C3F', '\u97EE', '\u4EFB', '\u598A', '\u5FCD', '\u8A8D', '\u6FE1',
                '\u79B0', '\u7962', '\u5BE7', '\u8471', '\u732B', '\u71B1', '\u5E74', '\u5FF5',
                '\u637B', '\u649A', '\u71C3', '\u7C98', '\u4E43', '\u5EFC', '\u4E4B', '\u57DC',
                '\u56A2', '\u60A9', '\u6FC3', '\u7D0D', '\u80FD', '\u8133', '\u81BF', '\u8FB2',
                '\u8997', '\u86A4', '\u5DF4', '\u628A', '\u64AD', '\u8987', '\u6777', '\u6CE2',
                '\u6D3E', '\u7436', '\u7834', '\u5A46', '\u7F75', '\u82AD', '\u99AC', '\u4FF3',
                '\u5EC3', '\u62DD', '\u6392', '\u6557', '\u676F', '\u76C3', '\u744C', '\u80CC',
                '\u80BA', '\u8F29', '\u914D', '\u500D', '\u57F9', '\u5A92', '\u6885', '\uFFFD',
                '\u6973', '\u7164', '\u72FD', '\u8CB7', '\u58F2', '\u8CE0', '\u966A', '\u9019',
                '\u877F', '\u79E4', '\u77E7', '\u8429', '\u4F2F', '\u5265', '\u535A', '\u62CD',
                '\u67CF', '\u6CCA', '\u767D', '\u7B94', '\u7C95', '\u8236', '\u8584', '\u8FEB',
                '\u66DD', '\u6F20', '\u7206', '\u7E1B', '\u83AB', '\u99C1', '\u9EA6', '\u51FD',
                '\u7BB1', '\u7872', '\u7BB8', '\u8087', '\u7B48', '\u68E8', '\u5E61', '\u808C',
                '\u7551', '\u7560', '\u516B', '\u9262', '\u6E8C', '\u767A', '\u9197', '\u9AEA',
                '\u4F10', '\u7F70', '\u629C', '\u7B4F', '\u95A5', '\u9CE9', '\u567A', '\u5859',
                '\u86E4', '\u96BC', '\u4F34', '\u5224', '\u534A', '\u53CD', '\u53DB', '\u5E06',
                '\u642C', '\u6591', '\u677F', '\u6C3E', '\u6E4E', '\u7248', '\u72AF', '\u73ED',
                '\u7554', '\u7E41', '\u822C', '\u85E9', '\u8CA9', '\u7BC4', '\u91C6', '\u7169',
                '\u9812', '\u98EF', '\u633D', '\u6669', '\u756A', '\u76E4', '\u78D0', '\u8543',
                '\u86EE', '\u532A', '\u5351', '\u5426', '\u8359', '\u5E87', '\u5F7C', '\u60B2',
                '\u6249', '\u6279', '\u62AB', '\u6590', '\u6BD4', '\u6CCC', '\u75B2', '\u76AE',
                '\u7891', '\u79D8', '\u7DCB', '\u7F77', '\u80A5', '\u88AB', '\u8AB9', '\u8CBB',
                '\u907F', '\u975E', '\u98DB', '\u6A0B', '\u7C38', '\u5099', '\u5C3E', '\u5FAE',
                '\u6787', '\u6BD8', '\u7435', '\u7709', '\u7F8E', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 95 ===
            {
                '\u9F3B', '\u67CA', '\u7A17', '\u5339', '\u758B', '\u9AED', '\u5F66', '\u819D',
                '\u83F1', '\u8098', '\u5F3C', '\u5FC5', '\u7562', '\u7B46', '\u903C', '\u6867',
                '\u59EB', '\u5A9B', '\u7D10', '\u767E', '\u8B2C', '\u4FF5', '\u5F6A', '\u6A19',
                '\u6C37', '\u6F02', '\u74E2', '\u7968', '\u8868', '\u8A55', '\u8C79', '\u5EDF',
                '\u63CF', '\u75C5', '\u79D2', '\u82D7', '\u9328', '\u92F2', '\u849C', '\u86ED',
                '\u9C2D', '\u54C1', '\u5F6C', '\u658C', '\u6D5C', '\u7015', '\u8CA7', '\u8CD3',
                '\u983B', '\u654F', '\u74F6', '\u4E0D', '\u4ED8', '\u57E0', '\u592B', '\u5A66',
                '\u5BCC', '\u51A8', '\u5E03', '\u5E9C', '\u6016', '\u6276', '\u6577', '\uFFFD',
                '\u65A7', '\u666E', '\u6D6E', '\u7236', '\u7B26', '\u8150', '\u819A', '\u8299',
                '\u8B5C', '\u8CA0', '\u8CE6', '\u8D74', '\u961C', '\u9644', '\u4FAE', '\u64AB',
                '\u6B66', '\u821E', '\u8461', '\u856A', '\u90E8', '\u5C01', '\u6953', '\u98A8',
                '\u847A', '\u8557', '\u4F0F', '\u526F', '\u5FA9', '\u4E45', '\u670D', '\u798F',
                '\u8179', '\u8907', '\u8986', '\u6DF5', '\u5F17', '\u6255', '\u6CB8', '\u4ECF',
                '\u7269', '\u9B92', '\u5206', '\u543B', '\u5674', '\u58B3', '\u61A4', '\u626E',
                '\u711A', '\u596E', '\u7C89', '\u7CDE', '\u7D1B', '\u96F0', '\u6587', '\u805E',
                '\u4E19', '\u4F75', '\u5175', '\u5840', '\u5E63', '\u5E73', '\u5F0A', '\u67C4',
                '\u4E26', '\u853D', '\u9589', '\u965B', '\u7C73', '\u9801', '\u50FB', '\u58C1',
                '\u7656', '\u78A7', '\u5225', '\u77A5', '\u8511', '\u7B86', '\u504F', '\u5909',
                '\u7247', '\u7BC7', '\u7DE8', '\u8FBA', '\u8FD4', '\u904F', '\u4FBF', '\u52C9',
                '\u5A29', '\u5F01', '\u97AD', '\u4FDD', '\u8217', '\u92EA', '\u5703', '\u6355',
                '\u6B69', '\u752B', '\u88DC', '\u8F14', '\u7A42', '\u52DF', '\u5893', '\u6155',
                '\u620A', '\u66AE', '\u6BCD', '\u7C3F', '\u83E9', '\u5023', '\u4FF8', '\u5305',
                '\u5445', '\u5831', '\u5949', '\u5B9D', '\u5CF0', '\u5CEF', '\u5D29', '\u5E96',
                '\u62B1', '\u6367', '\u653E', '\u65B9', '\u670B', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 96 ===
            {
                '\u6CD5', '\u6CE1', '\u70F9', '\u7832', '\u7E2B', '\u80DE', '\u82B3', '\u840C',
                '\u84EC', '\u8702', '\u8912', '\u8A2A', '\u8C4A', '\u90A6', '\u92D2', '\u98FD',
                '\u9CF3', '\u9D6C', '\u4E4F', '\u4EA1', '\u508D', '\u5256', '\u574A', '\u59A8',
                '\u5E3D', '\u5FD8', '\u5FD9', '\u623F', '\u66B4', '\u671B', '\u67D0', '\u68D2',
                '\u5192', '\u7D21', '\u80AA', '\u81A8', '\u8B00', '\u8C8C', '\u8CBF', '\u927E',
                '\u9632', '\u5420', '\u982C', '\u5317', '\u50D5', '\u535C', '\u58A8', '\u64B2',
                '\u6734', '\u7267', '\u7766', '\u7A46', '\u91E6', '\u52C3', '\u6CA1', '\u6B86',
                '\u5800', '\u5E4C', '\u5954', '\u672C', '\u7FFB', '\u51E1', '\u76C6', '\uFFFD',
                '\u6469', '\u78E8', '\u9B54', '\u9EBB', '\u57CB', '\u59B9', '\u6627', '\u679A',
                '\u6BCE', '\u54E9', '\u69D9', '\u5E55', '\u819C', '\u6795', '\u9BAA', '\u67FE',
                '\u9C52', '\u685D', '\u4EA6', '\u4FE3', '\u53C8', '\u62B9', '\u672B', '\u6CAB',
                '\u8FC4', '\u4FAD', '\u7E6D', '\u9EBF', '\u4E07', '\u6162', '\u6E80', '\u6F2B',
                '\u8513', '\u5473', '\u672A', '\u9B45', '\u5DF3', '\u7B95', '\u5CAC', '\u5BC6',
                '\u871C', '\u6E4A', '\u84D1', '\u7A14', '\u8108', '\u5999', '\u7C8D', '\u6C11',
                '\u7720', '\u52D9', '\u5922', '\u7121', '\u725F', '\u77DB', '\u9727', '\u9D61',
                '\u690B', '\u5A7F', '\u5A18', '\u51A5', '\u540D', '\u547D', '\u660E', '\u76DF',
                '\u8FF7', '\u9298', '\u9CF4', '\u59EA', '\u725D', '\u6EC5', '\u514D', '\u67C9',
                '\u7DBF', '\u7DEC', '\u9762', '\u9EBA', '\u6478', '\u6A21', '\u8302', '\u5984',
                '\u5B5F', '\u6BDB', '\u731B', '\u76F2', '\u7DB2', '\u8017', '\u8499', '\u5132',
                '\u6728', '\u9ED9', '\u76EE', '\u6762', '\u52FF', '\u9905', '\u5C24', '\u623B',
                '\u7C7E', '\u8CB0', '\u554F', '\u60B6', '\u7D0B', '\u9580', '\u5301', '\u4E5F',
                '\u51B6', '\u591C', '\u723A', '\u8036', '\u91CE', '\u5F25', '\u77E2', '\u5384',
                '\u5F79', '\u7D04', '\u85AC', '\u8A33', '\u8E8D', '\u9756', '\u67F3', '\u85AE',
                '\u9453', '\u6109', '\u6108', '\u6CB9', '\u7652', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 97 ===
            {
                '\u8AED', '\u8F38', '\u552F', '\u4F51', '\u512A', '\u52C7', '\u53CB', '\u5BA5',
                '\u5E7D', '\u60A0', '\u6182', '\u63D6', '\u6709', '\u67DA', '\u6E67', '\u6D8C',
                '\u7336', '\u7337', '\u7531', '\u7950', '\u88D5', '\u8A98', '\u904A', '\u9091',
                '\u90F5', '\u96C4', '\u878D', '\u5915', '\u4E88', '\u4F59', '\u4E0E', '\u8A89',
                '\u8F3F', '\u9810', '\u50AD', '\u5E7C', '\u5996', '\u5BB9', '\u5EB8', '\u63DA',
                '\u63FA', '\u64C1', '\u66DC', '\u694A', '\u69D8', '\u6D0B', '\u6EB6', '\u7194',
                '\u7528', '\u7AAF', '\u7F8A', '\u8000', '\u8449', '\u84C9', '\u8981', '\u8B21',
                '\u8E0A', '\u9065', '\u967D', '\u990A', '\u617E', '\u6291', '\u6B32', '\uFFFD',
                '\u6C83', '\u6D74', '\u7FCC', '\u7FFC', '\u6DC0', '\u7F85', '\u87BA', '\u88F8',
                '\u6765', '\u83B1', '\u983C', '\u96F7', '\u6D1B', '\u7D61', '\u843D', '\u916A',
                '\u4E71', '\u5375', '\u5D50', '\u6B04', '\u6FEB', '\u85CD', '\u862D', '\u89A7',
                '\u5229', '\u540F', '\u5C65', '\u674E', '\u68A8', '\u7406', '\u7483', '\u75E2',
                '\u88CF', '\u88E1', '\u91CC', '\u96E2', '\u9678', '\u5F8B', '\u7387', '\u7ACB',
                '\u844E', '\u63A0', '\u7565', '\u5289', '\u6D41', '\u6E9C', '\u7409', '\u7559',
                '\u786B', '\u7C92', '\u9686', '\u7ADC', '\u9F8D', '\u4FB6', '\u616E', '\u65C5',
                '\u865C', '\u4E86', '\u4EAE', '\u50DA', '\u4E21', '\u51CC', '\u5BEE', '\u6599',
                '\u6881', '\u6DBC', '\u731F', '\u7642', '\u77AD', '\u7A1C', '\u7CE7', '\u826F',
                '\u8AD2', '\u907C', '\u91CF', '\u9675', '\u9818', '\u529B', '\u7DD1', '\u502B',
                '\u5398', '\u6797', '\u6DCB', '\u71D0', '\u7433', '\u81E8', '\u8F2A', '\u96A3',
                '\u9C57', '\u9E9F', '\u7460', '\u5841', '\u6D99', '\u7D2F', '\u985E', '\u4EE4',
                '\u4F36', '\u4F8B', '\u51B7', '\u52B1', '\u5DBA', '\u601C', '\u73B2', '\u793C',
                '\u82D3', '\u9234', '\u96B7', '\u96F6', '\u970A', '\u9E97', '\u9F62', '\u66A6',
                '\u6B74', '\u5217', '\u51A3', '\u70C8', '\u88C2', '\u5EC9', '\u604B', '\u9190',
                '\u6F23', '\u7149', '\u7C3E', '\u7DF4', '\u806F', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 98 ===
            {
                '\u84EE', '\u9023', '\u932C', '\u5442', '\u9B6F', '\u6AD3', '\u7089', '\u8CC2',
                '\u8DEF', '\u9732', '\u52B4', '\u5A41', '\u5ECA', '\u5F04', '\u6717', '\u697C',
                '\u6994', '\u6D6A', '\u6F0F', '\u7262', '\u72FC', '\u7BED', '\u8001', '\u807E',
                '\u874B', '\u90CE', '\u516D', '\u9E93', '\u7984', '\u808B', '\u9332', '\u8AD6',
                '\u502D', '\u548C', '\u8A71', '\u6B6A', '\u8CC4', '\u8107', '\u60D1', '\u67A0',
                '\u9DF2', '\u4E99', '\u4E98', '\u9C10', '\u8A6B', '\u85C1', '\u8568', '\u6900',
                '\u6E7E', '\u7897', '\u8155', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\u5F0C',
                '\u4E10', '\u4E15', '\u4E2A', '\u4E31', '\u4E36', '\u4E3C', '\u4E3F', '\u4E42',
                '\u4E56', '\u4E58', '\u4E82', '\u4E85', '\u8C6B', '\u4EBA', '\u8212', '\u5F0D',
                '\u4E8E', '\u4E9E', '\u4E9F', '\u4EA0', '\u4EA2', '\u4EB0', '\u4EB3', '\u4EB6',
                '\u4ECE', '\u4ECD', '\u4EC4', '\u4EC6', '\u4EC2', '\u4ED7', '\u4EDE', '\u4EED',
                '\u4EDF', '\u4EF7', '\u4F09', '\u4F5A', '\u4F30', '\u4F5B', '\u4F5D', '\u4F57',
                '\u4F47', '\u4F76', '\u4F88', '\u4F8F', '\u4F98', '\u4F7B', '\u4F69', '\u4F70',
                '\u4F91', '\u4F6F', '\u4F86', '\u4F96', '\u5118', '\u4FD4', '\u4FDF', '\u4FCE',
                '\u4FD8', '\u4FDB', '\u4FD1', '\u4FDA', '\u4FD0', '\u4FE4', '\u4FE5', '\u501A',
                '\u5028', '\u5014', '\u502A', '\u5025', '\u5005', '\u4F1C', '\u4FF6', '\u5021',
                '\u5029', '\u502C', '\u4FFE', '\u4FEF', '\u5011', '\u5006', '\u5043', '\u5047',
                '\u6703', '\u5055', '\u5050', '\u5048', '\u505A', '\u5056', '\u506C', '\u5078',
                '\u5080', '\u509A', '\u5085', '\u50B4', '\u50B2', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 99 ===
            {
                '\u50C9', '\u50CA', '\u50B3', '\u50C2', '\u50D6', '\u50DE', '\u50E5', '\u50ED',
                '\u50E3', '\u50EE', '\u50F9', '\u50F5', '\u5109', '\u5101', '\u5102', '\u5116',
                '\u5115', '\u5114', '\u511A', '\u5121', '\u513A', '\u5137', '\u513C', '\u513B',
                '\u513F', '\u5140', '\u5152', '\u514C', '\u5154', '\u5162', '\u7AF8', '\u5169',
                '\u516A', '\u516E', '\u5180', '\u5182', '\u56D8', '\u518C', '\u5189', '\u518F',
                '\u5191', '\u5193', '\u5195', '\u5196', '\u51A4', '\u51A6', '\u51A2', '\u51A9',
                '\u51AA', '\u51AB', '\u51B3', '\u51B1', '\u51B2', '\u51B0', '\u51B5', '\u51BD',
                '\u51C5', '\u51C9', '\u51DB', '\u51E0', '\u8655', '\u51E9', '\u51ED', '\uFFFD',
                '\u51F0', '\u51F5', '\u51FE', '\u5204', '\u520B', '\u5214', '\u520E', '\u5227',
                '\u522A', '\u522E', '\u5233', '\u5239', '\u524F', '\u5244', '\u524B', '\u524C',
                '\u525E', '\u5254', '\u526A', '\u5274', '\u5269', '\u5273', '\u527F', '\u527D',
                '\u528D', '\u5294', '\u5292', '\u5271', '\u5288', '\u5291', '\u8FA8', '\u8FA7',
                '\u52AC', '\u52AD', '\u52BC', '\u52B5', '\u52C1', '\u52CD', '\u52D7', '\u52DE',
                '\u52E3', '\u52E6', '\u98ED', '\u52E0', '\u52F3', '\u52F5', '\u52F8', '\u52F9',
                '\u5306', '\u5308', '\u7538', '\u530D', '\u5310', '\u530F', '\u5315', '\u531A',
                '\u5323', '\u532F', '\u5331', '\u5333', '\u5338', '\u5340', '\u5346', '\u5345',
                '\u4E17', '\u5349', '\u534D', '\u51D6', '\u535E', '\u5369', '\u536E', '\u5918',
                '\u537B', '\u5377', '\u5382', '\u5396', '\u53A0', '\u53A6', '\u53A5', '\u53AE',
                '\u53B0', '\u53B6', '\u53C3', '\u7C12', '\u96D9', '\u53DF', '\u66FC', '\u71EE',
                '\u53EE', '\u53E8', '\u53ED', '\u53FA', '\u5401', '\u543D', '\u5440', '\u542C',
                '\u542D', '\u543C', '\u542E', '\u5436', '\u5429', '\u541D', '\u544E', '\u548F',
                '\u5475', '\u548E', '\u545F', '\u5471', '\u5477', '\u5470', '\u5492', '\u547B',
                '\u5480', '\u5476', '\u5484', '\u5490', '\u5486', '\u54C7', '\u54A2', '\u54B8',
                '\u54A5', '\u54AC', '\u54C4', '\u54C8', '\u54A8', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 9A ===
            {
                '\u54AB', '\u54C2', '\u54A4', '\u54BE', '\u54BC', '\u54D8', '\u54E5', '\u54E6',
                '\u550F', '\u5514', '\u54FD', '\u54EE', '\u54ED', '\u54FA', '\u54E2', '\u5539',
                '\u5540', '\u5563', '\u554C', '\u552E', '\u555C', '\u5545', '\u5556', '\u5557',
                '\u5538', '\u5533', '\u555D', '\u5599', '\u5580', '\u54AF', '\u558A', '\u559F',
                '\u557B', '\u557E', '\u5598', '\u559E', '\u55AE', '\u557C', '\u5583', '\u55A9',
                '\u5587', '\u55A8', '\u55DA', '\u55C5', '\u55DF', '\u55C4', '\u55DC', '\u55E4',
                '\u55D4', '\u5614', '\u55F7', '\u5616', '\u55FE', '\u55FD', '\u561B', '\u55F9',
                '\u564E', '\u5650', '\u71DF', '\u5634', '\u5636', '\u5632', '\u5638', '\uFFFD',
                '\u566B', '\u5664', '\u562F', '\u566C', '\u566A', '\u5686', '\u5680', '\u568A',
                '\u56A0', '\u5694', '\u568F', '\u56A5', '\u56AE', '\u56B6', '\u56B4', '\u56C2',
                '\u56BC', '\u56C1', '\u56C3', '\u56C0', '\u56C8', '\u56CE', '\u56D1', '\u56D3',
                '\u56D7', '\u56EE', '\u56F9', '\u5700', '\u56FF', '\u5704', '\u5709', '\u5708',
                '\u570B', '\u570D', '\u5713', '\u5718', '\u5716', '\u55C7', '\u571C', '\u5726',
                '\u5737', '\u5738', '\u574E', '\u573B', '\u5740', '\u574F', '\u5769', '\u57C0',
                '\u5788', '\u5761', '\u577F', '\u5789', '\u5793', '\u57A0', '\u57B3', '\u57A4',
                '\u57AA', '\u57B0', '\u57C3', '\u57C6', '\u57D4', '\u57D2', '\u57D3', '\u580A',
                '\u57D6', '\u57E3', '\u580B', '\u5819', '\u581D', '\u5872', '\u5821', '\u5862',
                '\u584B', '\u5870', '\u6BC0', '\u5852', '\u583D', '\u5879', '\u5885', '\u58B9',
                '\u589F', '\u58AB', '\u58BA', '\u58DE', '\u58BB', '\u58B8', '\u58AE', '\u58C5',
                '\u58D3', '\u58D1', '\u58D7', '\u58D9', '\u58D8', '\u58E5', '\u58DC', '\u58E4',
                '\u58DF', '\u58EF', '\u58FA', '\u58F9', '\u58FB', '\u58FC', '\u58FD', '\u5902',
                '\u590A', '\u5910', '\u591B', '\u68A6', '\u5925', '\u592C', '\u592D', '\u5932',
                '\u5938', '\u593E', '\u7AD2', '\u5955', '\u5950', '\u594E', '\u595A', '\u5958',
                '\u5962', '\u5960', '\u5967', '\u596C', '\u5969', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 9B ===
            {
                '\u5978', '\u5981', '\u599D', '\u4F5E', '\u4FAB', '\u59A3', '\u59B2', '\u59C6',
                '\u59E8', '\u59DC', '\u598D', '\u59D9', '\u59DA', '\u5A25', '\u5A1F', '\u5A11',
                '\u5A1C', '\u5A09', '\u5A1A', '\u5A40', '\u5A6C', '\u5A49', '\u5A35', '\u5A36',
                '\u5A62', '\u5A6A', '\u5A9A', '\u5ABC', '\u5ABE', '\u5ACB', '\u5AC2', '\u5ABD',
                '\u5AE3', '\u5AD7', '\u5AE6', '\u5AE9', '\u5AD6', '\u5AFA', '\u5AFB', '\u5B0C',
                '\u5B0B', '\u5B16', '\u5B32', '\u5AD0', '\u5B2A', '\u5B36', '\u5B3E', '\u5B43',
                '\u5B45', '\u5B40', '\u5B51', '\u5B55', '\u5B5A', '\u5B5B', '\u5B65', '\u5B69',
                '\u5B70', '\u5B73', '\u5B75', '\u5B78', '\u6588', '\u5B7A', '\u5B80', '\uFFFD',
                '\u5B83', '\u5BA6', '\u5BB8', '\u5BC3', '\u5BC7', '\u5BC9', '\u5BD4', '\u5BD0',
                '\u5BE4', '\u5BE6', '\u5BE2', '\u5BDE', '\u5BE5', '\u5BEB', '\u5BF0', '\u5BF6',
                '\u5BF3', '\u5C05', '\u5C07', '\u5C08', '\u5C0D', '\u5C13', '\u5C20', '\u5C22',
                '\u5C28', '\u5C38', '\u5C39', '\u5C41', '\u5C46', '\u5C4E', '\u5C53', '\u5C50',
                '\u5C4F', '\u5B71', '\u5C6C', '\u5C6E', '\u4E62', '\u5C76', '\u5C79', '\u5C8C',
                '\u5C91', '\u5C94', '\u599B', '\u5CAB', '\u5CBB', '\u5CB6', '\u5CBC', '\u5CB7',
                '\u5CC5', '\u5CBE', '\u5CC7', '\u5CD9', '\u5CE9', '\u5CFD', '\u5CFA', '\u5CED',
                '\u5D8C', '\u5CEA', '\u5D0B', '\u5D15', '\u5D17', '\u5D5C', '\u5D1F', '\u5D1B',
                '\u5D11', '\u5D14', '\u5D22', '\u5D1A', '\u5D19', '\u5D18', '\u5D4C', '\u5D52',
                '\u5D4E', '\u5D4B', '\u5D6C', '\u5D73', '\u5D76', '\u5D87', '\u5D84', '\u5D82',
                '\u5DA2', '\u5D9D', '\u5DAC', '\u5DAE', '\u5DBD', '\u5D90', '\u5DB7', '\u5DBC',
                '\u5DC9', '\u5DCD', '\u5DD3', '\u5DD2', '\u5DD6', '\u5DDB', '\u5DEB', '\u5DF2',
                '\u5DF5', '\u5E0B', '\u5E1A', '\u5E19', '\u5E11', '\u5E1B', '\u5E36', '\u5E37',
                '\u5E44', '\u5E43', '\u5E40', '\u5E4E', '\u5E57', '\u5E54', '\u5E5F', '\u5E62',
                '\u5E64', '\u5E47', '\u5E75', '\u5E76', '\u5E7A', '\u9EBC', '\u5E7F', '\u5EA0',
                '\u5EC1', '\u5EC2', '\u5EC8', '\u5ED0', '\u5ECF', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 9C ===
            {
                '\u5ED6', '\u5EE3', '\u5EDD', '\u5EDA', '\u5EDB', '\u5EE2', '\u5EE1', '\u5EE8',
                '\u5EE9', '\u5EEC', '\u5EF1', '\u5EF3', '\u5EF0', '\u5EF4', '\u5EF8', '\u5EFE',
                '\u5F03', '\u5F09', '\u5F5D', '\u5F5C', '\u5F0B', '\u5F11', '\u5F16', '\u5F29',
                '\u5F2D', '\u5F38', '\u5F41', '\u5F48', '\u5F4C', '\u5F4E', '\u5F2F', '\u5F51',
                '\u5F56', '\u5F57', '\u5F59', '\u5F61', '\u5F6D', '\u5F73', '\u5F77', '\u5F83',
                '\u5F82', '\u5F7F', '\u5F8A', '\u5F88', '\u5F91', '\u5F87', '\u5F9E', '\u5F99',
                '\u5F98', '\u5FA0', '\u5FA8', '\u5FAD', '\u5FBC', '\u5FD6', '\u5FFB', '\u5FE4',
                '\u5FF8', '\u5FF1', '\u5FDD', '\u60B3', '\u5FFF', '\u6021', '\u6060', '\uFFFD',
                '\u6019', '\u6010', '\u6029', '\u600E', '\u6031', '\u601B', '\u6015', '\u602B',
                '\u6026', '\u600F', '\u603A', '\u605A', '\u6041', '\u606A', '\u6077', '\u605F',
                '\u604A', '\u6046', '\u604D', '\u6063', '\u6043', '\u6064', '\u6042', '\u606C',
                '\u606B', '\u6059', '\u6081', '\u608D', '\u60E7', '\u6083', '\u609A', '\u6084',
                '\u609B', '\u6096', '\u6097', '\u6092', '\u60A7', '\u608B', '\u60E1', '\u60B8',
                '\u60E0', '\u60D3', '\u60B4', '\u5FF0', '\u60BD', '\u60C6', '\u60B5', '\u60D8',
                '\u614D', '\u6115', '\u6106', '\u60F6', '\u60F7', '\u6100', '\u60F4', '\u60FA',
                '\u6103', '\u6121', '\u60FB', '\u60F1', '\u610D', '\u610E', '\u6147', '\u613E',
                '\u6128', '\u6127', '\u614A', '\u613F', '\u613C', '\u612C', '\u6134', '\u613D',
                '\u6142', '\u6144', '\u6173', '\u6177', '\u6158', '\u6159', '\u615A', '\u616B',
                '\u6174', '\u616F', '\u6165', '\u6171', '\u615F', '\u615D', '\u6153', '\u6175',
                '\u6199', '\u6196', '\u6187', '\u61AC', '\u6194', '\u619A', '\u618A', '\u6191',
                '\u61AB', '\u61AE', '\u61CC', '\u61CA', '\u61C9', '\u61F7', '\u61C8', '\u61C3',
                '\u61C6', '\u61BA', '\u61CB', '\u7F79', '\u61CD', '\u61E6', '\u61E3', '\u61F6',
                '\u61FA', '\u61F4', '\u61FF', '\u61FD', '\u61FC', '\u61FE', '\u6200', '\u6208',
                '\u6209', '\u620D', '\u620C', '\u6214', '\u621B', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 9D ===
            {
                '\u621E', '\u6221', '\u622A', '\u622E', '\u6230', '\u6232', '\u6233', '\u6241',
                '\u624E', '\u625E', '\u6263', '\u625B', '\u6260', '\u6268', '\u627C', '\u6282',
                '\u6289', '\u627E', '\u6292', '\u6293', '\u6296', '\u62D4', '\u6283', '\u6294',
                '\u62D7', '\u62D1', '\u62BB', '\u62CF', '\u62FF', '\u62C6', '\u64D4', '\u62C8',
                '\u62DC', '\u62CC', '\u62CA', '\u62C2', '\u62C7', '\u629B', '\u62C9', '\u630C',
                '\u62EE', '\u62F1', '\u6327', '\u6302', '\u6308', '\u62EF', '\u62F5', '\u6350',
                '\u633E', '\u634D', '\u641C', '\u634F', '\u6396', '\u638E', '\u6380', '\u63AB',
                '\u6376', '\u63A3', '\u638F', '\u6389', '\u639F', '\u63B5', '\u636B', '\uFFFD',
                '\u6369', '\u63BE', '\u63E9', '\u63C0', '\u63C6', '\u63E3', '\u63C9', '\u63D2',
                '\u63F6', '\u63C4', '\u6416', '\u6434', '\u6406', '\u6413', '\u6426', '\u6436',
                '\u651D', '\u6417', '\u6428', '\u640F', '\u6467', '\u646F', '\u6476', '\u644E',
                '\u652A', '\u6495', '\u6493', '\u64A5', '\u64A9', '\u6488', '\u64BC', '\u64DA',
                '\u64D2', '\u64C5', '\u64C7', '\u64BB', '\u64D8', '\u64C2', '\u64F1', '\u64E7',
                '\u8209', '\u64E0', '\u64E1', '\u62AC', '\u64E3', '\u64EF', '\u652C', '\u64F6',
                '\u64F4', '\u64F2', '\u64FA', '\u6500', '\u64FD', '\u6518', '\u651C', '\u6505',
                '\u6524', '\u6523', '\u652B', '\u6534', '\u6535', '\u6537', '\u6536', '\u6538',
                '\u754B', '\u6548', '\u6556', '\u6555', '\u654D', '\u6558', '\u655E', '\u655D',
                '\u6572', '\u6578', '\u6582', '\u6583', '\u8B8A', '\u659B', '\u659F', '\u65AB',
                '\u65B7', '\u65C3', '\u65C6', '\u65C1', '\u65C4', '\u65CC', '\u65D2', '\u65DB',
                '\u65D9', '\u65E0', '\u65E1', '\u65F1', '\u6772', '\u660A', '\u6603', '\u65FB',
                '\u6773', '\u6635', '\u6636', '\u6634', '\u661C', '\u664F', '\u6644', '\u6649',
                '\u6641', '\u665E', '\u665D', '\u6664', '\u6667', '\u6668', '\u665F', '\u6662',
                '\u6670', '\u6683', '\u6688', '\u668E', '\u6689', '\u6684', '\u6698', '\u669D',
                '\u66C1', '\u66B9', '\u66C9', '\u66BE', '\u66BC', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 9E ===
            {
                '\u66C4', '\u66B8', '\u66D6', '\u66DA', '\u66E0', '\u663F', '\u66E6', '\u66E9',
                '\u66F0', '\u66F5', '\u66F7', '\u670F', '\u6716', '\u671E', '\u6726', '\u6727',
                '\u9738', '\u672E', '\u673F', '\u6736', '\u6741', '\u6738', '\u6737', '\u6746',
                '\u675E', '\u6760', '\u6759', '\u6763', '\u6764', '\u6789', '\u6770', '\u67A9',
                '\u677C', '\u676A', '\u678C', '\u678B', '\u67A6', '\u67A1', '\u6785', '\u67B7',
                '\u67EF', '\u67B4', '\u67EC', '\u67B3', '\u67E9', '\u67B8', '\u67E4', '\u67DE',
                '\u67DD', '\u67E2', '\u67EE', '\u67B9', '\u67CE', '\u67C6', '\u67E7', '\u6A9C',
                '\u681E', '\u6846', '\u6829', '\u6840', '\u684D', '\u6832', '\u684E', '\uFFFD',
                '\u68B3', '\u682B', '\u6859', '\u6863', '\u6877', '\u687F', '\u689F', '\u688F',
                '\u68AD', '\u6894', '\u689D', '\u689B', '\u6883', '\u6AAE', '\u68B9', '\u6874',
                '\u68B5', '\u68A0', '\u68BA', '\u690F', '\u688D', '\u687E', '\u6901', '\u68CA',
                '\u6908', '\u68D8', '\u6922', '\u6926', '\u68E1', '\u690C', '\u68CD', '\u68D4',
                '\u68E7', '\u68D5', '\u6936', '\u6912', '\u6904', '\u68D7', '\u68E3', '\u6925',
                '\u68F9', '\u68E0', '\u68EF', '\u6928', '\u692A', '\u691A', '\u6923', '\u6921',
                '\u68C6', '\u6979', '\u6977', '\u695D', '\u6978', '\u696B', '\u6954', '\u697E',
                '\u696E', '\u6939', '\u6974', '\u693D', '\u6959', '\u6930', '\u6961', '\u695E',
                '\u695D', '\u6981', '\u696A', '\u69B2', '\u69AE', '\u69D0', '\u69BF', '\u69C1',
                '\u69D3', '\u69BE', '\u69CE', '\u5BE8', '\u69CA', '\u69DD', '\u69BB', '\u69C3',
                '\u69A7', '\u6A2E', '\u6991', '\u69A0', '\u699C', '\u6995', '\u69B4', '\u69DE',
                '\u69E8', '\u6A02', '\u6A1B', '\u69FF', '\u6B0A', '\u69F9', '\u69F2', '\u69E7',
                '\u6A05', '\u69B1', '\u6A1E', '\u69ED', '\u6A14', '\u69EB', '\u6A0A', '\u6A12',
                '\u6AC1', '\u6A23', '\u6A13', '\u6A44', '\u6A0C', '\u6A72', '\u6A36', '\u6A78',
                '\u6A47', '\u6A62', '\u6A59', '\u6A66', '\u6A48', '\u6A38', '\u6A22', '\u6A90',
                '\u6A8D', '\u6AA0', '\u6A84', '\u6AA2', '\u6AA3', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE 9F ===
            {
                '\u6A97', '\u8617', '\u6ABB', '\u6AC3', '\u6AC2', '\u6AB8', '\u6AB3', '\u6AAC',
                '\u6ADE', '\u6AD1', '\u6ADF', '\u6AAA', '\u6ADA', '\u6AEA', '\u6AFB', '\u6B05',
                '\u8616', '\u6AFA', '\u6B12', '\u6B16', '\u9B31', '\u6B1F', '\u6B38', '\u6B37',
                '\u76DC', '\u6B39', '\u98EE', '\u6B47', '\u6B43', '\u6B49', '\u6B50', '\u6B59',
                '\u6B54', '\u6B5B', '\u6B5F', '\u6B61', '\u6B78', '\u6B79', '\u6B7F', '\u6B80',
                '\u6B84', '\u6B83', '\u6B8D', '\u6B98', '\u6B95', '\u6B9E', '\u6BA4', '\u6BAA',
                '\u6BAB', '\u6BAF', '\u6BB2', '\u6BB1', '\u6BB3', '\u6BB7', '\u6BBC', '\u6BC6',
                '\u6BCB', '\u6BD3', '\u6BDF', '\u6BEC', '\u6BEB', '\u6BF3', '\u6BEF', '\uFFFD',
                '\u9EBE', '\u6C08', '\u6C13', '\u6C14', '\u6C1B', '\u6C24', '\u6C23', '\u6C5E',
                '\u6C55', '\u6C62', '\u6C6A', '\u6C82', '\u6C8D', '\u6C9A', '\u6C81', '\u6C9B',
                '\u6C7E', '\u6C68', '\u6C73', '\u6C92', '\u6C90', '\u6CC4', '\u6CF1', '\u6CD3',
                '\u6CBD', '\u6CD7', '\u6CC5', '\u6CDD', '\u6CAE', '\u6CB1', '\u6CBE', '\u6CBA',
                '\u6CDB', '\u6CEF', '\u6CD9', '\u6CEA', '\u6D1F', '\u884D', '\u6D36', '\u6D2B',
                '\u6D3D', '\u6D38', '\u6D19', '\u6D35', '\u6D33', '\u6D12', '\u6D0C', '\u6D63',
                '\u6D93', '\u6D64', '\u6D5A', '\u6D79', '\u6D59', '\u6D8E', '\u6D95', '\u6FE4',
                '\u6D85', '\u6DF9', '\u6E15', '\u6E0A', '\u6DB5', '\u6DC7', '\u6DE6', '\u6DB8',
                '\u6DC6', '\u6DEC', '\u6DDE', '\u6DCC', '\u6DE8', '\u6DD2', '\u6DC5', '\u6DFA',
                '\u6DD9', '\u6DE4', '\u6DD5', '\u6DEA', '\u6DEE', '\u6E2D', '\u6E6E', '\u6E2E',
                '\u6E19', '\u6E72', '\u6E5F', '\u6E3E', '\u6E23', '\u6E6B', '\u6E2B', '\u6E76',
                '\u6E4D', '\u6E1F', '\u6E43', '\u6E3A', '\u6E4E', '\u6E24', '\u6EFF', '\u6E1D',
                '\u6E38', '\u6E82', '\u6EAA', '\u6E98', '\u6EC9', '\u6EB7', '\u6ED3', '\u6EBD',
                '\u6EAF', '\u6EC4', '\u6EB2', '\u6ED4', '\u6ED5', '\u6E8F', '\u6EA5', '\u6EC2',
                '\u6E9F', '\u6F41', '\u6F11', '\u704C', '\u6EEC', '\u6EF8', '\u6EFE', '\u6F3F',
                '\u6EF2', '\u6F31', '\u6EEF', '\u6F32', '\u6ECC', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E0 ===
            {
                '\u6F3E', '\u6F13', '\u6EF7', '\u6F86', '\u6F7A', '\u6F78', '\u6F81', '\u6F80',
                '\u6F6F', '\u6F5B', '\u6FF3', '\u6F6D', '\u6F82', '\u6F7C', '\u6F58', '\u6F8E',
                '\u6F91', '\u6FC2', '\u6F66', '\u6FB3', '\u6FA3', '\u6FA1', '\u6FA4', '\u6FB9',
                '\u6FC6', '\u6FAA', '\u6FDF', '\u6FD5', '\u6FEC', '\u6FD4', '\u6FD8', '\u6FF1',
                '\u6FEE', '\u6FDB', '\u7009', '\u700B', '\u6FFA', '\u7011', '\u7001', '\u700F',
                '\u6FFE', '\u701B', '\u701A', '\u6F74', '\u701D', '\u7018', '\u701F', '\u7030',
                '\u703E', '\u7032', '\u7051', '\u7063', '\u7099', '\u7092', '\u70AF', '\u70F1',
                '\u70AC', '\u70B8', '\u70B3', '\u70AE', '\u70DF', '\u70CB', '\u70DD', '\uFFFD',
                '\u70D9', '\u7109', '\u70FD', '\u711C', '\u7119', '\u7165', '\u7155', '\u7188',
                '\u7166', '\u7162', '\u714C', '\u7156', '\u716C', '\u718F', '\u71FB', '\u7184',
                '\u7195', '\u71A8', '\u71AC', '\u71D7', '\u71B9', '\u71BE', '\u71D2', '\u71C9',
                '\u71D4', '\u71CE', '\u71E0', '\u71EC', '\u71E7', '\u71F5', '\u71FC', '\u71F9',
                '\u71FF', '\u720D', '\u7210', '\u721B', '\u7228', '\u722D', '\u722C', '\u7230',
                '\u7232', '\u723B', '\u723C', '\u723F', '\u7240', '\u7246', '\u724B', '\u7258',
                '\u7274', '\u727E', '\u7282', '\u7281', '\u7287', '\u7292', '\u7296', '\u72A2',
                '\u72A7', '\u72B9', '\u72B2', '\u72C3', '\u72C6', '\u72C4', '\u72CE', '\u72D2',
                '\u72E2', '\u72E0', '\u72E1', '\u72F9', '\u72F7', '\u500F', '\u7317', '\u730A',
                '\u731C', '\u7316', '\u731D', '\u7334', '\u732F', '\u7329', '\u7325', '\u733E',
                '\u734E', '\u734F', '\u9ED8', '\u7357', '\u736A', '\u7368', '\u7370', '\u7378',
                '\u7375', '\u737B', '\u737A', '\u73C8', '\u73B3', '\u73CE', '\u73BB', '\u73C0',
                '\u73E5', '\u73EE', '\u73DE', '\u74A2', '\u7405', '\u746F', '\u7425', '\u73F8',
                '\u7432', '\u743A', '\u7455', '\u743F', '\u745F', '\u7459', '\u7441', '\u745C',
                '\u7469', '\u7470', '\u7463', '\u746A', '\u7476', '\u747E', '\u748B', '\u749E',
                '\u74A7', '\u74CA', '\u74CF', '\u74D4', '\u73F1', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E1 ===
            {
                '\u74E0', '\u74E3', '\u74E7', '\u74E9', '\u74EE', '\u74F2', '\u74F0', '\u74F1',
                '\u74F8', '\u74F7', '\u7504', '\u7503', '\u7505', '\u750C', '\u750E', '\u750D',
                '\u7515', '\u7513', '\u751E', '\u7526', '\u752C', '\u753C', '\u7544', '\u754D',
                '\u754A', '\u7549', '\u755B', '\u7546', '\u755A', '\u7569', '\u7564', '\u7567',
                '\u756B', '\u756D', '\u7578', '\u7576', '\u7586', '\u7587', '\u7574', '\u758A',
                '\u7589', '\u7582', '\u7594', '\u759A', '\u759D', '\u75A5', '\u75A3', '\u75C2',
                '\u75B3', '\u75C3', '\u75B5', '\u75BD', '\u75B8', '\u75BC', '\u75B1', '\u75CD',
                '\u75CA', '\u75D2', '\u75D9', '\u75E3', '\u75DE', '\u75FE', '\u75FF', '\uFFFD',
                '\u75FC', '\u7601', '\u75F0', '\u75FA', '\u75F2', '\u75F3', '\u760B', '\u760D',
                '\u7609', '\u761F', '\u7627', '\u7620', '\u7621', '\u7622', '\u7624', '\u7634',
                '\u7630', '\u763B', '\u7647', '\u7648', '\u7646', '\u765C', '\u7658', '\u7661',
                '\u7662', '\u7668', '\u7669', '\u766A', '\u7667', '\u766C', '\u7670', '\u7672',
                '\u7676', '\u7678', '\u767C', '\u7680', '\u7683', '\u7688', '\u768B', '\u768E',
                '\u7696', '\u7693', '\u7699', '\u769A', '\u76B0', '\u76B4', '\u76B8', '\u76B9',
                '\u76BA', '\u76C2', '\u76CD', '\u76D6', '\u76D2', '\u76DE', '\u76E1', '\u76E5',
                '\u76E7', '\u76EA', '\u862F', '\u76FB', '\u7708', '\u7707', '\u7704', '\u7729',
                '\u7724', '\u771E', '\u7725', '\u7726', '\u771B', '\u7737', '\u7738', '\u7747',
                '\u775A', '\u7768', '\u776B', '\u775B', '\u7765', '\u777F', '\u777E', '\u7779',
                '\u778E', '\u778B', '\u7791', '\u77A0', '\u779E', '\u77B0', '\u77B6', '\u77B9',
                '\u77BF', '\u77BC', '\u77BD', '\u77BB', '\u77C7', '\u77CD', '\u77D7', '\u77DA',
                '\u77DC', '\u77E3', '\u77EE', '\u77FC', '\u780C', '\u7812', '\u7926', '\u7820',
                '\u792A', '\u7845', '\u788E', '\u7874', '\u7886', '\u787C', '\u789A', '\u788C',
                '\u78A3', '\u78B5', '\u78AA', '\u77AF', '\u78D1', '\u78C6', '\u78CB', '\u78D4',
                '\u78BE', '\u78BC', '\u78C5', '\u78CA', '\u78EC', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E2 ===
            {
                '\u78E7', '\u78DA', '\u78FD', '\u78F4', '\u7907', '\u7912', '\u7911', '\u7919',
                '\u792C', '\u792B', '\u7940', '\u7960', '\u7957', '\u795F', '\u795A', '\u7955',
                '\u7953', '\u797A', '\u797F', '\u798A', '\u799D', '\u79A7', '\u9F4B', '\u79AA',
                '\u79AE', '\u79B3', '\u79B9', '\u79BA', '\u79C9', '\u79D5', '\u79E7', '\u79EC',
                '\u79E1', '\u79E3', '\u7A08', '\u7A0D', '\u7A18', '\u7A19', '\u7A20', '\u7A1F',
                '\u7980', '\u7A31', '\u7A3B', '\u7A3E', '\u7A37', '\u7A43', '\u7A57', '\u7A49',
                '\u7A61', '\u7A62', '\u7A69', '\u9F9D', '\u7A70', '\u7A79', '\u7A7D', '\u7A88',
                '\u7A97', '\u7A95', '\u7A98', '\u7A96', '\u7AA9', '\u7AC8', '\u7AB0', '\uFFFD',
                '\u7AB6', '\u7AC5', '\u7AC4', '\u7ABF', '\u9083', '\u7AC7', '\u7ACA', '\u7ACD',
                '\u7ACF', '\u7AD5', '\u7AD3', '\u7AD9', '\u7ADA', '\u7ADD', '\u7AE1', '\u7AE2',
                '\u7AE6', '\u7AED', '\u7AF0', '\u7B02', '\u7B0F', '\u7B0A', '\u7B06', '\u7B33',
                '\u7B18', '\u7B19', '\u7B1E', '\u7B35', '\u7B28', '\u7B36', '\u7B50', '\u7B7A',
                '\u7B04', '\u7B4D', '\u7B0B', '\u7B4C', '\u7B45', '\u7B75', '\u7B65', '\u7B74',
                '\u7B67', '\u7B70', '\u7B71', '\u7B6C', '\u7B6E', '\u7B9D', '\u7B98', '\u7B9F',
                '\u7B8D', '\u7B9C', '\u7B9A', '\u7B8B', '\u7B92', '\u7B8F', '\u7B5D', '\u7B99',
                '\u7BCB', '\u7BC1', '\u7BCC', '\u7BCF', '\u7BB4', '\u7BC6', '\u7BDD', '\u7BE9',
                '\u7C11', '\u7C14', '\u7BE6', '\u7BE8', '\u7C60', '\u7C00', '\u7C07', '\u7C13',
                '\u7BF3', '\u7BF7', '\u7C17', '\u7C0D', '\u7BF6', '\u7C26', '\u7C27', '\u7C2A',
                '\u7C1F', '\u7C37', '\u7C2B', '\u7C3D', '\u7C4C', '\u7C43', '\u7C54', '\u7C4F',
                '\u7C40', '\u7C50', '\u7C58', '\u7C5F', '\u7C64', '\u7C56', '\u7C65', '\u7C6C',
                '\u7C75', '\u7C83', '\u7C90', '\u7CA4', '\u7CAD', '\u7CA2', '\u7CAB', '\u7CA1',
                '\u7CA8', '\u7CB3', '\u7CB2', '\u7CB1', '\u7CAE', '\u7CB9', '\u7CBD', '\u7CC0',
                '\u7CC5', '\u7CC2', '\u7CD8', '\u7CD2', '\u7CDC', '\u7CE2', '\u9B3B', '\u7CEF',
                '\u7CF2', '\u7CF4', '\u7CF6', '\u7CFA', '\u7D06', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E3 ===
            {
                '\u7D02', '\u7D1C', '\u7D15', '\u7D0A', '\u7D45', '\u7D4B', '\u7D2E', '\u7D32',
                '\u7D3F', '\u7D35', '\u7D46', '\u7D73', '\u7D56', '\u7D4E', '\u7D72', '\u7D68',
                '\u7D6E', '\u7D4F', '\u7D63', '\u7D93', '\u7D89', '\u7D5B', '\u7D8F', '\u7D7D',
                '\u7D9B', '\u7DBA', '\u7DAE', '\u7DA3', '\u7DB5', '\u7DC7', '\u7DBD', '\u7DAB',
                '\u7E3D', '\u7DA2', '\u7DAF', '\u7DDC', '\u7DB8', '\u7D9F', '\u7DB0', '\u7DD8',
                '\u7DDD', '\u7DE4', '\u7DDE', '\u7DFB', '\u7DF2', '\u7DE1', '\u7E05', '\u7E0A',
                '\u7E23', '\u7E21', '\u7E12', '\u7E31', '\u7E1F', '\u7E09', '\u7E0B', '\u7E22',
                '\u7E46', '\u7E66', '\u7E3B', '\u7E35', '\u7E39', '\u7E43', '\u7E37', '\uFFFD',
                '\u7E32', '\u7E3A', '\u7E67', '\u7E5D', '\u7E56', '\u7E5E', '\u7E59', '\u7E5A',
                '\u7E79', '\u7E6A', '\u7E69', '\u7E7C', '\u7E7B', '\u7E83', '\u7DD5', '\u7E7D',
                '\u8FAE', '\u7E7F', '\u7E88', '\u7E89', '\u7E8C', '\u7E92', '\u7E90', '\u7E93',
                '\u7E94', '\u7E96', '\u7E8E', '\u7E9B', '\u7E9C', '\u7F38', '\u7F3A', '\u7F45',
                '\u7F4C', '\u7F4D', '\u7F4E', '\u7F50', '\u7F51', '\u7F55', '\u7F54', '\u7F58',
                '\u7F5F', '\u7F60', '\u7F68', '\u7F69', '\u7F67', '\u7F78', '\u7F82', '\u7F86',
                '\u7F83', '\u7F88', '\u7F87', '\u7F8C', '\u7F94', '\u7F9E', '\u7F9D', '\u7F9A',
                '\u7FA3', '\u7FAF', '\u7FB2', '\u7FB9', '\u7FAE', '\u7FB6', '\u7FB8', '\u8B71',
                '\u7FC5', '\u7FF6', '\u7FCA', '\u7FD5', '\u7FD4', '\u7FE1', '\u7FE6', '\u7FE9',
                '\u7FF3', '\u7FF9', '\u98DC', '\u8006', '\u8004', '\u800B', '\u8012', '\u8018',
                '\u8019', '\u801C', '\u8021', '\u8028', '\u803F', '\u803B', '\u804A', '\u8046',
                '\u8052', '\u8058', '\u805A', '\u805F', '\u8062', '\u8068', '\u8073', '\u8072',
                '\u8070', '\u8076', '\u8079', '\u807D', '\u807F', '\u8084', '\u8086', '\u8085',
                '\u809B', '\u8093', '\u809A', '\u80AD', '\u5190', '\u80AC', '\u80DB', '\u80E5',
                '\u80D9', '\u80DD', '\u80C4', '\u80DA', '\u80D6', '\u8109', '\u80EF', '\u80F1',
                '\u811B', '\u8129', '\u8123', '\u812F', '\u814B', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E4 ===
            {
                '\u968B', '\u8146', '\u813E', '\u8153', '\u8151', '\u81FC', '\u8171', '\u816E',
                '\u8165', '\u8166', '\u8174', '\u8183', '\u8188', '\u818A', '\u8180', '\u8182',
                '\u81A0', '\u8195', '\u81A4', '\u81A3', '\u815F', '\u8193', '\u81A9', '\u81B0',
                '\u81B5', '\u81BE', '\u81B8', '\u81BD', '\u81C0', '\u81C2', '\u81BA', '\u81C9',
                '\u81CD', '\u81D1', '\u81D9', '\u81D8', '\u81C8', '\u81DA', '\u81DF', '\u81E0',
                '\u81E7', '\u81FA', '\u81FB', '\u81FE', '\u8201', '\u8202', '\u8205', '\u8207',
                '\u820A', '\u820D', '\u8210', '\u8216', '\u8229', '\u822B', '\u8238', '\u8233',
                '\u8240', '\u8259', '\u8258', '\u825D', '\u825A', '\u825F', '\u8264', '\uFFFD',
                '\u8262', '\u8268', '\u826A', '\u826B', '\u822E', '\u8271', '\u8277', '\u8278',
                '\u827E', '\u828D', '\u8292', '\u82AB', '\u829F', '\u82BB', '\u82AC', '\u82E1',
                '\u82E3', '\u82DF', '\u82D2', '\u82F4', '\u82F3', '\u82FA', '\u8393', '\u8303',
                '\u82FB', '\u82F9', '\u82DE', '\u8306', '\u82DC', '\u8309', '\u82D9', '\u8335',
                '\u8334', '\u8316', '\u8332', '\u8331', '\u8340', '\u8339', '\u8350', '\u8345',
                '\u832F', '\u832B', '\u8317', '\u8318', '\u8385', '\u839A', '\u83AA', '\u839F',
                '\u83A2', '\u8396', '\u8323', '\u838E', '\u8387', '\u838A', '\u837C', '\u83B5',
                '\u8373', '\u8375', '\u83A0', '\u8389', '\u83A8', '\u83F4', '\u8413', '\u83EB',
                '\u83CE', '\u83FD', '\u8403', '\u83D8', '\u840B', '\u83C1', '\u83F7', '\u8407',
                '\u83E0', '\u83F2', '\u840D', '\u8422', '\u8420', '\u83BD', '\u8438', '\u8506',
                '\u83FB', '\u846D', '\u842A', '\u843C', '\u845A', '\u8484', '\u8477', '\u846B',
                '\u84AD', '\u846E', '\u8482', '\u8469', '\u8446', '\u842C', '\u846F', '\u8479',
                '\u8435', '\u84CA', '\u8462', '\u84B9', '\u84BF', '\u849F', '\u84D9', '\u84CD',
                '\u84BB', '\u84DA', '\u84D0', '\u84C1', '\u84C6', '\u84D6', '\u84A1', '\u8521',
                '\u84FF', '\u84F4', '\u8517', '\u8518', '\u852C', '\u851F', '\u8515', '\u8514',
                '\u84FC', '\u8540', '\u8563', '\u8558', '\u8548', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E5 ===
            {
                '\u8541', '\u8602', '\u854B', '\u8555', '\u8580', '\u82A4', '\u8588', '\u8591',
                '\u858A', '\u85A8', '\u856D', '\u8594', '\u859B', '\u85EA', '\u8587', '\u859C',
                '\u8577', '\u857E', '\u8590', '\u85C9', '\u85BA', '\u85CF', '\u85B9', '\u85D0',
                '\u85D5', '\u85DD', '\u85E5', '\u85DC', '\u85F9', '\u860A', '\u8613', '\u860B',
                '\u85FE', '\u85FA', '\u8606', '\u8622', '\u861A', '\u8630', '\u863F', '\u864D',
                '\u4E55', '\u8654', '\u865F', '\u8667', '\u8671', '\u8693', '\u86A3', '\u86A9',
                '\u86AA', '\u868B', '\u868C', '\u86B6', '\u86AF', '\u86C4', '\u86C6', '\u86B0',
                '\u86C9', '\u8823', '\u86AB', '\u86D4', '\u86DE', '\u86E9', '\u86EC', '\uFFFD',
                '\u86DF', '\u86DB', '\u86EF', '\u8712', '\u8706', '\u8708', '\u8700', '\u8703',
                '\u86FB', '\u8711', '\u8709', '\u870D', '\u86F9', '\u870A', '\u8734', '\u873F',
                '\u8737', '\u873B', '\u8725', '\u8729', '\u871A', '\u8760', '\u875F', '\u8778',
                '\u874C', '\u874E', '\u8774', '\u8757', '\u8768', '\u876E', '\u8759', '\u8753',
                '\u8763', '\u876A', '\u8805', '\u87A2', '\u879F', '\u8782', '\u87AF', '\u87CB',
                '\u87BD', '\u87C0', '\u87D0', '\u96D9', '\u87AB', '\u87C4', '\u87B3', '\u87C7',
                '\u87C6', '\u87BB', '\u87EF', '\u87F2', '\u87E0', '\u880F', '\u880D', '\u87FE',
                '\u87F6', '\u87F7', '\u880E', '\u87D2', '\u8811', '\u8816', '\u8815', '\u8822',
                '\u8821', '\u8831', '\u8836', '\u8839', '\u8827', '\u883B', '\u8844', '\u8842',
                '\u8852', '\u8859', '\u885E', '\u8862', '\u886B', '\u8881', '\u887E', '\u889E',
                '\u8875', '\u887D', '\u88B5', '\u8872', '\u8882', '\u8897', '\u8892', '\u88AE',
                '\u8899', '\u88A2', '\u888D', '\u88A4', '\u88B0', '\u88BF', '\u88B1', '\u88C3',
                '\u88C4', '\u88D4', '\u88D8', '\u88D9', '\u88DD', '\u88F9', '\u8902', '\u88FC',
                '\u88F4', '\u88E8', '\u88F2', '\u8904', '\u890C', '\u890A', '\u8913', '\u8943',
                '\u891E', '\u8925', '\u892A', '\u892B', '\u8941', '\u8944', '\u893B', '\u8936',
                '\u8938', '\u894C', '\u891D', '\u8960', '\u895E', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E6 ===
            {
                '\u8966', '\u8964', '\u896D', '\u896A', '\u896F', '\u8974', '\u8977', '\u897E',
                '\u8983', '\u8988', '\u898A', '\u8993', '\u8998', '\u89A1', '\u89A9', '\u89A6',
                '\u89AC', '\u89AF', '\u89B2', '\u89BA', '\u89BD', '\u89BF', '\u89C0', '\u89DA',
                '\u89DC', '\u89DD', '\u89E7', '\u89F4', '\u89F8', '\u8A03', '\u8A16', '\u8A10',
                '\u8A0C', '\u8A1B', '\u8A1D', '\u8A25', '\u8A36', '\u8A41', '\u8A5B', '\u8A52',
                '\u8A46', '\u8A48', '\u8A7C', '\u8A6D', '\u8A6C', '\u8A62', '\u8A85', '\u8A82',
                '\u8A84', '\u8AA8', '\u8AA1', '\u8A91', '\u8AA5', '\u8AA6', '\u8A9A', '\u8AA3',
                '\u8AC4', '\u8ACD', '\u8AC2', '\u8ADA', '\u8AEB', '\u8AF3', '\u8AE7', '\uFFFD',
                '\u8AE4', '\u8AF1', '\u8B14', '\u8AE0', '\u8AE2', '\u8AF7', '\u8ADE', '\u8ADB',
                '\u8B0C', '\u8B07', '\u8B1A', '\u8AE1', '\u8B16', '\u8B10', '\u8B17', '\u8B20',
                '\u8B33', '\u97AB', '\u8B26', '\u8B2B', '\u8B3E', '\u8B28', '\u8B41', '\u8B4C',
                '\u8B4F', '\u8B4E', '\u8B49', '\u8B56', '\u8B5B', '\u8B5A', '\u8B6B', '\u8B5F',
                '\u8B6C', '\u8B6F', '\u8B74', '\u8B7D', '\u8B80', '\u8B8C', '\u8B8E', '\u8B92',
                '\u8B93', '\u8B96', '\u8B99', '\u8B9A', '\u8C3A', '\u8C41', '\u8C3F', '\u8C48',
                '\u8C4C', '\u8C4E', '\u8C50', '\u8C55', '\u8C62', '\u8C6C', '\u8C78', '\u8C7A',
                '\u8C82', '\u8C89', '\u8C85', '\u8C8A', '\u8C8D', '\u8C8E', '\u8C94', '\u8C7C',
                '\u8C98', '\u621D', '\u8CAD', '\u8CAA', '\u8CBD', '\u8CB2', '\u8CB3', '\u8CAE',
                '\u8CB6', '\u8CC8', '\u8CC1', '\u8CE4', '\u8CE3', '\u8CDA', '\u8CFD', '\u8CFA',
                '\u8CFB', '\u8D04', '\u8D05', '\u8D0A', '\u8D07', '\u8D0F', '\u8D0D', '\u8D10',
                '\u9F4E', '\u8D13', '\u8CCD', '\u8D14', '\u8D16', '\u8D67', '\u8D6D', '\u8D71',
                '\u8D73', '\u8D81', '\u8D99', '\u8DC2', '\u8DBE', '\u8DBA', '\u8DCF', '\u8DDA',
                '\u8DD6', '\u8DCC', '\u8DDB', '\u8DCB', '\u8DEA', '\u8DEB', '\u8DDF', '\u8DE3',
                '\u8DFC', '\u8E08', '\u8E09', '\u8DFF', '\u8E1D', '\u8E1E', '\u8E10', '\u8E1F',
                '\u8E42', '\u8E35', '\u8E30', '\u8E34', '\u8E4A', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E7 ===
            {
                '\u8E47', '\u8E49', '\u8E4C', '\u8E50', '\u8E48', '\u8E59', '\u8E64', '\u8E60',
                '\u8E2A', '\u8E63', '\u8E55', '\u8E76', '\u8E72', '\u8E7C', '\u8E81', '\u8E87',
                '\u8E85', '\u8E84', '\u8E8B', '\u8E8A', '\u8E93', '\u8E91', '\u8E94', '\u8E99',
                '\u8EAA', '\u8EA1', '\u8EAC', '\u8EB0', '\u8EC6', '\u8EB1', '\u8EBE', '\u8EC5',
                '\u8EC8', '\u8ECB', '\u8EDB', '\u8EE3', '\u8EFC', '\u8EFB', '\u8EEB', '\u8EFE',
                '\u8F0A', '\u8F05', '\u8F15', '\u8F12', '\u8F19', '\u8F13', '\u8F1C', '\u8F1F',
                '\u8F1B', '\u8F0C', '\u8F26', '\u8F33', '\u8F3B', '\u8F39', '\u8F45', '\u8F42',
                '\u8F3E', '\u8F4C', '\u8F49', '\u8F46', '\u8F4E', '\u8F57', '\u8F5C', '\uFFFD',
                '\u8F62', '\u8F63', '\u8F64', '\u8F9C', '\u8F9F', '\u8FA3', '\u8FAD', '\u8FAF',
                '\u8FB7', '\u8FDA', '\u8FE5', '\u8FE2', '\u8FEA', '\u8FEF', '\u9087', '\u8FF4',
                '\u9005', '\u8FF9', '\u8FFA', '\u9011', '\u9015', '\u9021', '\u900D', '\u901E',
                '\u9016', '\u900B', '\u9027', '\u9036', '\u9035', '\u9039', '\u8FF8', '\u904F',
                '\u9050', '\u9051', '\u9052', '\u900E', '\u9049', '\u903E', '\u9056', '\u9058',
                '\u905E', '\u9068', '\u906F', '\u9076', '\u96A8', '\u9072', '\u9082', '\u907D',
                '\u9081', '\u9080', '\u908A', '\u9089', '\u908F', '\u90A8', '\u90AF', '\u90B1',
                '\u90B5', '\u90E2', '\u90E4', '\u6248', '\u90DB', '\u9102', '\u9112', '\u9119',
                '\u9132', '\u9130', '\u914A', '\u9156', '\u9158', '\u9163', '\u9165', '\u9169',
                '\u9173', '\u9172', '\u918B', '\u9189', '\u9182', '\u91A2', '\u91AB', '\u91AF',
                '\u91AA', '\u91B5', '\u91B4', '\u91BA', '\u91C0', '\u91C1', '\u91C9', '\u91CB',
                '\u91D0', '\u91D6', '\u91DF', '\u91E1', '\u91DB', '\u91FC', '\u91F5', '\u91F6',
                '\u921E', '\u91FF', '\u9214', '\u922C', '\u9215', '\u9211', '\u925E', '\u9257',
                '\u9245', '\u9249', '\u9264', '\u9248', '\u9295', '\u923F', '\u924B', '\u9250',
                '\u929C', '\u9296', '\u9293', '\u929B', '\u925A', '\u92CF', '\u92B9', '\u92B7',
                '\u92E9', '\u930F', '\u92FA', '\u9344', '\u932E', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E8 ===
            {
                '\u9319', '\u9322', '\u931A', '\u9323', '\u933A', '\u9335', '\u933B', '\u935C',
                '\u9360', '\u937C', '\u936E', '\u9356', '\u93B0', '\u93AC', '\u93AD', '\u9394',
                '\u93B9', '\u93D6', '\u93D7', '\u93E8', '\u93E5', '\u93D8', '\u93C3', '\u93DD',
                '\u93D0', '\u93C8', '\u93E4', '\u941A', '\u9414', '\u9413', '\u9403', '\u9407',
                '\u9410', '\u9436', '\u942B', '\u9435', '\u9421', '\u943A', '\u9441', '\u9452',
                '\u9444', '\u945B', '\u9460', '\u9462', '\u945E', '\u946A', '\u9229', '\u9470',
                '\u9475', '\u9477', '\u947D', '\u945A', '\u947C', '\u947E', '\u9481', '\u947F',
                '\u9582', '\u9587', '\u958A', '\u9594', '\u9596', '\u9598', '\u9599', '\uFFFD',
                '\u95A0', '\u95A8', '\u95A7', '\u95AD', '\u95BC', '\u95BB', '\u95B9', '\u95BE',
                '\u95CA', '\u6FF6', '\u95C3', '\u95CD', '\u95CC', '\u95D5', '\u95D4', '\u95D6',
                '\u95DC', '\u95E1', '\u95E5', '\u95E2', '\u9621', '\u9628', '\u962E', '\u962F',
                '\u9642', '\u964C', '\u964F', '\u964B', '\u9677', '\u965C', '\u965E', '\u965D',
                '\u96F5', '\u9666', '\u9672', '\u966C', '\u968D', '\u9698', '\u9695', '\u9697',
                '\u96AA', '\u96A7', '\u96B1', '\u96B2', '\u96B0', '\u96B4', '\u96B6', '\u96B8',
                '\u96B9', '\u96CE', '\u96CB', '\u96C9', '\u96CD', '\u894D', '\u96DC', '\u970D',
                '\u96D5', '\u96F9', '\u9704', '\u9706', '\u9708', '\u9713', '\u970E', '\u9711',
                '\u970F', '\u9716', '\u9719', '\u9724', '\u972A', '\u9730', '\u9739', '\u973D',
                '\u973E', '\u9744', '\u9746', '\u9748', '\u9742', '\u9749', '\u975C', '\u9760',
                '\u9764', '\u9766', '\u9768', '\u52D2', '\u976B', '\u9771', '\u9779', '\u9785',
                '\u977C', '\u9781', '\u977A', '\u9786', '\u978B', '\u978F', '\u9790', '\u979C',
                '\u97A8', '\u97A6', '\u97A3', '\u97B3', '\u97B4', '\u97C3', '\u97C6', '\u97C8',
                '\u97CB', '\u97DC', '\u97ED', '\u9F4F', '\u97F2', '\u7ADF', '\u97F6', '\u97F5',
                '\u980F', '\u980C', '\u9838', '\u9824', '\u9821', '\u9837', '\u983D', '\u9846',
                '\u984F', '\u984B', '\u986B', '\u986F', '\u9870', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE E9 ===
            {
                '\u9871', '\u9874', '\u9873', '\u98AA', '\u98AF', '\u98B1', '\u98B6', '\u98C4',
                '\u98C3', '\u98C6', '\u98E9', '\u98EB', '\u9903', '\u9909', '\u9912', '\u9914',
                '\u9918', '\u9921', '\u991D', '\u991E', '\u9924', '\u9920', '\u992C', '\u992E',
                '\u993D', '\u993E', '\u9942', '\u9949', '\u9945', '\u9950', '\u994B', '\u9951',
                '\u9952', '\u994C', '\u9955', '\u9997', '\u9998', '\u99A5', '\u99AD', '\u99AE',
                '\u99BC', '\u99DF', '\u99DB', '\u99DD', '\u99D8', '\u99D1', '\u99ED', '\u99EE',
                '\u99F1', '\u99F2', '\u99FB', '\u99F8', '\u9A01', '\u9A0F', '\u9A05', '\u99E2',
                '\u9A19', '\u9A2B', '\u9A37', '\u9A45', '\u9A42', '\u9A40', '\u9A43', '\uFFFD',
                '\u9A3E', '\u9A55', '\u9A4D', '\u9A5B', '\u9A57', '\u9A5F', '\u9A62', '\u9A65',
                '\u9A64', '\u9A69', '\u9A6B', '\u9A6A', '\u9AAD', '\u9AB0', '\u9ABC', '\u9AC0',
                '\u9ACF', '\u9AD1', '\u9AD3', '\u9AD4', '\u9ADE', '\u9ADF', '\u9AE2', '\u9EE3',
                '\u9AE6', '\u9AEF', '\u9AEB', '\u9AEE', '\u9AF4', '\u9AF1', '\u9AF7', '\u9AFB',
                '\u9B06', '\u9B18', '\u9B1A', '\u9B1F', '\u9B22', '\u9B23', '\u9B25', '\u9B27',
                '\u9B28', '\u9B29', '\u9B2A', '\u9B2E', '\u9B2F', '\u9B32', '\u9B44', '\u9B43',
                '\u9B4F', '\u9B4D', '\u9B4E', '\u9B51', '\u9B58', '\u9B74', '\u9B93', '\u9B83',
                '\u9B91', '\u9B96', '\u9B97', '\u9B9F', '\u9BA0', '\u9BA8', '\u9BB4', '\u9BC0',
                '\u9BCA', '\u9BB9', '\u9BC6', '\u9BCF', '\u9BD1', '\u9BD2', '\u9BE3', '\u9BE2',
                '\u9BE4', '\u9BD4', '\u9BE1', '\u9C3A', '\u9BF2', '\u9BF1', '\u9BF0', '\u9C15',
                '\u9C14', '\u9C09', '\u9C13', '\u9C0C', '\u9C06', '\u9C08', '\u9C12', '\u9C0A',
                '\u9C04', '\u9C2E', '\u9C1B', '\u9C25', '\u9C24', '\u9C21', '\u9C30', '\u9C47',
                '\u9C32', '\u9C46', '\u9C3E', '\u9C5A', '\u9C60', '\u9C67', '\u9C76', '\u9C78',
                '\u9CE7', '\u9CEC', '\u9CF0', '\u9D09', '\u9D08', '\u9CEB', '\u9D03', '\u9D06',
                '\u9D2A', '\u9D26', '\u9DAF', '\u9D23', '\u9D1F', '\u9D44', '\u9D15', '\u9D12',
                '\u9D41', '\u9D3F', '\u9D3E', '\u9D46', '\u9D48', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE EA ===
            {
                '\u9D5D', '\u9D5E', '\u9D64', '\u9D51', '\u9D50', '\u9D59', '\u9D72', '\u9D89',
                '\u9D87', '\u9DAB', '\u9D6F', '\u9D7A', '\u9D9A', '\u9DA4', '\u9DA9', '\u9DB2',
                '\u9DC4', '\u9DC1', '\u9DBB', '\u9DB8', '\u9DBA', '\u9DC6', '\u9DCF', '\u9DC2',
                '\u9DD9', '\u9DD3', '\u9DF8', '\u9DE6', '\u9DED', '\u9DEF', '\u9DFD', '\u9E1A',
                '\u9E1B', '\u9E1E', '\u9E75', '\u9E79', '\u9E7D', '\u9E81', '\u9E88', '\u9E8B',
                '\u9E8C', '\u9E92', '\u9E95', '\u9E91', '\u9E9D', '\u9EA5', '\u9EA9', '\u9EB8',
                '\u9EAA', '\u9EAD', '\u9761', '\u9ECC', '\u9ECE', '\u9ECF', '\u9ED0', '\u9ED4',
                '\u9EDC', '\u9EDE', '\u9EDD', '\u9EE0', '\u9EE5', '\u9EE8', '\u9EEF', '\uFFFD',
                '\u9EF4', '\u9EF6', '\u9EF7', '\u9EF9', '\u9EFB', '\u9EFC', '\u9EFD', '\u9F07',
                '\u9F08', '\u76B7', '\u9F15', '\u9F21', '\u9F2C', '\u9F3E', '\u9F4A', '\u9F52',
                '\u9F54', '\u9F63', '\u9F5F', '\u9F60', '\u9F61', '\u9F66', '\u9F67', '\u9F6C',
                '\u9F6A', '\u9F77', '\u9F72', '\u9F76', '\u9F95', '\u9F9C', '\u9FA0', '\u582F',
                '\u69C7', '\u9059', '\u7464', '\u51DC', '\u7199', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE EB ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE EC ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE ED ===
            {
                '\u7E8A', '\u891C', '\u9348', '\u9288', '\u84DC', '\u4FC9', '\u70BB', '\u6631',
                '\u68C8', '\u92F9', '\u66FB', '\u5F45', '\u4E28', '\u4EE1', '\u4EFC', '\u4F00',
                '\u4F03', '\u4F39', '\u4F56', '\u4F92', '\u4F8A', '\u4F9A', '\u4F94', '\u4FCD',
                '\u5040', '\u5022', '\u4FFF', '\u501E', '\u5046', '\u5070', '\u5042', '\u5094',
                '\u50F4', '\u50D8', '\u514A', '\u5164', '\u519D', '\u51BE', '\u51EC', '\u5215',
                '\u529C', '\u52A6', '\u52C0', '\u52DB', '\u5300', '\u5307', '\u5324', '\u5372',
                '\u5393', '\u53B2', '\u53DD', '\uFA0E', '\u549C', '\u548A', '\u54A9', '\u54FF',
                '\u5586', '\u5759', '\u5765', '\u57AC', '\u57C8', '\u57C7', '\uFA0F', '\uFFFD',
                '\uFA10', '\u589E', '\u58B2', '\u590B', '\u5953', '\u595B', '\u595D', '\u5963',
                '\u59A4', '\u59BA', '\u5B56', '\u5BC0', '\u752F', '\u5BD8', '\u5BEC', '\u5C1E',
                '\u5CA6', '\u5CBA', '\u5CF5', '\u5D27', '\u5D53', '\uFA11', '\u5D42', '\u5D6D',
                '\u5DB8', '\u5DB9', '\u5DD0', '\u5F21', '\u5F34', '\u5F67', '\u5FB7', '\u5FDE',
                '\u605D', '\u6085', '\u608A', '\u60DE', '\u60D5', '\u6120', '\u60F2', '\u6111',
                '\u6137', '\u6130', '\u6198', '\u6213', '\u62A6', '\u63F5', '\u6460', '\u649D',
                '\u64CE', '\u654E', '\u6600', '\u6615', '\u663B', '\u6609', '\u662E', '\u661E',
                '\u6624', '\u6665', '\u6657', '\u6659', '\uFA12', '\u6673', '\u6699', '\u66A0',
                '\u66B2', '\u66BF', '\u66FA', '\u670E', '\uF929', '\u6766', '\u67BB', '\u6852',
                '\u67C0', '\u6801', '\u6844', '\u68CF', '\uFA13', '\u6968', '\uFA14', '\u6998',
                '\u69E2', '\u6A30', '\u6A6B', '\u6A46', '\u6A73', '\u6A7E', '\u6AE2', '\u6AE4',
                '\u6BD6', '\u6C3F', '\u6C5C', '\u6C86', '\u6C6F', '\u6CDA', '\u6D04', '\u6D87',
                '\u6D6F', '\u6D96', '\u6DAC', '\u6DCF', '\u6DF8', '\u6DF2', '\u6DFC', '\u6E39',
                '\u6E5C', '\u6E27', '\u6E3C', '\u6EBF', '\u6F88', '\u6FB5', '\u6FF5', '\u7005',
                '\u7007', '\u7028', '\u7085', '\u70AB', '\u710F', '\u7104', '\u715C', '\u7146',
                '\u7147', '\uFA15', '\u71C1', '\u71FE', '\u72B1', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE EE ===
            {
                '\u72BE', '\u7324', '\uFA16', '\u7377', '\u73BD', '\u73C9', '\u73D6', '\u73E3',
                '\u73D2', '\u7407', '\u73F5', '\u7426', '\u742A', '\u7429', '\u742E', '\u7462',
                '\u7489', '\u749F', '\u7501', '\u756F', '\u7682', '\u769C', '\u769E', '\u769B',
                '\u76A6', '\uFA17', '\u7746', '\u52AF', '\u7821', '\u784E', '\u7864', '\u787A',
                '\u7930', '\uFA18', '\uFA19', '\uFA1A', '\u7994', '\uFA1B', '\u799B', '\u7AD1',
                '\u7AE7', '\uFA1C', '\u7AEB', '\u7B9E', '\uFA1D', '\u7D48', '\u7D5C', '\u7DB7',
                '\u7DA0', '\u7DD6', '\u7E52', '\u7F47', '\u7FA1', '\uFA1E', '\u8301', '\u8362',
                '\u837F', '\u83C7', '\u83F6', '\u8448', '\u45B8', '\u5538', '\u0598', '\uFFFD',
                '\u856B', '\uFA1F', '\u85B0', '\uFA20', '\uFA21', '\u8807', '\u88F5', '\u8A12',
                '\u8A37', '\u8A79', '\u8AA7', '\u8ABE', '\u8ADF', '\uFA22', '\u8AF6', '\u8B53',
                '\u8B7F', '\u8CF0', '\u8CF4', '\u8D12', '\u8D76', '\uFA23', '\u8ECF', '\uFA24',
                '\uFA25', '\u9067', '\u90DE', '\uFA26', '\u9115', '\u9127', '\u91DA', '\u91D7',
                '\u91DE', '\u91ED', '\u91EE', '\u91E4', '\u91E5', '\u9206', '\u9210', '\u920A',
                '\u923A', '\u9240', '\u923C', '\u924E', '\u9259', '\u9251', '\u9239', '\u9267',
                '\u92A7', '\u9277', '\u9278', '\u92E7', '\u92D7', '\u92D9', '\u92D0', '\uFA27',
                '\u92D5', '\u92E0', '\u92D3', '\u9325', '\u9321', '\u92FB', '\uFA28', '\u931E',
                '\u92FF', '\u931D', '\u9302', '\u9370', '\u9357', '\u93A4', '\u93C6', '\u93DE',
                '\u93F8', '\u9431', '\u9445', '\u9448', '\u9592', '\uF9DC', '\uFA29', '\u969D',
                '\u96AF', '\u9733', '\u973B', '\u9743', '\u974D', '\u974F', '\u9751', '\u9755',
                '\u9857', '\u9865', '\uFA2A', '\uFA2B', '\u9927', '\uFA2C', '\u999E', '\u9A4E',
                '\u9AD9', '\u9ADC', '\u9B75', '\u9B72', '\u9B8F', '\u9BBA', '\u9BBB', '\u9C00',
                '\u9D70', '\u9D6B', '\uFA2D', '\u1E19', '\u9ED1', '\uFFFD', '\uFFFD', '\u2170',
                '\u2171', '\u2172', '\u2173', '\u2174', '\u2175', '\u2176', '\u2177', '\u2178',
                '\u2179', '\uFFE2', '\uFFE4', '\uFF07', '\uFF02', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE EF ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F0 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F1 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F2 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F3 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F4 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F5 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F6 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F7 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F8 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE F9 ===
            {
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE FA ===
            {
                '\u2170', '\u2171', '\u2172', '\u2173', '\u2174', '\u2175', '\u2176', '\u2177',
                '\u2178', '\u2179', '\u2160', '\u2161', '\u2162', '\u2163', '\u2164', '\u2165',
                '\u2166', '\u2167', '\u2168', '\u2169', '\uFFE2', '\uFFE4', '\uFF07', '\uFF02',
                '\u3231', '\u2116', '\u2121', '\u2235', '\u7E8A', '\u891C', '\u9348', '\u9288',
                '\u84DC', '\u4FC9', '\u70BB', '\u6631', '\u68C8', '\u92F9', '\u66FB', '\u5F45',
                '\u4E28', '\u4EE1', '\u4EFC', '\u4F00', '\u4F03', '\u4F39', '\u4F56', '\u4F92',
                '\u4F8A', '\u4F9A', '\u4F94', '\u4FCD', '\u5040', '\u5022', '\u4FFF', '\u501E',
                '\u5046', '\u5070', '\u5042', '\u5094', '\u50F4', '\u50D8', '\u514A', '\uFFFD',
                '\u5164', '\u519D', '\u51BE', '\u51EC', '\u5215', '\u529C', '\u52A6', '\u52C0',
                '\u52DB', '\u5300', '\u5307', '\u5324', '\u5372', '\u5393', '\u53B2', '\u53DD',
                '\uFA0E', '\u549C', '\u548A', '\u54A9', '\u54FF', '\u5586', '\u5759', '\u5765',
                '\u57AC', '\u57C8', '\u57C7', '\uFA0F', '\uFA10', '\u589E', '\u58B2', '\u590B',
                '\u5953', '\u595B', '\u595D', '\u5963', '\u59A4', '\u59BA', '\u5B56', '\u5BC0',
                '\u752F', '\u5BD8', '\u5BEC', '\u5C1E', '\u5CA6', '\u5CBA', '\u5CF5', '\u5D27',
                '\u5D53', '\uFA11', '\u5D42', '\u5D6D', '\u5DB8', '\u5DB9', '\u5DD0', '\u5F21',
                '\u5F34', '\u5F67', '\u5FB7', '\u5FDE', '\u605D', '\u6085', '\u608A', '\u60DE',
                '\u60D5', '\u6120', '\u60F2', '\u6111', '\u6137', '\u6130', '\u6198', '\u6213',
                '\u62A6', '\u63F5', '\u6460', '\u649D', '\u64CE', '\u654E', '\u6000', '\u6015',
                '\u663B', '\u6609', '\u662E', '\u661E', '\u6624', '\u6665', '\u6657', '\u6659',
                '\uFA12', '\u6673', '\u6699', '\u66A0', '\u66B2', '\u66BF', '\u66FA', '\u670E',
                '\uF929', '\u6766', '\u67BB', '\u5268', '\uC067', '\u6801', '\u6844', '\u68CF',
                '\uFA13', '\u6968', '\uFA14', '\u6998', '\u69E2', '\u6A30', '\u6A6B', '\u6A46',
                '\u6A73', '\u6A7E', '\u6AE2', '\u6AE4', '\u6BD6', '\u6C3F', '\u6C5C', '\u6C86',
                '\u6C6F', '\u6CDA', '\u6D04', '\u6D87', '\u6D6F', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE FB ===
            {
                '\u6D96', '\u6DAC', '\u6DCF', '\u6DF8', '\u6DF2', '\u6DFC', '\u6E39', '\u6E5C',
                '\u6E27', '\u6E3C', '\u6EBF', '\u6F88', '\u6FB5', '\u6FF5', '\u7005', '\u7007',
                '\u7028', '\u7085', '\u70AB', '\u710F', '\u7104', '\u715C', '\u7146', '\u7147',
                '\uFA15', '\u7CC1', '\u71FE', '\u72B1', '\u72BE', '\u7324', '\uFA16', '\u7377',
                '\u73BD', '\u73C9', '\u73D6', '\u73E3', '\u73D2', '\u7407', '\u73F5', '\u7426',
                '\u742A', '\u7429', '\u742E', '\u7462', '\u7489', '\u749F', '\u7501', '\u756F',
                '\u7682', '\u769C', '\u769E', '\u769B', '\u76A6', '\uFA17', '\u7746', '\u52AF',
                '\u7821', '\u784E', '\u7864', '\u787A', '\u7930', '\uFA18', '\uFA19', '\uFFFD',
                '\uFA1A', '\u7994', '\uFA1B', '\u799B', '\u7AD1', '\u7AE7', '\uFA1C', '\u7AEB',
                '\u7B9E', '\uFA1D', '\u7D48', '\u7D5C', '\u7DB7', '\u7DA0', '\u7DD6', '\u7E52',
                '\u7F47', '\u7FA1', '\uFA1E', '\u8301', '\u8362', '\u837F', '\u83C7', '\u83F6',
                '\u8448', '\u84B4', '\u8553', '\u8559', '\u856B', '\uFA1F', '\u85B0', '\uFA20',
                '\uFA21', '\u8807', '\u88F5', '\u8A12', '\u8A37', '\u8A79', '\u8AA7', '\u8ABE',
                '\u8ADF', '\uFA22', '\u8AF6', '\u8B53', '\u8B7F', '\u8CF0', '\u8CF4', '\u8D12',
                '\u8D76', '\uFA23', '\u8ECF', '\uFA24', '\uFA25', '\u9067', '\u90DE', '\uFA26',
                '\u9115', '\u9127', '\u91DA', '\u91D7', '\u91DE', '\u91ED', '\u91EE', '\u91E4',
                '\u91E5', '\u9206', '\u9210', '\u920A', '\u923A', '\u9240', '\u923C', '\u924E',
                '\u9259', '\u9251', '\u9239', '\u9267', '\u92A7', '\u9277', '\u9278', '\u92E7',
                '\u92D7', '\u92D9', '\u92D0', '\uFA27', '\u92D5', '\u92E0', '\u92D3', '\u9325',
                '\u9321', '\u92FB', '\uFA28', '\u931E', '\u92FF', '\u931D', '\u9302', '\u9370',
                '\u9357', '\u93A4', '\u93C6', '\u93DE', '\u93F8', '\u9431', '\u9445', '\u9448',
                '\u9592', '\uF9DC', '\uFA29', '\u969D', '\u96AF', '\u9733', '\u973B', '\u9743',
                '\u974D', '\u974F', '\u9751', '\u9755', '\u9857', '\u9865', '\uFA2A', '\uFA2B',
                '\u9927', '\uFA2C', '\u999E', '\u9A4E', '\u9AD9', '\uFFFD', '\uFFFD', '\uFFFD'
            },

            // === PLANE FC ===
            {
                '\u9ADC', '\u9B75', '\u9B72', '\u9B8F', '\u9BB1', '\u9BBB', '\u9C00', '\u9D70',
                '\u9D6B', '\uFA2D', '\u9E19', '\u9ED1', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD',
                '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD', '\uFFFD' 
            }
        };
    }
}
