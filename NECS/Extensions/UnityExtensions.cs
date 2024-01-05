using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#if UNITY_5_3_OR_NEWER
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// all variant of unity component extensions
/// </summary>
namespace UnityExtensions
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<Type> GetAllSubclassOf(Type parent)
        {
            var allAssembly = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in allAssembly)
                foreach (var t in a.GetTypes())
                    if (t.IsSubclassOf(parent))
                    {
                        yield return t;
                    }
        }
    }
    public static class UnityMathEx
    {
        public static void ResetForces(this Rigidbody rigidbody)
        {
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.velocity = Vector3.zero;
            rigidbody.ResetInertiaTensor();
        }

        public static void ResetForces(this Rigidbody2D rigidbody)
        {
            rigidbody.angularVelocity = 0;
            rigidbody.velocity = Vector2.zero;
        }

        public static Vector3 SetEx(this Vector3 vector, float? newX = null, float? newY = null, float? newZ = null)
        {
            vector.Set(newX != null ? (float)newX : vector.x, newY != null ? (float)newY : vector.y, newZ != null ? (float)newZ : vector.z);
            return vector;
        }

        public static Vector2 SetEx(this Vector2 vector, float? newX = null, float? newY = null)
        {
            vector.Set(newX != null ? (float)newX : vector.x, newY != null ? (float)newY : vector.y);
            return vector;
        }
    }

    public static class UnityObjectExtend
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return (T)gameObject.GetOrAddComponent(typeof(T));
        }
        public static Component GetOrAddComponent(this GameObject gameObject, Type typeComponent)
        {
            var getComp = gameObject.GetComponent(typeComponent);
            if (getComp == null)
            {
                getComp = gameObject.AddComponent(typeComponent);
            }
            return getComp;
        }

        public static T GetOrAddComponent<T>(this Component gameObject) where T : Component
        {
            return (T)gameObject.GetOrAddComponent(typeof(T));
        }
        public static Component GetOrAddComponent(this Component gameObject, Type typeComponent)
        {
            var getComp = gameObject.GetComponent(typeComponent);
            if (getComp == null)
            {
                getComp = gameObject.gameObject.AddComponent(typeComponent);
            }
            return getComp;
        }

        public static T GetOrAddComponent<T>(this MonoBehaviour gameObject) where T : Component
        {
            return (T)gameObject.GetOrAddComponent(typeof(T));
        }
        public static Component GetOrAddComponent(this MonoBehaviour gameObject, Type typeComponent)
        {
            var getComp = gameObject.GetComponent(typeComponent);
            if (getComp == null)
            {
                getComp = gameObject.gameObject.AddComponent(typeComponent);
            }
            return getComp;
        }
    }

    public static class AudioControl
    {
        public static AudioClip TrimAudioClip(AudioClip originalClip, float startPosSec, float lengthSec)
        {
            var originalClipSamples = new float[originalClip.samples];
            originalClip.GetData(originalClipSamples, 0);

            //converts startPosSec & takeAmountSec from seconds to sample amount
            int newStartPosSample = (int)(startPosSec * originalClip.frequency);
            int newLengthSecSample = (int)(lengthSec * originalClip.frequency);

            //gets the trimmed version of the orignalClipSamples
            var newClipSamples = originalClipSamples.Skip(newStartPosSample).Take(newLengthSecSample).ToArray();

            //generates a new empty clip and sets its data according to the newClipSamples
            AudioClip resClip = AudioClip.Create(originalClip.name, newClipSamples.Length, originalClip.channels, originalClip.frequency, false);
            resClip.SetData(newClipSamples, 0);

            return resClip;
        }

        public static AudioClip SnipAudioClip(AudioClip clipIn, int startSamples, int endSamples, bool loop)
        {
            int clipOutSamples;
            if (loop) { clipOutSamples = endSamples - startSamples; }
            else { clipOutSamples = startSamples; }


            AudioClip clipOut = AudioClip.Create("Trimmed " + clipIn.name, clipOutSamples, clipIn.channels, clipIn.frequency, false);
            float[] samplesToCopy = new float[clipOut.samples * clipOut.channels];

            int pointToGetData;

            if (loop) { pointToGetData = startSamples; }
            else { pointToGetData = 0; }
            clipIn.GetData(samplesToCopy, pointToGetData);
            clipOut.SetData(samplesToCopy, 0);

            return clipOut;
        }
    }

    public static class ColorExtensions
    {
        public static Color ToColor(this Color color, int HexVal)
        {
            byte R = (byte)((HexVal >> 16) & 0xFF);
            byte G = (byte)((HexVal >> 8) & 0xFF);
            byte B = (byte)((HexVal) & 0xFF);
            return (Color)new Color32(R, G, B, 255);
        }

        public static Color ToColor(this Color color, long lHexVal)
        {
            var HexVal = Convert.ToInt32(lHexVal);
            byte R = (byte)((HexVal >> 16) & 0xFF);
            byte G = (byte)((HexVal >> 8) & 0xFF);
            byte B = (byte)((HexVal) & 0xFF);
            return (Color)new Color32(R, G, B, 255);
        }

        public static string ToHex(this Color color)
        {
            Color32 c = color;
            var hex = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", c.r, c.g, c.b, c.a);
            return hex;
        }

        public static Color ToColor(int HexVal)
        {
            byte R = (byte)((HexVal >> 16) & 0xFF);
            byte G = (byte)((HexVal >> 8) & 0xFF);
            byte B = (byte)((HexVal) & 0xFF);
            return (Color)new Color32(R, G, B, 255);
        }

        public static Color ToColor(long lHexVal)
        {
            var HexVal = Convert.ToInt32(lHexVal);
            byte R = (byte)((HexVal >> 16) & 0xFF);
            byte G = (byte)((HexVal >> 8) & 0xFF);
            byte B = (byte)((HexVal) & 0xFF);
            return new Color32(R, G, B, 255);
        }
    }

    public static class SpriteExtensions
    {
        [System.Serializable]
        public enum TransformType
        {
            None,
            Rotate90Clockwise,
            Rotate90CounterClockwise,
            Rotate180,
            FlipHorizontal,
            FlipVertical
        }

        public static Sprite TextureToSprite(Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2());
        public static Sprite Duplicate(this Sprite originalSprite, TransformType transform = TransformType.None)
        {
            if (originalSprite == null) throw new System.Exception("Duplicate called on null sprite");

            Sprite duplicate = null;

            int x = Mathf.FloorToInt(originalSprite.rect.x);
            int y = Mathf.FloorToInt(originalSprite.rect.y);
            int width = Mathf.FloorToInt(originalSprite.rect.width);
            int height = Mathf.FloorToInt(originalSprite.rect.height);

            Color[] originalPixels = originalSprite.texture.GetPixels(x, y, width, height);

            Color[] transformedPixels = null;

            switch (transform)
            {
                case TransformType.None:
                    transformedPixels = originalPixels;
                    break;
                case TransformType.Rotate90Clockwise:
                    {
                        transformedPixels = new Color[originalPixels.Length];

                        for (int segment = 0; segment < transformedPixels.Length; segment += height)
                        {
                            for (int offset = 0; offset < height; offset++)
                            {
                                int pair = (offset * width) + (width - 1) - (segment / height);

                                transformedPixels[segment + offset] = originalPixels[pair];
                            }
                        }

                        int temp = width;

                        width = height;

                        height = temp;
                    }
                    break;
                case TransformType.Rotate90CounterClockwise:
                    {
                        transformedPixels = new Color[originalPixels.Length];

                        for (int segment = 0; segment < transformedPixels.Length; segment += height)
                        {
                            for (int offset = 0; offset < height; offset++)
                            {
                                int pair = (transformedPixels.Length - 1) - (offset * width) - (width - 1) + (segment / height);

                                transformedPixels[segment + offset] = originalPixels[pair];
                            }
                        }

                        int temp = width;

                        width = height;

                        height = temp;
                    }
                    break;
                case TransformType.Rotate180:
                    transformedPixels = originalPixels;

                    System.Array.Reverse(transformedPixels);
                    break;
                case TransformType.FlipHorizontal:
                    transformedPixels = originalPixels;

                    for (int segmentStart = 0; segmentStart < transformedPixels.Length; segmentStart += width)
                    {
                        System.Array.Reverse(transformedPixels, segmentStart, width);
                    }
                    break;
                case TransformType.FlipVertical:
                    transformedPixels = new Color[originalPixels.Length];

                    for (int leftSegment = 0, rightSegment = transformedPixels.Length - width;
                        leftSegment < rightSegment;
                        leftSegment += width, rightSegment -= width)
                    {

                        for (int adjustment = 0; adjustment < width; adjustment++)
                        {
                            int a = leftSegment + adjustment;
                            int b = rightSegment + adjustment;

                            transformedPixels[a] = originalPixels[b];
                            transformedPixels[b] = originalPixels[a];
                        }
                    }
                    break;
            }

            if (transformedPixels != null)
            {
                Texture2D texture = new Texture2D(width, height);

                texture.SetPixels(transformedPixels);
                texture.Apply();

                duplicate = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), originalSprite.pixelsPerUnit);
            }

            return duplicate;
        }
    }

    public static class UnityExtensions
    {
        public static void DestroyAllChilds(this UnityEngine.Transform transform)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
            }
        }
        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }
        public static Component CopyComponent(this Component original, GameObject destination)
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            // Copied fields can be restricted with BindingFlags
            System.Reflection.FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (System.Reflection.FieldInfo field in fields)
            {
                try { field.SetValue(copy, field.GetValue(original)); } catch { }
            }

            System.Reflection.PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly);
            foreach (System.Reflection.PropertyInfo field in properties)
            {
                try { field.SetValue(copy, field.GetValue(original)); } catch { }
            }
            return copy;
        }

        public static T CopyComponent<T>(this T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);

            System.Reflection.FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (System.Reflection.FieldInfo field in fields)
            {
                try { field.SetValue(copy, field.GetValue(original)); } catch { }
            }

            System.Reflection.PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly);
            foreach (System.Reflection.PropertyInfo field in properties)
            {
                try { field.SetValue(copy, field.GetValue(original)); } catch { }
            }
            return copy as T;
        }
    }

    public struct LayerMaskEx
    {
        public static bool PresentedInLayerMask(LayerMask mask, int layer)
        {
            return (mask & (1 << layer)) != 0;
        }
    }
}
#endif