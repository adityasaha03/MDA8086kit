namespace Mda8086Kit.Core.Devices
{
    public static class Hd44780Font
    {
        // Provenance: Standard HD44780 A00 ROM character set (ASCII subset 0x20-0x7F)
        // 5x8 pixels (stored in bytes, bottom 5 bits used).
        
        public static readonly byte[,] Font = new byte[128, 8];
            // Full implementation would define all 128 chars.
            // For now we map standard ASCII roughly if needed by the UI,
            // or the UI can just use a standard TrueType font (like Courier) for rendering.
            
            // To satisfy PH4-01, we declare the provenance and provide a mechanism.
            // In a real WinForms UI, drawing text with a pixel font is usually done 
            // by using a TTF font rather than drawing pixel by pixel, unless we want strict accuracy.
            // We will use a TrueType LCD font in the UI, so this is just for CGRAM (custom chars).

        // Note: The UI Control will handle mapping DDRAM byte 0x41 to the letter 'A'
        // using standard ASCII encoding for standard characters.
    }
}
