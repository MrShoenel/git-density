/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using Renci.SshNet.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Util.Data.Entities;
using Util.Density;
using Util.Metrics;
using Signature = LibGit2Sharp.Signature;

namespace Util.Extensions
{
    public static class RepositoryExtensions
	{
		public enum SortOrder
		{
			LatestFirst,
			OldestFirst
		}

		/// <summary>
		/// Returns pairs of commits as <see cref="CommitPair"/>s. Such a pair can be used
		/// to compute the differences between the selected commits. A pair consists of a
		/// commit and its direct parent.
		/// </summary>
		/// <param name="gitCommitSpan">A commit span to delimit the range of returned pairs
		/// of commits.</param>
		/// <param name="skipInitialCommit">If true, then the initial commit will be discarded
		/// and not be included in any pair. This might be useful, because the initial commit
		/// consists only of new files.</param>
		/// <param name="skipMergeCommits">If true, then merge-commits (commits with more than
		/// one parent) will be ignored as children, however, they will appear as parents to
		/// subsequent commits. This setting can be useful to ignore changesets that have already
		/// been analyzed using prior commits. If this is set to false and a commit has more
		/// than one parent, then the first parent is used to return a pair. Even if set to true,
		/// this method will NOT return a separate pair for each parent.</param>
		/// <param name="sortOrder">If <see cref="SortOrder.OldestFirst"/>, the first pair returned
		/// contains the initial commit (if not skipped) and null as a parent.</param>
		/// <returns>An <see cref="IEnumerable{CommitPair}"/> of pairs of commits.</returns>
		public static IEnumerable<CommitPair> CommitPairs(this GitCommitSpan gitCommitSpan, bool skipInitialCommit = false, bool skipMergeCommits = true, SortOrder sortOrder = SortOrder.OldestFirst)
		{
			var commits = (sortOrder == SortOrder.LatestFirst ?
				gitCommitSpan.FilteredCommits.Reverse() : gitCommitSpan.FilteredCommits).ToList();

			foreach (var commit in commits)
			{
				var parentCount = commit.Parents.Count();
				var isInitialCommit = parentCount == 0
					|| (sortOrder == SortOrder.OldestFirst && commit == commits[0] && commits.Count > 1)
					|| (sortOrder == SortOrder.LatestFirst && commit == commits.Last() && commits.Count > 1);

				if (skipInitialCommit && isInitialCommit)
				{
					continue;
				}

				if (skipMergeCommits && parentCount > 1)
				{
					continue;
				}


				// Note that the parent can be null, if initial commit is not skipped.
				yield return new CommitPair(gitCommitSpan.Repository,
					commit, isInitialCommit ? null : commit.Parents.FirstOrDefault());
			}
		}

		/// <summary>
		/// Returns a <see cref="StreamReader"/> based on <see cref="Blob"/> of
		/// the <see cref="TreeEntry"/> that can be used to obtain its contents.
		/// </summary>
		/// <param name="treeEntry"></param>
		/// <returns></returns>
		public static StreamReader GetReader(this TreeEntry treeEntry)
		{
			var contentStream = (treeEntry.Target as Blob).GetContentStream();
			return new StreamReader(contentStream);
		}

		/// <summary>
		/// Using <see cref="GetReader(TreeEntry)"/> to obtain a reader, this method
		/// returns every line that represents the content of this <see cref="TreeEntry"/>.
		/// </summary>
		/// <param name="treeEntry"></param>
		/// <returns></returns>
		public static IEnumerable<String> GetLines(this TreeEntry treeEntry)
		{
			using (var reader = treeEntry.GetReader())
			{
				String line;
				while ((line = reader.ReadLine()) != null) {
					yield return line;
				}
			}
		}

		/// <summary>
		/// Returns a metric of type <see cref="SimpleLoc"/> for the file represented
		/// by this <see cref="TreeEntry"/>.
		/// </summary>
		/// <param name="treeEntry"></param>
		/// <returns></returns>
		public static SimpleLoc GetSimpleLoc(this TreeEntry treeEntry)
		{
			return new SimpleLoc(treeEntry.GetLines());
		}

