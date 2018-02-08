/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the MIT-license.
///
/// ---------------------------------------------------------------------------------
///
/// Permission is hereby granted, free of charge, to any person obtaining a
/// copy of this software and associated documentation files (the "Software"),
/// to deal in the Software without restriction, including without limitation
/// the rights to use, copy, modify, merge, publish, distribute, sublicense,
/// and/or sell copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
/// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
	public static class EntityExtensions
	{
		public static RepositoryEntity AsEntity(this Repository repository, GitHoursSpan gitHoursSpan)
		{
			return new RepositoryEntity
			{
				Url = repository.Info.Path,
				SinceCommitSha1 = gitHoursSpan.FilteredCommits.First().Sha,
				UntilCommitSha1 = gitHoursSpan.FilteredCommits.Last().Sha
			};
		}

		public static TreeEntryChangesEntity AsEntity(this TreeEntryChanges changes, CommitPairEntity commitPairEntity, Boolean addToCommitPair = true)
		{
			var entity = new TreeEntryChangesEntity
			{
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

		public static DeveloperEntity AsEntity(this Signature signature, RepositoryEntity repositoryEntity = null, Boolean addToRepository = true)
		{
			var entity = new DeveloperEntity
			{
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

		public static CommitPairEntity AsEntity(this CommitPair pair, RepositoryEntity repositoryEntity = null, CommitEntity childCommitEntity = null, CommitEntity parentCommitEntity = null, bool addToRepository = true, bool addCommitsToRepository = true)
		{
			var entity = new CommitPairEntity
			{
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

		public static CommitEntity AsEntity(this Commit commit, RepositoryEntity repositoryEntity = null, DeveloperEntity developerEntity = null, Boolean addToRepository = true, Boolean addToDeveloper = true)
		{
			var entity = new CommitEntity
			{
				CommitDate = commit.Author.When.DateTime,
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
