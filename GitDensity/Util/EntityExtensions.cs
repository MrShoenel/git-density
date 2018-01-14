using GitDensity.Data.Entities;
using GitDensity.Density;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Util
{
	public static class EntityExtensions
	{
		public static RepositoryEntity AsEntity(this Repository repository)
		{
			return new RepositoryEntity
			{
				ShaHead = repository.Head.Tip.Sha,
				Url = repository.Info.Path
			};
		}

		public static TreeEntryChangesEntity AsEntity(this TreeEntryChanges changes)
		{
			return new TreeEntryChangesEntity
			{
				PathNew = changes.Path,
				PathOld = changes.OldPath,
				Status = changes.Status
			};
		}

		public static DeveloperEntity AsEntity(this Signature signature, RepositoryEntity repositoryEntity = null)
		{
			return new DeveloperEntity
			{
				Email = signature.Email,
				Name = signature.Email,
				Repository = repositoryEntity
			};
		}

		public static CommitPairEntity AsEntity(this CommitPair pair, RepositoryEntity repositoryEntity = null)
		{
			return new CommitPairEntity
			{
				ChildCommit = pair.Child.AsEntity(repositoryEntity,
					pair.Child.Author.AsEntity(repositoryEntity)),
				ParentCommit = pair.Parent?.AsEntity(repositoryEntity,
					pair.Parent?.Author.AsEntity(repositoryEntity)),
				Repository = repositoryEntity
			};
		}

		public static CommitEntity AsEntity(this Commit commit, RepositoryEntity repositoryEntity = null, DeveloperEntity developerEntity = null)
		{
			return new CommitEntity
			{
				CommitDate = commit.Author.When.DateTime,
				HashSHA1 = commit.Sha,
				IsMergeCommit = commit.Parents.Count() > 1,
				Repository = repositoryEntity,
				Developer = developerEntity
			};
		}
	}
}