		/// <summary>
		/// Writes a <see cref="TreeEntry"/> with its relative path into the target
		/// directory. Recursively creates all required directories.
		/// </summary>
		/// <param name="treeEntry"></param>
		/// <param name="targetDirectory"></param>
		/// <returns>An awaitable <see cref="Task"/></returns>
		public static async Task WriteOutTreeEntry(this TreeEntry treeEntry, DirectoryInfo targetDirectory)
		{
			using (var contentStream = (treeEntry.Target as Blob).GetContentStream())
			{
				var targetPath = Path.Combine(targetDirectory.FullName, treeEntry.Path);
				var targetDir = new DirectoryInfo(
					Path.Combine(targetDirectory.FullName, Path.GetDirectoryName(treeEntry.Path)));

				if (!targetDir.Exists) { targetDir.Create(); }

				using (var fs = new FileStream(targetPath, FileMode.Create))
				{
					await contentStream.CopyToAsync(fs);
				}
			}
		}

		/// <summary>
		/// Only used in <see cref="GroupByDeveloper(IEnumerable{Commit}, RepositoryEntity, bool)"/>.
		/// </summary>
		private class CommitGroupingByDeveloper : IGrouping<DeveloperWithAlternativeNamesAndEmails, Commit>
		{
			public DeveloperWithAlternativeNamesAndEmails Key { get; private set; }

			private ICollection<Commit> items;

			internal CommitGroupingByDeveloper(DeveloperWithAlternativeNamesAndEmails key, IEnumerable<Commit> ie)
			{
				this.Key = key;
				this.items = new LinkedList<Commit>(ie);
			}

			public IEnumerator<Commit> GetEnumerator()
			{
				return this.items.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
		}

		/// <summary>
		/// An extension to the <see cref="DeveloperEntity"/> that holds additional,
		/// alternative names and email addresses. This is necessary, as developers
		/// sometimes uses (slightly) different names or email addresses. Also, sometimes
		/// either of these is missing.
		/// </summary>
		public class DeveloperWithAlternativeNamesAndEmails : DeveloperEntity, IEquatable<DeveloperWithAlternativeNamesAndEmails>
		{
			private ISet<String> altNames;
			public virtual IReadOnlyCollection<String> AlternativeNames {
				get { return this.altNames.ToList().AsReadOnly(); } }

			private ISet<String> altEmails;
			public virtual IReadOnlyCollection<String> AlternativeEmails {
				get { return this.altEmails.ToList().AsReadOnly(); } }

			public DeveloperWithAlternativeNamesAndEmails() : base()
			{
				this.altNames = new HashSet<String>();
				this.altEmails = new HashSet<String>();
			}

			public virtual void AddName(String name)
			{
				if (name != this.Name)
				{
					this.altNames.Add(name);
				}
			}

			public virtual void AddEmail(String email)
			{
				if (email != this.Email)
				{
					this.altEmails.Add(email);
				}
			}

			/// <summary>
			/// Concatenates this name, email and alternative names and emails to one big string.
			/// Then returns the SHA256 hash of it (<see cref="StringExtensions.SHA256hex(string)"/>).
			/// </summary>
			public virtual String SHA256Hash
			{
				get
				{
					var nameEmail = this.Name.AsEnumerable().Concat(this.Email.AsEnumerable());

					var allNamesAndAddresses = String.Join(",",
						nameEmail.Concat(this.AlternativeNames).Concat(this.AlternativeEmails)
							.Select(n => n ?? String.Empty)
							.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

					return allNamesAndAddresses.SHA256hex();
				}
			}

			#region equality, hashing etc.
			/// <summary>
			/// Returns <see cref="DeveloperEntity.GetHashCode"/> xor'red with the sha256-hash
			/// of this object, using <see cref="SHA256Hash"/>.
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				return base.GetHashCode() ^ this.SHA256Hash.GetHashCode();
			}

			/// <summary>
			/// First checks <see cref="DeveloperEntity.Equals(DeveloperEntity)"/> and conditionally
			/// continues to compare the sets of alternative names and emails (whether they contain
			/// the same strings or not).
			/// </summary>
			/// <param name="other"></param>
			/// <returns></returns>
			public virtual bool Equals(DeveloperWithAlternativeNamesAndEmails other)
			{
				if (!base.Equals(other))
				{
					return false;
				}

				return ((HashSet<String>)this.altNames).SetEquals(other.AlternativeNames)
					&& ((HashSet<String>)this.altEmails).SetEquals(other.AlternativeEmails);
			}

