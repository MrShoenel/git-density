﻿using GitDensity.Data.Entities;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Density
{
	internal class GitDensityAnalysisResult
	{
		//[System.Runtime.CompilerServices.IndexerName("CommitPair")]
		//public TreeChanges this[CommitPair pair]
		//{
		//	get
		//	{
		//		var f = pair;
		//		return null;
		//	}
		//}

		public RepositoryEntity Repository { get; protected internal set; }

		public GitDensityAnalysisResult(RepositoryEntity repositoryEntity)
		{
			this.Repository = repositoryEntity;
		}

		public void PersistToDatabase()
		{

		}
	}
}
