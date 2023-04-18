﻿using FarNet;
using LibGit2Sharp;
using System.Data.Common;
using System.IO;
using System.Text;

namespace GitKit;

sealed class CommitCommand : BaseCommand
{
	readonly CommitOptions op = new();
	readonly string _message;
	readonly bool _All;
	readonly char _CommentaryChar;

	public CommitCommand(Repository repo, string value, DbConnectionStringBuilder parameters) : base(repo)
	{
		_message = value;

		_All = parameters.GetValue<bool>("All");

		op.AmendPreviousCommit = parameters.GetValue<bool>("AmendPreviousCommit");
		op.AllowEmptyCommit = parameters.GetValue<bool>("AllowEmptyCommit");

		var PrettifyMessage = parameters.GetValue<bool>("PrettifyMessage");
		_CommentaryChar = parameters.GetValue<char>("CommentaryChar");
		if (_CommentaryChar == 0)
		{
			op.PrettifyMessage = PrettifyMessage;
		}
		else
		{
			op.PrettifyMessage = true;
			op.CommentaryChar = _CommentaryChar;
		}
	}

	string EditMessage()
	{
		Commit? tip = _repo.Head.Tip;

		var message = string.Empty;
		if (op.AmendPreviousCommit && tip is not null)
			message = tip.Message;

		if (_CommentaryChar > 0 && tip is not null)
		{
			var sb = new StringBuilder();
			sb.AppendLine(message.TrimEnd());
			sb.AppendLine();

			if (op.AmendPreviousCommit && _repo.Head.TrackedBranch is not null && _repo.Head.TrackedBranch.Tip == tip)
			{
				sb.AppendLine($"{_CommentaryChar} WARNING:");
				sb.AppendLine($"{_CommentaryChar}\tThe remote commit will be amended.");
				sb.AppendLine();
			}

			sb.AppendLine($"{_CommentaryChar} Changes to be committed:");

			var changes = _repo.Diff.Compare<TreeChanges>(
				tip.Tree,
				_All ? (DiffTargets.Index | DiffTargets.WorkingDirectory) : DiffTargets.Index);

			foreach (var change in changes)
				sb.AppendLine($"{_CommentaryChar}\t{change.Status}:\t{change.Path}");

			message = sb.ToString();
		}

		var file = Path.Combine(_repo.Info.Path, "COMMIT_EDITMSG");
		File.WriteAllText(file, message);

		var editor = Far.Api.CreateEditor();
		editor.FileName = file;
		editor.CodePage = 65001;
		editor.DisableHistory = true;
		editor.Caret = new Point(0, 0);
		editor.Title = (op.AmendPreviousCommit ? "Amend commit" : "Commit") + $" on branch {_repo.Head.FriendlyName} -- empty message aborts the commit";
		editor.Open(OpenMode.Modal);

		message = File.ReadAllText(file);
		if (_CommentaryChar > 0)
		{
			op.PrettifyMessage = false;
			message = Commit.PrettifyMessage(message, _CommentaryChar);
		}

		return message;
	}

	public override void Invoke()
	{
		var message = _message == "#" ? EditMessage() : _message;
		if (message.Length == 0)
		{
			Far.Api.UI.WriteLine("Aborting commit due to empty commit message.");
			return;
		}

		if (_All)
			Commands.Stage(_repo, "*");

		var sig = Lib.BuildSignature(_repo);
		_repo.Commit(message, sig, sig, op);
	}
}