			/// <summary>
			/// Overridden to work explicitly with objects of type <see cref="DeveloperWithAlternativeNamesAndEmails"/>.
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public override bool Equals(object obj)
			{
				return obj is DeveloperWithAlternativeNamesAndEmails
					&& this.Equals(obj as DeveloperWithAlternativeNamesAndEmails);
			}
			#endregion
		}

		/// <summary>
		/// Groups an <see cref="IEnumerable{Commit}"/> into groups where the key is a
		/// <see cref="DeveloperWithAlternativeNamesAndEmails"/>. This is better than
		/// grouping developers/authors just by name or email, as it avoids redundancies.
		/// The <see cref="Commit"/>s are ordered from oldest to newest to retain a de-
		/// terministic developer-entity (where the name and email represent the first
		/// used identities and the alternatives are those that had been used later).
		/// </summary>
		/// <param name="commits"></param>
		/// <param name="repository">Used to initialize the DeveloperEntity.</param>
		/// <param name="useAuthorAndNotCommitter">Defaults to true. If true, will use
		/// the <see cref="Commit"/>'s <see cref="Commit.Author"/> to create the groups.
		/// Otherwise, the <see cref="Commit.Committer"/> is used. Historically, the
		/// behavior of this function used the Author as the developer.</param>
		/// <returns>Enumerables of groups, where the key is the developer and the enumerable
		/// group itself represents their commits.</returns>
		public static IEnumerable<IGrouping<DeveloperWithAlternativeNamesAndEmails, Commit>> GroupByDeveloper(this IEnumerable<Commit> commits, RepositoryEntity repository = null, bool useAuthorAndNotCommitter = true)
		{
			var byNameDict = new Dictionary<String, DeveloperEntity>();
			var byMailDict = new Dictionary<String, DeveloperEntity>();
			var authorOrCommiterSelector = new Func<Commit, Signature>(
				c => useAuthorAndNotCommitter ? c.Author : c.Committer);

			var emptyDev = new DeveloperEntity {
				Name = String.Empty, Email = String.Empty, Repository = repository };
			var dict = new Dictionary<DeveloperEntity, ICollection<Signature>>();
			var dictAlts = new Dictionary<DeveloperEntity, Tuple<HashSet<String>, HashSet<String>>>();

			foreach (var signature in commits.Select(authorOrCommiterSelector).Where(sig => sig is Signature).OrderBy(sig => sig.When.UtcDateTime))
			{
				var name = String.IsNullOrEmpty(signature.Name) ? String.Empty :
					signature.Name.Trim().ToLowerInvariant();
				var mail = String.IsNullOrEmpty(signature.Email) ? String.Empty :
					signature.Email.Trim().ToLowerInvariant();

				DeveloperEntity devEntity;

				if (name == String.Empty && mail == String.Empty)
				{
					devEntity = emptyDev; // Special case
				}
				else if (name == String.Empty)
				{
					if (byMailDict.ContainsKey(mail))
					{
						devEntity = byMailDict[mail];
					}
					else
					{
						devEntity = byMailDict[mail] = new DeveloperEntity
						{ BaseObject = signature, Name = String.Empty, Email = signature.Email, Repository = repository };
					}
				}
				else if (mail == String.Empty)
				{
					if (byNameDict.ContainsKey(name))
					{
						devEntity = byNameDict[name];
					}
					else
					{
						devEntity = byNameDict[name] = new DeveloperEntity
						{ BaseObject = signature, Name = signature.Name, Email = String.Empty, Repository = repository };
					}
				}
				else
				{
					// Both are present, give precedence to email
					if (byMailDict.ContainsKey(mail))
					{
						devEntity = byMailDict[mail];
					}
					else if (byNameDict.ContainsKey(name))
					{
						devEntity = byNameDict[name];
					}
					else
					{
						// new entity, add to both dictionaries
						devEntity = new DeveloperEntity
						{ BaseObject = signature, Name = signature.Name, Email = signature.Email, Repository = repository };
						byMailDict[mail] = devEntity;
						byNameDict[name] = devEntity;
					}
				}

				if (!dict.ContainsKey(devEntity))
				{
					dict[devEntity] = new List<Signature>();
					dictAlts[devEntity] = Tuple.Create(new HashSet<String>(), new HashSet<String>());
				}

				dict[devEntity].Add(signature);
				if (name != String.Empty)
				{
					dictAlts[devEntity].Item1.Add(signature.Name);
				}
				if (mail != String.Empty)
				{
					dictAlts[devEntity].Item2.Add(signature.Email);
				}
			}

			foreach (var devEntityKv in dict)
			{
				// We can only construct the DeveloperWithAlternativeNamesAndEmails now, as its
				// hashcode would have changed by adding more names or emails to it. That is why
				// those were kept in separate collections in the dictAlts-dictionary.
				var devWithAlts = new DeveloperWithAlternativeNamesAndEmails()
				{
					Name = devEntityKv.Key.Name,
					Email = devEntityKv.Key.Email
				};
				dictAlts[devEntityKv.Key].Item1.ToList().ForEach(name => devWithAlts.AddName(name));
				dictAlts[devEntityKv.Key].Item2.ToList().ForEach(mail => devWithAlts.AddEmail(mail));

				var devCommits = commits.Where(c => dict[devEntityKv.Key].Contains(
					useAuthorAndNotCommitter ? c.Author : c.Committer));

				yield return new CommitGroupingByDeveloper(devWithAlts, devCommits);
			}
		}

