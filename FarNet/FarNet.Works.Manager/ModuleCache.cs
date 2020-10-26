﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace FarNet.Works
{
	class ModuleCache
	{
		const int Version = 4;
		const int idVersion = 0;
		readonly string _FileName;
		readonly Hashtable _Cache;
		readonly int _CountToLoad;
		bool _ToUpdate;
		public int CountLoaded { get; set; }
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public ModuleCache()
		{
			//! read the cache; do not check existence, normally it exists
			_FileName = Far.Api.GetFolderPath(SpecialFolder.LocalData) + (IntPtr.Size == 4 ? @"\FarNet\Cache32.binary" : @"\FarNet\Cache64.binary");
			try
			{
				object deserialized;
				var formatter = new BinaryFormatter();
				using (var stream = new FileStream(_FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
					deserialized = formatter.Deserialize(stream);

				_Cache = deserialized as Hashtable;

				if (_Cache != null && Version != (int)_Cache[idVersion])
					_Cache = null;
			}
			catch (IOException) //! FileNotFoundException, DirectoryNotFoundException
			{
				_Cache = null;
				_ToUpdate = true;
			}
			catch (Exception ex)
			{
				_Cache = null;
				_ToUpdate = true;
				Far.Api.ShowError("Reading cache", ex);
			}

			// new empty cache
			if (_Cache == null)
			{
				_Cache = new Hashtable
				{
					{ idVersion, Version }
				};
			}

			// count to load
			_CountToLoad = _Cache.Count - 1;
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void Update()
		{
			// obsolete records? 
			if (_CountToLoad != CountLoaded)
			{
				var list = new List<string>();

				foreach (var key in _Cache.Keys)
				{
					if (key is string name && !File.Exists(name))
						list.Add(name);
				}

				if (list.Count > 0)
				{
					_ToUpdate = true;
					foreach (var name in list)
						_Cache.Remove(name);
				}
			}

			// write cache
			if (_ToUpdate)
			{
				try
				{
					// ensure the directory
					var dir = Path.GetDirectoryName(_FileName);
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);

					// write the cache
					var formatter = new BinaryFormatter();
					using (var stream = new FileStream(_FileName, FileMode.Create, FileAccess.Write, FileShare.None))
						formatter.Serialize(stream, _Cache);
				}
				catch (Exception ex)
				{
					Far.Api.ShowError("Writing cache", ex);
				}
			}
		}
		public object Get(string key)
		{
			return _Cache[key];
		}
		public void Set(string key, object data)
		{
			_Cache[key] = data;
			_ToUpdate = true;
		}
		public void Remove(string key)
		{
			_Cache.Remove(key);
			_ToUpdate = true;
		}
	}
}
