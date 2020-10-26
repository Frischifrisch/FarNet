﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;
using System.Collections;

namespace FarNet.Works
{
	public sealed class ProxyDrawer : ProxyAction, IModuleDrawer
	{
		const int idMask = 0, idPriority = 1;
		readonly Action<IEditor, ModuleDrawerEventArgs> _Handler;
		string _Mask;
		int _Priority;
		internal ProxyDrawer(ModuleManager manager, EnumerableReader reader)
			: base(manager, reader, new ModuleDrawerAttribute())
		{
			Attribute.Mask = (string)reader.Read();
			Attribute.Priority = (int)reader.Read();

			Init();
		}
		internal ProxyDrawer(ModuleManager manager, Type classType)
			: base(manager, classType, typeof(ModuleDrawerAttribute))
		{
			Init();
		}
		public ProxyDrawer(ModuleManager manager, Guid id, ModuleDrawerAttribute attribute, Action<IEditor, ModuleDrawerEventArgs> handler)
			: base(manager, id, (attribute == null ? null : (ModuleDrawerAttribute)attribute.Clone()))
		{
			_Handler = handler;

			Init();
		}
		public Action<IEditor, ModuleDrawerEventArgs> CreateHandler()
		{
			if (_Handler != null)
				return _Handler;

			Invoking();
			ModuleDrawer instance = (ModuleDrawer)GetInstance();
			return instance.Invoke;
		}
		public sealed override string ToString()
		{
			return string.Format(null, "{0} Mask='{1}'", base.ToString(), Mask);
		}
		internal sealed override void WriteCache(IList data)
		{
			base.WriteCache(data);
			data.Add(Attribute.Mask);
			data.Add(Attribute.Priority);
		}
		new ModuleDrawerAttribute Attribute => (ModuleDrawerAttribute)base.Attribute;
		public override ModuleItemKind Kind => ModuleItemKind.Drawer;
		public string Mask
		{
			get { return _Mask; }
			set { _Mask = value ?? throw new ArgumentNullException("value"); }
		}
		public int Priority
		{
			get { return _Priority; }
			set { _Priority = value; }
		}
		void Init()
		{
			if (Attribute.Mask == null)
				Attribute.Mask = string.Empty;
		}
		internal override Hashtable SaveData()
		{
			var data = new Hashtable();

			if (_Mask != Attribute.Mask)
				data.Add(idMask, _Mask);

			if (_Priority != Attribute.Priority)
				data.Add(idPriority, _Priority);

			return data;
		}
		internal override void LoadData(Hashtable data)
		{
			if (data == null)
			{
				_Mask = Attribute.Mask;
				_Priority = Attribute.Priority;
			}
			else
			{
				_Mask = data[idMask] as string ?? Attribute.Mask;
				_Priority = (int)(data[idPriority] ?? Attribute.Priority);
			}
		}
	}
}
