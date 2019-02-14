/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using LibGit2Sharp;
using System;
using System.Linq;
using Util.Data.Entities;
using Util.Density;

namespace Util.Extensions
{
	/// <summary>
	/// A class with the sole purpose of hosting extension methods for types such as
	/// <see cref="Repository"/>, <see cref="TreeEntryChanges"/>, <see cref="Signature"/>, <see cref="CommitPair"/> or <see cref="Commit"/>.
	/// </summary>
	public static class EntityExtensions
	{
		/// <summary>
		/// Creates and returns a new <see cref="RepositoryEntity"/> from this <see cref="Repository"/>.
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="gitCommitSpan"></param>
		/// <returns></returns>
		public static RepositoryEntity AsEntity(this Repository repository, GitCommitSpan gitCommitSpan)
		{
			return new RepositoryEntity
			{
				BaseObject = repository,
				Url = repository.Info.Path,
				SinceCommitSha1 = gitCommitSpan.FilteredCommits.First().Sha,
				UntilCommitSha1 = gitCommitSpan.FilteredCommits.Last().Sha
			};
		}

		/// <summary>
		/// Creates and returns a new <see cref="TreeEntryChangesEntity"/> from this <see cref="TreeEntryChanges"/>.
		/// </summary>
		/// <param name="changes"></param>
		/// <param name="commitPairEntity"></param>
		/// <param name="addToCommitPair"></param>
		/// <returns></returns>
		public static TreeEntryChangesEntity AsEntity(this TreeEntryChanges changes, CommitPairEntity commitPairEntity, Boolean addToCommitPair = true)
		{
			var entity = new TreeEntryChangesEntity
			{
				BaseObject = changes,
				PathNew = changes.Path,
				PathOld = changes.OldPath,
				Status = changes.Status,
				CommitPair = commitPairEntity
			};

			if (addToCommitPair && commitPairEntity is CommitPairEntity)
			{
				commitPairEntity.AddTreeEntryChanges(entity);
			}

			return entity;
		}

		/// <summary>
		/// Creates and returns a new <see cref="DeveloperEntity"/> from this <see cref="Signature"/>.
		/// </summary>
		/// <param name="signature"></param>
		/// <param name="repositoryEntity"></param>
		/// <param name="addToRepository"></param>
		/// <returns></returns>
		public static DeveloperEntity AsEntity(this Signature signature, RepositoryEntity repositoryEntity = null, Boolean addToRepository = true)
		{
			var entity = new DeveloperEntity
			{
				BaseObject = signature,
				Email = signature.Email,
				Name = signature.Email,
				Repository = repositoryEntity
			};

			if (addToRepository && repositoryEntity is RepositoryEntity)
			{
				repositoryEntity.AddDeveloper(entity);
			}

			return entity;
		}

		/// <summary>
		/// Creates and returns a new <see cref="CommitPairEntity"/> from this <see cref="CommitPair"/>.
		/// </summary>
		/// <param name="pair"></param>
		/// <param name="repositoryEntity"></param>
		/// <param name="childCommitEntity"></param>
		/// <param name="parentCommitEntity"></param>
		/// <param name="addToRepository"></param>
		/// <param name="addCommitsToRepository"></param>
		/// <returns></returns>
		public static CommitPairEntity AsEntity(this CommitPair pair, RepositoryEntity repositoryEntity = null, CommitEntity childCommitEntity = null, CommitEntity parentCommitEntity = null, bool addToRepository = true, bool addCommitsToRepository = true)
		{
			var entity = new CommitPairEntity
			{
				BaseObject = pair,
				ID = pair.Id,
				ChildCommit = childCommitEntity,
				ParentCommit = parentCommitEntity,
				Repository = repositoryEntity
			};

			if (repositoryEntity is RepositoryEntity)
			{
				if (addToRepository)
				{
					repositoryEntity.AddCommitPair(entity);
				}
				if (addCommitsToRepository)
				{
					if (entity.ChildCommit is CommitEntity)
					{
						repositoryEntity.AddCommit(entity.ChildCommit);
					}
					if (entity.ParentCommit is CommitEntity)
					{
						repositoryEntity.AddCommit(entity.ParentCommit);
					}
				}
			}

			return entity;
		}

		/// <summary>
		/// Creates and returns a new <see cref="CommitEntity"/> from this <see cref="Commit"/>.
		/// </summary>
		/// <param name="commit"></param>
		/// <param name="repositoryEntity"></param>
		/// <param name="developerEntity"></param>
		/// <param name="addToRepository"></param>
		/// <param name="addToDeveloper"></param>
		/// <returns></returns>
		public static CommitEntity AsEntity(this Commit commit, RepositoryEntity repositoryEntity = null, DeveloperEntity developerEntity = null, Boolean addToRepository = true, Boolean addToDeveloper = true)
		{
			var entity = new CommitEntity
			{
				BaseObject = commit,
				CommitDate = commit.Committer.When.UtcDateTime,
				HashSHA1 = commit.Sha,
				IsMergeCommit = commit.Parents.Count() > 1,
				Repository = repositoryEntity,
				Developer = developerEntity
			};

			if (addToRepository && repositoryEntity is RepositoryEntity)
			{
				repositoryEntity.AddCommit(entity);
			}
			if (addToDeveloper && developerEntity is DeveloperEntity)
			{
				developerEntity.AddCommit(entity);
			}

			return entity;
		}
	}
}