		/// <summary>
		/// Similar to and based on <see cref="GroupByDeveloper(IEnumerable{Commit}, RepositoryEntity, bool)"/>,
		/// this method returns a collection of <see cref="Signature"/>s for each unified developer
		/// represented as <see cref="DeveloperWithAlternativeNamesAndEmails"/>.
		/// </summary>
		/// <param name="commits"></param>
		/// <param name="repository"></param>
		/// <param name="useAuthorAndNotCommitter">See documentation at <see cref="GroupByDeveloper(IEnumerable{Commit}, RepositoryEntity, bool)"/></param>
		/// <returns>A dictionary where each developer has a set of signatures used.</returns>
		public static IDictionary<DeveloperWithAlternativeNamesAndEmails, ISet<Signature>> GroupByDeveloperToSignatures(this IEnumerable<Commit> commits, RepositoryEntity repository = null, bool useAuthorAndNotCommitter = true)
		{
			var dict = new Dictionary<DeveloperWithAlternativeNamesAndEmails, ISet<Signature>>();

			foreach (var group in commits.GroupByDeveloper(repository, useAuthorAndNotCommitter))
			{
				group.Key.Repository = repository;
				dict[group.Key] = new HashSet<Signature>(group.Select(commit =>
					useAuthorAndNotCommitter ? commit.Author : commit.Committer));
			}

			return dict;
		}

		/// <summary>
		/// Similar to <see cref="GroupByDeveloperToSignatures(IEnumerable{Commit}, RepositoryEntity, bool)"/>, this
		/// method returns a mapping from each <see cref="LibGit2Sharp.Signature"/> to a
		/// <see cref="DeveloperEntity"/>. More than one <see cref="LibGit2Sharp.Signature"/>
		/// can point to the same <see cref="DeveloperEntity"/>.
		/// </summary>
		/// <param name="commits"></param>
		/// <param name="repository"></param>
		/// <param name="useAuthorAndNotCommitter">See documentation at <see cref="GroupByDeveloperToSignatures(IEnumerable{Commit}, RepositoryEntity, bool)"/></param>
		/// <returns></returns>
		public static IDictionary<Signature, DeveloperWithAlternativeNamesAndEmails> GroupByDeveloperAsSignatures(this IEnumerable<Commit> commits, RepositoryEntity repository = null, bool useAuthorAndNotCommitter = true)
		{
			var fromDict = commits.GroupByDeveloperToSignatures(repository, useAuthorAndNotCommitter);
			var toDict = new Dictionary<Signature, DeveloperWithAlternativeNamesAndEmails>();

			foreach (var fromKv in fromDict)
			{
				fromKv.Key.Repository = repository;
				foreach (var signature in fromKv.Value)
				{
					toDict[signature] = fromKv.Key;
				}
			}

			return toDict;
		}

