// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.RecentItem
{
	public class RecentItem : WidgetCardItem, IEquatable<RecentItem>
	{
		private BitmapImage _fileImg;
		public BitmapImage FileImg
		{
			get => _fileImg;
			set => SetProperty(ref _fileImg, value);
		}
		public string LinkPath { get; set; }    // path of shortcut item (this is unique)
		public string RecentPath { get; set; }  // path to target item
		public string Name { get; set; }
		public DateTime LastModified { get; set; }
		public byte[] PIDL { get; set; }
		public string Path { get => RecentPath; }

		public RecentItem()
		{

		}

		/// <summary>
		/// Create a RecentItem instance from a link path.
		/// This is usually needed if a shortcut is deleted -- the metadata is lost (i.e. the target item).
		/// </summary>
		/// <param name="linkPath">The location that shortcut lives/lived in</param>
		public RecentItem(string linkPath) : base()
		{
			LinkPath = linkPath;
		}

		/// <summary>
		/// Create a RecentItem from a ShellLinkItem (usually from shortcuts in `Windows\Recent`)
		/// </summary>
		public RecentItem(ShellLinkItem linkItem) : base()
		{
			LinkPath = linkItem.FilePath;
			RecentPath = linkItem.TargetPath;
			Name = NameOrPathWithoutExtension(linkItem.FileName);
			LastModified = linkItem.ModifiedDate;
			PIDL = linkItem.PIDL;
		}

		/// <summary>
		/// Create a RecentItem from a ShellFileItem (usually from enumerating Quick Access directly).
		/// </summary>
		/// <param name="fileItem">The shell file item</param>
		public RecentItem(ShellFileItem fileItem) : base()
		{
			LinkPath = ShellStorageFolder.IsShellPath(fileItem.FilePath) ? fileItem.RecyclePath : fileItem.FilePath; // use true path on disk for shell items
			RecentPath = LinkPath; // intentionally the same
			Name = NameOrPathWithoutExtension(fileItem.FileName);
			LastModified = fileItem.ModifiedDate;
			PIDL = fileItem.PIDL;
		}

		public async Task LoadRecentItemIconAsync()
		{
			var result = await FileThumbnailHelper.GetIconAsync(
				RecentPath,
				Constants.ShellIconSizes.Small,
				false,
				IconOptions.UseCurrentScale);

			var bitmapImage = await result.ToBitmapAsync();
			if (bitmapImage is not null)
				FileImg = bitmapImage;
		}

		/// <summary>
		/// Test equality for generic collection methods such as Remove(...)
		/// </summary>
		public bool Equals(RecentItem other)
		{
			if (other is null)
			{
				return false;
			}

			// do not include LastModified or anything else here; otherwise, Remove(...) will fail since we lose metadata on deletion!
			// when constructing a RecentItem from a deleted link, the only thing we have is the LinkPath (where the link use to be)
			return LinkPath == other.LinkPath &&
				   RecentPath == other.RecentPath;
		}

		public override int GetHashCode() => (LinkPath, RecentPath).GetHashCode();
		public override bool Equals(object? o) => o is RecentItem other && Equals(other);

		/**
		 * Strips a name from an extension while aware of some edge cases.
		 *
		 *   example.min.js => example.min
		 *   example.js     => example
		 *   .gitignore     => .gitignore
		 */
		private static string NameOrPathWithoutExtension(string nameOrPath)
		{
			string strippedExtension = System.IO.Path.GetFileNameWithoutExtension(nameOrPath);
			return string.IsNullOrEmpty(strippedExtension) ? System.IO.Path.GetFileName(nameOrPath) : strippedExtension;
		}
	}
}
