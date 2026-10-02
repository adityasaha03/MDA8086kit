using System;
using Xunit;
using Mda8086Kit.Core;

namespace Mda8086Kit.Core.Tests
{
    public class KitConfigTests
    {
        [Fact]
        public void Parse_ValidLines_ReturnsConfig()
        {
            var lines = new[]
            {
                "; A comment",
                "[Ports]",
                "MatrixRowPort = 0A",
                "MatrixColPort = 0B",
                "MatrixControlPort = 0C",
                "LcdDataPort = 0D",
                "LcdControlPort = 0E",
                "LcdWriteSeqPort = 30H",
                "LedPort = 0x0F"
            };

            var config = KitConfig.Parse(lines);
            Assert.Equal(0x0A, config.MatrixRowPort);
            Assert.Equal(0x0B, config.MatrixColPort);
            Assert.Equal(0x0C, config.MatrixControlPort);
            Assert.Equal(0x0D, config.LcdDataPort);
            Assert.Equal(0x0E, config.LcdControlPort);
            Assert.Equal(0x30, config.LcdWriteSeqPort);
            Assert.Equal(0x0F, config.LedPort);
        }

        [Fact]
        public void Parse_MissingKey_ThrowsArgumentException()
        {
            var lines = new[]
            {
                "[Ports]",
                "MatrixRowPort = 0A"
                // Missing others
            };

            Assert.Throws<ArgumentException>(() => KitConfig.Parse(lines));
        }

        [Fact]
        public void Parse_DuplicateKey_ThrowsArgumentException()
        {
            var lines = new[]
            {
                "[Ports]",
                "MatrixRowPort = 0A",
                "MatrixColPort = 0B",
                "MatrixControlPort = 0C",
                "LcdDataPort = 0D",
                "LcdControlPort = 0E",
                "LcdWriteSeqPort = 30H",
                "LedPort = 0x0F",
                "MatrixRowPort = 0B" // Duplicate
            };

            Assert.Throws<ArgumentException>(() => KitConfig.Parse(lines));
        }

        [Fact]
        public void Parse_BadHex_ThrowsArgumentException()
        {
            var lines = new[]
            {
                "[Ports]",
                "MatrixRowPort = 0A",
                "MatrixColPort = 0B",
                "MatrixControlPort = 0C",
                "LcdDataPort = 0D",
                "LcdControlPort = 0E",
                "LcdWriteSeqPort = 30H",
                "LedPort = ZZZ" // Bad hex
            };

            Assert.Throws<ArgumentException>(() => KitConfig.Parse(lines));
        }
    }
}