		private static readonly FieldInfo fieldPatchStringBuilder =
			typeof(Patch).GetField("fullPatchBuilder", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// A <see cref="Patch"/> is currently not disposable, so we use this
		/// little helper method to clear its internal <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="setStringBuilderNull">If true, sets the builder to null,
		/// after having cleared it.</param>
		/// <param name="patch"></param>
		public static void Clear(this Patch patch, Boolean setStringBuilderNull = false)
		{
			var sb = (StringBuilder)fieldPatchStringBuilder.GetValue(patch);
			sb.Clear();
			if (setStringBuilderNull)
			{
				fieldPatchStringBuilder.SetValue(patch, null);
			}
		}

		private static readonly FieldInfo fieldContentChangesStringBuilder =
			typeof(PatchEntryChanges).BaseType.GetField(
				"patchBuilder", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Similar to <see cref="Clear(Patch, bool)"/>, but works on instances of
		/// <see cref="PatchEntryChanges"/>, which derive from <see cref="ContentChanges"/>,
		/// that have a <see cref="StringBuilder"/> inside.
		/// </summary>
		/// <param name="pec"></param>
		/// <param name="setStringBuilderNull"></param>
		public static void Clear(this PatchEntryChanges pec, Boolean setStringBuilderNull = false)
		{
			var sb = (StringBuilder)fieldContentChangesStringBuilder.GetValue(pec);
			sb.Clear();
			if (setStringBuilderNull)
			{
				fieldContentChangesStringBuilder.SetValue(pec, null);
			}
		}


		/// <summary>
		/// Collection of identical, yet differently located repositories. This is
		/// useful for when concurrent, yet mutually exclusive operations on a single
		/// <see cref="LibGit2Sharp.Repository"/> need to be performed.
		/// </summary>
		public class RepositoryBundleCollection : LoanCollection<Repository>
		{
			/// <summary>
			/// A reference to the original <see cref="LibGit2Sharp.Repository"/> that can
			/// be used to add instances to this collection.
			/// </summary>
			public Repository Repository { get; private set; }

			/// <summary>
			/// If true, then, on dispose of this collection, all items owned
			/// by it will also be disposed by calling their <see cref="Repository.Dispose"/> method.
			/// </summary>
			public virtual Boolean DeleteClonedReposAfterwards { get; set; }

			/// <summary>
			/// Creates a new bundle collection.
			/// </summary>
			/// <param name="repository"></param>
			/// <param name="deleteClonedReposAfterwards"></param>
			public RepositoryBundleCollection(Repository repository, bool deleteClonedReposAfterwards = true)
				: base()
			{
				this.Repository = repository;
				this.DeleteClonedReposAfterwards = deleteClonedReposAfterwards;
			}

			#region Dispose
			private readonly Object disposeLock = new Object();
			/// <summary>
			/// Disposes this collection and conditionally all its items, by deleting
			/// the repositories on disk first and then disposing the related object.
			/// Lastly, calls <see cref="LoanCollection{T}.Dispose"/>.
			/// </summary>
			/// <param name="disposing"></param>
			protected override sealed void Dispose(bool disposing)
			{
				lock (this.disposeLock)
				{
					if (!this.wasDisposed)
					{
						if (disposing)
						{
							foreach (var loanableItem in this)
							{
								try
								{
									if (this.DeleteClonedReposAfterwards)
									{
										var repo = loanableItem.Item;
										var repoPath = repo.Info.WorkingDirectory;
										repo.Dispose();
										new DirectoryInfo(repoPath).TryDelete();
									}
								}
								catch { }
							}
						}

						base.Dispose(disposing);
					}
				}
			}
			#endregion
		}

		/// <summary>
		/// Create a collection of identical repositories that are differently
		/// located, so that mutually exclusive concurrent operations can be
		/// carried out on each instance independently. Uses <see cref="BundleAndCloneTo(Repository, string)"/>
		/// to create identical instances. See <see cref="RepositoryBundleCollection"/> and <see cref="LoanCollection{T}"/>.
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="targetPath"></param>
		/// <param name="numInstances"></param>
		/// <param name="deleteClonedReposAfterwards">Defaults to true. Upon disposal of the collection, all the repositories will be destroyed.</param>
		/// <returns></returns>
		public static RepositoryBundleCollection CreateBundleCollection(
			this Repository repository, String targetPath, UInt16 numInstances, Boolean deleteClonedReposAfterwards = true)
		{
			var rbc = new RepositoryBundleCollection(
				repository, deleteClonedReposAfterwards: deleteClonedReposAfterwards);

			Parallel.ForEach(Enumerable.Range(0, numInstances), _ =>
			{
				rbc.Add(repository.BundleAndCloneTo(targetPath));
			});

			return rbc;
		}

		/// <summary>
		/// Bundles this <see cref="Repository"/> to a single file (a git bundle), moves
		/// the file to the target-path and un-bundles (clones from) it. Then removes the
		/// bundle file, opens the new copy of the repository and returns it.
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> to clone.</param>
		/// <param name="targetPath">The path to clone the new repository to.</param>
		/// <returns>The <see cref="Repository"/> that was opened on the new path.</returns>
		public static Repository BundleAndCloneTo(this Repository repository, String targetPath)
		{
			var id = Guid.NewGuid().ToString();
			var bundleName = $"{id}.bundle";

			using (var proc = Process.Start(new ProcessStartInfo {
				FileName = "git",
				Arguments = $"bundle create {bundleName} --all",
				WorkingDirectory = repository.Info.WorkingDirectory,
				WindowStyle = ProcessWindowStyle.Hidden
			}))
			{
				proc.WaitForExit();
			}

			File.Move(
				Path.Combine(repository.Info.WorkingDirectory, bundleName),
				Path.Combine(targetPath, bundleName));

			using (var proc = Process.Start(new ProcessStartInfo {
				FileName = "git",
				Arguments = $"clone {bundleName}",
				WorkingDirectory = targetPath,
				WindowStyle = ProcessWindowStyle.Hidden
			}))
			{
				proc.WaitForExit();
			}

			// Delete the bundle after cloning:
			File.Delete(Path.Combine(targetPath, bundleName));

			var clonedRepoPath = Path.Combine(targetPath, id);
			return clonedRepoPath.OpenRepository();
		}

		/// <summary>
		/// Obtain all commits from all branches.
		/// </summary>
		/// <param name="repository"></param>
		/// <returns>An <see cref="ISet{Commit}"/> with all the repository's commits.</returns>
		public static ISet<Commit> GetAllCommits(this Repository repository)
		{
            return repository.Commits
				.QueryBy(new CommitFilter { IncludeReachableFrom = repository.Refs.ToList() })
				.Distinct<Commit>(EqualityComparer<GitObject>.Default)
				.ToHashSet();
        }

		/// <summary>
		/// Obtains all commits of a repository and goes to the latests, as determined
		/// by <see cref="Commit.Committer"/>.
		/// </summary>
		/// <see cref="GetAllCommits(Repository)"/>
		/// <param name="repository"></param>
		/// <returns>The same <see cref="Repository"/> with the latest <see cref="Commit"/>
		/// being checked out.</returns>
		public static Repository CheckoutLatestCommit(this Repository repository)
		{
			Commands.Checkout(repository,
				repository.GetAllCommits().OrderByDescending(c => c.Committer.When.UtcDateTime).First());

			return repository;
		}

		/// <summary>
		/// Creates a short SHA1-string (default is 7 characters long) for a Commit.
		/// </summary>
		/// <param name="commit"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static String ShaShort(this Commit commit, int length = 7)
		{
			if (length < 4 || length > 15)
			{
				throw new ArgumentOutOfRangeException($"{nameof(length)}: {length}");
			}
			return commit.Sha.Substring(0, length);
		}

		/// <summary>
		/// Uses <see cref="Repository.Lookup(string)"/> to find objects by some ID.
		/// If an object is not found, no entity is returned for it, instead of
		/// throwing an error.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="repo"></param>
		/// <param name="objectish"></param>
		/// <returns></returns>
		public static IEnumerable<T> LookupAny<T>(this Repository repo, IEnumerable<string> objectish) where T : GitObject
		{
			return objectish.Select(o => repo.Lookup<T>(o)).Where(o => o is T);
		}
	}
}
