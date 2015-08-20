﻿using MatterHackers.Agg.Image;
using MatterHackers.Agg.PlatformAbstract;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MatterHackers.Agg
{
	public class FileSystemStaticData : IStaticData
	{
		private static Dictionary<string, ImageBuffer> cachedImages = new Dictionary<string, ImageBuffer>();

		private string basePath;

		public FileSystemStaticData()
		{
			this.basePath = Directory.Exists("StaticData") ? "StaticData" : Path.Combine("..", "..", "StaticData");
		}

		public FileSystemStaticData(string overridePath)
		{
			this.basePath = overridePath;
		}

		public bool DirectoryExists(string path)
		{
			return Directory.Exists(MapPath(path));
		}

		public bool FileExists(string path)
		{
			return File.Exists(MapPath(path));
		}

		public IEnumerable<string> GetDirectories(string path)
		{
			return Directory.GetDirectories(MapPath(path));
		}

		public IEnumerable<string> GetFiles(string path)
		{
			return Directory.GetFiles(MapPath(path)).Select(p => p.Substring(p.IndexOf("StaticData") + 11));
		}

		/// <summary>
		/// Loads the specified file from the StaticData/Icons path
		/// </summary>
		/// <param name="path">The file path to load</param>
		/// <returns>An ImageBuffer initialized with data from the given file</returns>
		public ImageBuffer LoadIcon(string path)
		{
			return LoadImage(Path.Combine("Icons", path));
		}

		/// <summary>
		/// Loads the specified file from the StaticData/Icons path
		/// </summary>
		/// <param name="path">The file path to load</param>
		/// <param name="buffer">The ImageBuffer to populate with data from the given file</param>
		public void LoadIcon(string path, ImageBuffer buffer)
		{
			LoadImage(Path.Combine("Icons", path), buffer);
		}

		public void LoadImage(string path, ImageBuffer destImage)
		{
			ImageBuffer cachedImage = null;
			if (!cachedImages.TryGetValue(path, out cachedImage))
			{
				using (var imageStream = OpenSteam(path))
				{
					var bitmap = new Bitmap(imageStream);
					cachedImage = new ImageBuffer();
					ImageIOWindowsPlugin.ConvertBitmapToImage(cachedImage, bitmap);
				}
				if (cachedImage.Width < 200 && cachedImage.Height < 200)
				{
					// only cache relatively small images
					cachedImages.Add(path, cachedImage);
				}
			}

			destImage.CopyFrom(cachedImage);
		}

		public ImageBuffer LoadImage(string path)
		{
			ImageBuffer temp = new ImageBuffer();
			LoadImage(path, temp);

			return temp;
		}

		public Stream OpenSteam(string path)
		{
			return File.OpenRead(MapPath(path));
		}

		public string[] ReadAllLines(string path)
		{
			return File.ReadLines(MapPath(path)).ToArray();
		}

		public string ReadAllText(string path)
		{
			return File.ReadAllText(MapPath(path));
		}

		public string MapPath(string path)
		{
			string fullPath = Path.GetFullPath(Path.Combine(this.basePath, path));
			return fullPath;
		}
	}
}