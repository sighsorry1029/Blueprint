using System.Runtime.InteropServices;
using System.Text;
public static class ClipboardUtils
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight; 
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();
    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);
    [DllImport("user32.dll")]
    private static extern bool IsClipboardFormatAvailable(uint format);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalSize(IntPtr hMem);
    private const uint CF_DIB = 8;
    private const uint CF_TEXT = 1;
    private const uint CF_UNICODETEXT = 13;
    private static Texture2D CreateTextureFromClipboardData(byte[] pixelData, int width, int height, int bytesPerPixel)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] colors = new Color32[width * height];
        int index = 0;
        for (int i = 0; i < colors.Length; i++)
        {
            byte r = pixelData[index + 2];
            byte g = pixelData[index + 1];
            byte b = pixelData[index];
            byte a = (bytesPerPixel == 4) ? pixelData[index + 3] : (byte)255;
            colors[i] = new Color32(r, g, b, a);
            index += bytesPerPixel;
        }
        texture.SetPixels32(colors);
        texture.Apply();
        return texture;
    }
    private static Texture2D ResizeTexture(Texture2D original, int targetWidth, int targetHeight)
    {
        Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight, original.format, false);
        float widthRatio = (float)original.width / targetWidth;
        float heightRatio = (float)original.height / targetHeight;
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                float sampleX = x * widthRatio;
                float sampleY = y * heightRatio;
                Color color = original.GetPixelBilinear(sampleX / original.width, sampleY / original.height);
                resizedTexture.SetPixel(x, y, color);
            }
        }

        resizedTexture.Apply();
        return resizedTexture;
    }
    private static Texture2D ConvertClipboardToTexture(IntPtr hData)
    {
        BITMAPINFOHEADER header = Marshal.PtrToStructure<BITMAPINFOHEADER>(hData);
        if (header.biBitCount != 24 && header.biBitCount != 32) return null;
        int width = header.biWidth;
        int height = Math.Abs(header.biHeight);
        int bytesPerPixel = header.biBitCount / 8;
        IntPtr pixelDataPtr = IntPtr.Add(hData, Marshal.SizeOf(typeof(BITMAPINFOHEADER)));
        byte[] rawPixelData = new byte[width * height * bytesPerPixel];
        Marshal.Copy(pixelDataPtr, rawPixelData, 0, rawPixelData.Length);
        return CreateTextureFromClipboardData(rawPixelData, width, height, bytesPerPixel);
    }
    public static Texture2D GetImage(int maxWidth, int maxHeight)
    {
        if (!IsClipboardFormatAvailable(CF_DIB)) return null;
        if (!OpenClipboard(IntPtr.Zero)) return null;
        IntPtr hData = GetClipboardData(CF_DIB);
        if (hData == IntPtr.Zero)
        {
            CloseClipboard();
            return null;
        }
        try
        {
            Texture2D tex = ConvertClipboardToTexture(hData);
            if (tex.width > maxWidth || tex.height > maxHeight) tex = ResizeTexture(tex, Math.Min(tex.width, maxWidth), Math.Min(tex.height, maxHeight));
            return tex;

        } finally { CloseClipboard(); }
    }
    public static string GetText()
    {
        if (!OpenClipboard(IntPtr.Zero)) return null;
        try
        {
            if (IsClipboardFormatAvailable(CF_UNICODETEXT))
            {
                string unicodeText = ReadUnicodeText();
                if (!string.IsNullOrEmpty(unicodeText))
                {
                    return unicodeText;
                }
            }

            if (IsClipboardFormatAvailable(CF_TEXT))
            {
                return ReadAnsiText();
            }

            return null;
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static string ReadUnicodeText()
    {
        IntPtr hData = GetClipboardData(CF_UNICODETEXT);
        if (hData == IntPtr.Zero) return null;
        IntPtr pData = GlobalLock(hData);
        if (pData == IntPtr.Zero) return null;
        try
        {
            return Marshal.PtrToStringUni(pData);
        }
        finally
        {
            GlobalUnlock(hData);
        }
    }

    private static string ReadAnsiText()
    {
        IntPtr hData = GetClipboardData(CF_TEXT);
        if (hData == IntPtr.Zero) return null;
        IntPtr pData = GlobalLock(hData);
        if (pData == IntPtr.Zero) return null;
        try
        {
            int size = GlobalSize(hData).ToInt32();
            if (size <= 0)
            {
                return Marshal.PtrToStringAnsi(pData);
            }

            byte[] buffer = new byte[size];
            Marshal.Copy(pData, buffer, 0, size);

            int terminatorIndex = Array.IndexOf(buffer, (byte)0);
            int length = terminatorIndex >= 0 ? terminatorIndex : buffer.Length;
            if (length <= 0)
            {
                return string.Empty;
            }

            try
            {
                return Encoding.Default.GetString(buffer, 0, length);
            }
            catch
            {
                return new UTF8Encoding(false, false).GetString(buffer, 0, length);
            }
        }
        finally
        {
            GlobalUnlock(hData);
        }
    }
}
