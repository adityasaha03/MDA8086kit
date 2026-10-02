using System;
using System.Collections.Generic;

namespace Mda8086Kit.Core
{
    public struct KitConfig
    {
        public int MatrixRowPort { get; }
        public int MatrixColPort { get; }
        public int MatrixControlPort { get; }
        public int LcdDataPort { get; }
        public int LcdControlPort { get; }
        public int LcdWriteSeqPort { get; }
        public int LedPort { get; }

        public KitConfig(int matrixRowPort, int matrixColPort, int matrixControlPort, int lcdDataPort, int lcdControlPort, int lcdWriteSeqPort, int ledPort)
        {
            MatrixRowPort = matrixRowPort;
            MatrixColPort = matrixColPort;
            MatrixControlPort = matrixControlPort;
            LcdDataPort = lcdDataPort;
            LcdControlPort = lcdControlPort;
            LcdWriteSeqPort = lcdWriteSeqPort;
            LedPort = ledPort;
        }

        public static KitConfig Parse(string[] lines)
        {
            var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            bool inPortsSection = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    inPortsSection = trimmed.Equals("[Ports]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inPortsSection) continue;

                var parts = trimmed.Split(new[] { '=' }, 2);
                if (parts.Length != 2) throw new ArgumentException($"Invalid line: {trimmed}");
                
                string key = parts[0].Trim();
                string valStr = parts[1].Trim();

                if (values.ContainsKey(key)) throw new ArgumentException($"Duplicate key: {key}");

                values[key] = ParseHex(valStr);
            }

            string[] requiredKeys = { "MatrixRowPort", "MatrixColPort", "MatrixControlPort", "LcdDataPort", "LcdControlPort", "LcdWriteSeqPort", "LedPort" };
            foreach (var key in requiredKeys)
            {
                if (!values.ContainsKey(key)) throw new ArgumentException($"Missing required key: {key}");
            }

            return new KitConfig(
                values["MatrixRowPort"],
                values["MatrixColPort"],
                values["MatrixControlPort"],
                values["LcdDataPort"],
                values["LcdControlPort"],
                values["LcdWriteSeqPort"],
                values["LedPort"]
            );
        }

        private static int ParseHex(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            else if (hex.EndsWith("H", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(0, hex.Length - 1);

            if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int result))
            {
                throw new ArgumentException($"Invalid hex value: {hex}");
            }
            return result;
        }
    }
}
