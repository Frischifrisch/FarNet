﻿using FarNet;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitKit;

public static class Lib
{
	public static string GetGitRoot(string path)
	{
		return Repository.Discover(path) ?? throw new ModuleException($"Not a git repository: {path}");
	}

	public static Signature BuildSignature(Repository repo)
	{
		return repo.Config.BuildSignature(DateTimeOffset.UtcNow);
	}

	public static IEnumerable<Branch> GetBranchesContainingCommit(Repository repo, Commit commit)
	{
		var heads = repo.Refs;
		var headsContainingCommit = repo.Refs.ReachableFrom(heads, new[] { commit });
		return headsContainingCommit
			.Select(branchRef => repo.Branches[branchRef.CanonicalName]);
	}
}