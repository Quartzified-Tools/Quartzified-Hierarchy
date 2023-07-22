using UnityEditor;
using UnityEngine;

namespace Quartzified.Tools.Hierarchy
{
    internal class Resources
    {
        private static Texture2D pixelWhite;

        public static Texture2D PixelWhite
        {
            get
            {
                if (pixelWhite == null)
                {
                    pixelWhite = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    pixelWhite.SetPixel(0, 0, Color.white);
                    pixelWhite.Apply();
                }

                return pixelWhite;
            }
        }

        private static Texture2D alphaTexture;

        public static Texture2D AlphaTexture
        {
            get
            {
                if (alphaTexture == null)
                {
                    alphaTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);

                    for (int x = 0; x < 16; ++x)
                    {
                        for (int y = 0; y < 16; ++y)
                        {
                            alphaTexture.SetPixel(x, y, Color.clear);
                        }
                    }

                    alphaTexture.Apply();
                }

                return alphaTexture;
            }
        }

        private static Texture2D ramp8x8White;

        public static Texture2D Ramp8x8White
        {
            get
            {
                if (ramp8x8White == null)
                {
                    ramp8x8White = new byte[]
                    {
                            137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 16,
                            0, 0, 0, 16, 8, 6, 0, 0, 0, 31, 243, 255, 97, 0, 0, 0, 40, 73, 68, 65, 84, 56, 17, 99, 252,
                            15, 4, 12, 12,
                            12, 31, 8, 224, 143, 184, 228, 153, 128, 18, 20, 129, 81, 3, 24, 24, 70, 195, 96, 52, 12,
                            64, 153, 104, 224,
                            211, 1, 0, 153, 171, 18, 45, 165, 62, 165, 211, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130
                    }.PNGImageDecode();
                }

                return ramp8x8White;
            }
        }

        internal static readonly Texture lockIconOn = EditorGUIUtility.IconContent("LockIcon-On").image;
    }
}

