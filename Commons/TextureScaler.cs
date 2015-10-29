using System.Threading;
using UnityEngine;

// Source: http://wiki.unity3d.com/index.php/TextureScale

namespace Ludiq.Commons
{
	public enum ScalingAlgorithm
	{
		Bilinear,
		Point
	}

	public class TextureScaler
	{
		private class ThreadData
		{
			public int start;
			public int end;
			public ThreadData(int s, int e)
			{
				start = s;
				end = e;
			}
		}

		private static Color[] texColors;
		private static Color[] newColors;
		private static int oldWidth;
		private static float ratioX;
		private static float ratioY;
		private static int newWidth;
		private static int finishCount;
		private static Mutex mutex;

		public static void Scale(Texture2D texture, int newWidth, int newHeight, ScalingAlgorithm algorithm = ScalingAlgorithm.Bilinear)
		{
			ThreadedScale(texture, newWidth, newHeight, algorithm);
		}

		private static void ThreadedScale(Texture2D texture, int newWidth, int newHeight, ScalingAlgorithm algorithm)
		{
			texColors = texture.GetPixels();

			newColors = new Color[newWidth * newHeight];

			if (algorithm == ScalingAlgorithm.Bilinear)
			{
				ratioX = 1.0f / ((float)newWidth / (texture.width - 1));
				ratioY = 1.0f / ((float)newHeight / (texture.height - 1));
			}
			else
			{
				ratioX = ((float)texture.width) / newWidth;
				ratioY = ((float)texture.height) / newHeight;
			}

			oldWidth = texture.width;
			TextureScaler.newWidth = newWidth;
			var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
			var slice = newHeight / cores;

			finishCount = 0;

			if (mutex == null)
			{
				mutex = new Mutex(false);
			}

			if (cores > 1)
			{
				int i = 0;

				ThreadData threadData;

				for (i = 0; i < cores - 1; i++)
				{
					threadData = new ThreadData(slice * i, slice * (i + 1));
					ParameterizedThreadStart ts = algorithm == ScalingAlgorithm.Bilinear ? new ParameterizedThreadStart(BilinearScale) : new ParameterizedThreadStart(PointScale);
					Thread thread = new Thread(ts);
					thread.Start(threadData);
				}

				threadData = new ThreadData(slice * i, newHeight);

				if (algorithm == ScalingAlgorithm.Bilinear)
				{
					BilinearScale(threadData);
				}
				else
				{
					PointScale(threadData);
				}
				while (finishCount < cores)
				{
					Thread.Sleep(1);
				}
			}
			else
			{
				ThreadData threadData = new ThreadData(0, newHeight);

				if (algorithm == ScalingAlgorithm.Bilinear)
				{
					BilinearScale(threadData);
				}
				else
				{
					PointScale(threadData);
				}
			}

			texture.Resize(newWidth, newHeight);
			texture.SetPixels(newColors);
			texture.Apply();
		}

		private static void BilinearScale(System.Object obj)
		{
			ThreadData threadData = (ThreadData)obj;

			for (var y = threadData.start; y < threadData.end; y++)
			{
				int yFloor = (int)Mathf.Floor(y * ratioY);
				var y1 = yFloor * oldWidth;
				var y2 = (yFloor + 1) * oldWidth;
				var yw = y * newWidth;

				for (var x = 0; x < newWidth; x++)
				{
					int xFloor = (int)Mathf.Floor(x * ratioX);
					var xLerp = x * ratioX - xFloor;
					newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp),
														   ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp),
														   y * ratioY - yFloor);
				}
			}

			mutex.WaitOne();
			finishCount++;
			mutex.ReleaseMutex();
		}

		private static void PointScale(System.Object obj)
		{
			ThreadData threadData = (ThreadData)obj;

			for (var y = threadData.start; y < threadData.end; y++)
			{
				var thisY = (int)(ratioY * y) * oldWidth;
				var yw = y * newWidth;
				for (var x = 0; x < newWidth; x++)
				{
					newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
				}
			}

			mutex.WaitOne();
			finishCount++;
			mutex.ReleaseMutex();
		}

		private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
		{
			return new Color(c1.r + (c2.r - c1.r) * value,
							  c1.g + (c2.g - c1.g) * value,
							  c1.b + (c2.b - c1.b) * value,
							  c1.a + (c2.a - c1.a) * value);
		}
	}
}