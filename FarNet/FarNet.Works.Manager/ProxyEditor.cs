﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;
using System.Collections;

namespace FarNet.Works
{
	public sealed class ProxyEditor : ProxyAction, IModuleEditor
	{
		string _Mask;
		internal ProxyEditor(ModuleManager manager, EnumerableReader reader)
			: base(manager, reader, new ModuleEditorAttribute())
		{
			Attribute.Mask = (string)reader.Read();

			Init();
		}
		internal ProxyEditor(ModuleManager manager, Type classType)
			: base(manager, classType, typeof(ModuleEditorAttribute))
		{
			Init();
		}
		public void Invoke(IEditor editor, ModuleEditorEventArgs e)
		{
			Log.Source.TraceInformation("Invoking {0} FileName='{1}'", ClassName, editor.FileName);
			Invoking();

			ModuleEditor instance = (ModuleEditor)GetInstance();
			instance.Invoke(editor, e);
		}
		public sealed override string ToString()
		{
			return string.Format(null, "{0} Mask='{1}'", base.ToString(), Mask);
		}
		internal sealed override void WriteCache(IList data)
		{
			base.WriteCache(data);
			data.Add(Attribute.Mask);
		}
		new ModuleEditorAttribute Attribute => (ModuleEditorAttribute)base.Attribute;
		public override ModuleItemKind Kind => ModuleItemKind.Editor;
		public string Mask
		{
			get { return _Mask; }
			set { _Mask = value ?? throw new ArgumentNullException("value"); }
		}
		void Init()
		{
			if (Attribute.Mask == null)
				Attribute.Mask = string.Empty;
		}
		readonly int idMask = 0;
		internal override Hashtable SaveData()
		{
			var data = new Hashtable();
			if (_Mask != Attribute.Mask)
				data.Add(idMask, _Mask);
			return data;
		}
		internal override void LoadData(Hashtable data)
		{
			if (data == null)
				_Mask = Attribute.Mask;
			else
				_Mask = data[idMask] as string ?? Attribute.Mask;
		}
	}
}
