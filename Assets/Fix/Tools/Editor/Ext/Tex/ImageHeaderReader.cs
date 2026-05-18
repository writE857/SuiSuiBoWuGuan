using System;
using System.IO;
using UnityEngine;

namespace Fix.Editor
{
    public static class ImageHeaderReader
    {
        /// <summary>
        /// 读取图片头部信息获取尺寸（不加载完整图片）
        /// </summary>
        public static bool TryGetImageSize(byte[] imageBytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (imageBytes == null || imageBytes.Length < 16)
                return false;

            // 检测图片格式
            if (IsPNG(imageBytes))
            {
                return TryGetPNGSize(imageBytes, out width, out height);
            }
            else if (IsJPEG(imageBytes))
            {
                return TryGetJPEGSize(imageBytes, out width, out height);
            }
            else if (IsBMP(imageBytes))
            {
                return TryGetBMPSize(imageBytes, out width, out height);
            }
            else if (IsGIF(imageBytes))
            {
                return TryGetGIFSize(imageBytes, out width, out height);
            }
            else if (IsTIFF(imageBytes))
            {
                return TryGetTIFFSize(imageBytes, out width, out height);
            }
            else if (IsWebP(imageBytes))
            {
                return TryGetWebPSize(imageBytes, out width, out height);
            }

            Debug.LogWarning("不支持的图片格式");
            return false;
        }

        #region 格式检测

        private static bool IsPNG(byte[] bytes)
        {
            // PNG 文件头: 89 50 4E 47 0D 0A 1A 0A
            return bytes.Length >= 8 &&
                   bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                   bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
        }

        private static bool IsJPEG(byte[] bytes)
        {
            // JPEG 文件头: FF D8 FF
            return bytes.Length >= 3 &&
                   bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        }

        private static bool IsBMP(byte[] bytes)
        {
            // BMP 文件头: "BM" (42 4D)
            return bytes.Length >= 2 &&
                   bytes[0] == 0x42 && bytes[1] == 0x4D;
        }

        private static bool IsGIF(byte[] bytes)
        {
            // GIF 文件头: "GIF87a" 或 "GIF89a"
            return bytes.Length >= 6 &&
                   ((bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 &&
                     bytes[3] == 0x38 && bytes[4] == 0x37 && bytes[5] == 0x61) || // GIF87a
                    (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 &&
                     bytes[3] == 0x38 && bytes[4] == 0x39 && bytes[5] == 0x61)); // GIF89a
        }

        private static bool IsTIFF(byte[] bytes)
        {
            // TIFF 文件头: "II" (49 49) 或 "MM" (4D 4D)
            return bytes.Length >= 4 &&
                   ((bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00) ||
                    (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A));
        }

        private static bool IsWebP(byte[] bytes)
        {
            // WebP 文件头: "RIFF" (52 49 46 46) 后跟 "WEBP" (57 45 42 50)
            return bytes.Length >= 12 &&
                   bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                   bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;
        }

        #endregion

        #region 尺寸读取实现

        private static bool TryGetPNGSize(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            // PNG 的 IHDR 块在 16 字节后开始
            // IHDR 块的宽高是 4 字节大端序
            if (bytes.Length < 24) return false;

            // 跳过 PNG 头部 (8字节) 和 IHDR 块类型 (4字节)
            int offset = 16;

            // 读取宽度（4字节，大端序）
            width = (bytes[offset] << 24) | (bytes[offset + 1] << 16) |
                    (bytes[offset + 2] << 8) | bytes[offset + 3];

            // 读取高度（4字节，大端序）
            height = (bytes[offset + 4] << 24) | (bytes[offset + 5] << 16) |
                     (bytes[offset + 6] << 8) | bytes[offset + 7];

            return width > 0 && height > 0;
        }

        private static bool TryGetJPEGSize(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            int i = 2; // 跳过 JPEG 头部

            while (i < bytes.Length - 9)
            {
                // 寻找标记 (FF)
                while (i < bytes.Length && bytes[i] != 0xFF) i++;
                if (i >= bytes.Length - 1) break;

                byte marker = bytes[i + 1];

                // SOF0, SOF1, SOF2 (Start Of Frame) 标记包含尺寸信息
                if ((marker >= 0xC0 && marker <= 0xC3) ||
                    (marker >= 0xC5 && marker <= 0xC7) ||
                    (marker >= 0xC9 && marker <= 0xCB) ||
                    (marker >= 0xCD && marker <= 0xCF))
                {
                    // 跳过标记和长度字段
                    int length = (bytes[i + 2] << 8) | bytes[i + 3];

                    // 尺寸在长度字段后的第5和第7字节
                    height = (bytes[i + 5] << 8) | bytes[i + 6];
                    width = (bytes[i + 7] << 8) | bytes[i + 8];

                    return width > 0 && height > 0;
                }

                // 跳过当前标记段
                int segmentLength = (bytes[i + 2] << 8) | bytes[i + 3];
                i += segmentLength + 2;
            }

            return false;
        }

        private static bool TryGetBMPSize(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (bytes.Length < 18) return false;

            // BMP 尺寸信息在 18 字节偏移处（小端序）
            width = bytes[18] | (bytes[19] << 8) | (bytes[20] << 16) | (bytes[21] << 24);
            height = bytes[22] | (bytes[23] << 8) | (bytes[24] << 16) | (bytes[25] << 24);

            // 高度可能是负数（表示从上到下的位图）
            height = Mathf.Abs(height);

            return width > 0 && height > 0;
        }

        private static bool TryGetGIFSize(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (bytes.Length < 10) return false;

            // GIF 尺寸在 6 字节偏移处（小端序）
            width = bytes[6] | (bytes[7] << 8);
            height = bytes[8] | (bytes[9] << 8);

            return width > 0 && height > 0;
        }

        private static bool TryGetTIFFSize(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            // TIFF 解析较复杂，这里简化处理
            // 实际项目中可能需要完整的 TIFF 解析器
            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // 检测字节序
                    bool isLittleEndian = reader.ReadChar() == 'I' && reader.ReadChar() == 'I';

                    // 重置位置
                    stream.Position = 0;

                    // 简化的 TIFF 头解析
                    // ... 复杂实现 ...
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryGetWebPSize(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            // WebP VP8/VP8L 尺寸解析
            if (bytes.Length < 30) return false;

            // 检查 VP8 签名
            if (bytes[12] == 0x56 && bytes[13] == 0x50 && bytes[14] == 0x38)
            {
                // VP8 格式
                int offset = 23;
                width = ((bytes[offset + 1] & 0x3F) << 8) | bytes[offset];
                height = ((bytes[offset + 3] & 0x3F) << 8) | bytes[offset + 2];
                return true;
            }
            // VP8L 和 VP8X 格式解析更复杂...

            return false;
        }

        #endregion
    }
}