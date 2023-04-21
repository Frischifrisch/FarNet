﻿using FarNet;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GitKit;

class ChangesExplorer : BaseExplorer
{
	public static Guid MyTypeId = new("7b4c229a-949e-4100-856e-45c17d516d25");
	public Func<TreeChanges> Changes { get; }

	public ChangesExplorer(Repository repository, Func<TreeChanges> changes) : base(repository, MyTypeId)
	{
		Changes = changes;

		CanGetContent = true;
	}

	public override Panel CreatePanel()
	{
		return new ChangesPanel(this);
	}

	public override IEnumerable<FarFile> GetFiles(GetFilesEventArgs args)
	{
		foreach (var change in Changes())
		{
			var file = new SetFile
			{
				Description = change.Status.ToString(),
				Data = change,
			};

			if (change.Status == ChangeKind.Renamed)
				file.Name = $"{change.Path} << {change.OldPath}";
			else
				file.Name = change.Path;

			yield return file;
		}
	}

	public override void GetContent(GetContentEventArgs args)
	{
		var compareOptions = new CompareOptions { ContextLines = 3 };

		var changes = (TreeEntryChanges)args.File.Data!;
		var newBlob = Repository.Lookup<Blob>(changes.Oid);

		string text;
		if (newBlob is null)
		{
			var patch = Repository.Diff.Compare<Patch>(new string[] { changes.Path }, true, null, compareOptions);
			text = patch.Content;
		}
		else
		{
			var oldBlob = Repository.Lookup<Blob>(changes.OldOid);
			var diff = Repository.Diff.Compare(oldBlob, newBlob, compareOptions);
			text = diff.Patch;
		}

		args.CanSet = false;
		args.UseText = text;
		args.UseFileExtension = "diff";
	}
}