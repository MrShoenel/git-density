using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using Util.Data.Entities;
using Util.Density;
using Util.Metrics;

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
		/// <param name="repo">The <see cref="Repository"/> to pull the commits from.</param>
		/// <param name="skipInitialCommit">If true, then the initial commit will be discarded
		/// and not be included in any pair. This might be useful, because the initial commit
		/// consists only of new files.</param>
		/// <param name="skipMergeCommits">If true, then merge-commits (commits with more than
		/// one parent) will be ignored as children, however, they will appear as parents to
		/// subsequent commits. This setting can be useful to ignore changesets that have already
		/// been analyzed using prior commits.</param>
		/// <param name="sortOrder">If <see cref="SortOrder.OldestFirst"/>, the first pair returned
		/// contains the initial commit (if not skipped) and null as a parent.</param>
		/// <returns>An <see cref="IEnumerable{CommitPair}"/> of pairs of commits.</returns>
		public static IEnumerable<CommitPair> CommitPairs(this GitHoursSpan gitHoursSpan, bool skipInitialCommit = false, bool skipMergeCommits = true, SortOrder sortOrder = SortOrder.OldestFirst)
		{
			var commits = (sortOrder == SortOrder.LatestFirst ?
				gitHoursSpan.FilteredCommits.Reverse() : gitHoursSpan.FilteredCommits).ToList();

			foreach (var commit in commits)
			{
				var parentCount = commit.Parents.Count();
				var isInitialCommit = parentCount == 0
					|| (sortOrder == SortOrder.OldestFirst && commit == commits[0])
					|| (sortOrder == SortOrder.LatestFirst && commit == commits.Last());

				if (skipInitialCommit && isInitialCommit)
				{
					continue;
				}

				if (skipMergeCommits && parentCount > 1)
				{
					continue;
				}


				// Note that the parent can be null, if initial commit is not skipped.
				yield return new CommitPair(gitHoursSpan.Repository,
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
		/// Returns a metric of type <see cref="SimpleLOC"/> for the file represented
		/// by this <see cref="TreeEntry"/>.
		/// </summary>
		/// <param name="treeEntry"></param>
		/// <returns></returns>
		public static SimpleLOC GetSimpleLOC(this TreeEntry treeEntry)
		{
			return new SimpleLOC(treeEntry.GetLines());
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
			var contentStream = (treeEntry.Target as Blob).GetContentStream();
			var targetPath = Path.Combine(targetDirectory.FullName, treeEntry.Path);
			var targetDir = new DirectoryInfo(
				Path.Combine(targetDirectory.FullName, Path.GetDirectoryName(treeEntry.Path)));

			if (!targetDir.Exists) { targetDir.Create(); }

			using (var fs = new FileStream(targetPath, FileMode.Create))
			{
				await contentStream.CopyToAsync(fs);
			}
		}

		/// <summary>
		/// Only used in <see cref="GroupByDeveloper(IEnumerable{Commit})"/>.
		/// </summary>
		private class CommitGroupingByDeveloper : IGrouping<DeveloperWithAlternativeNamesAndEmails, Commit>
		{
			public DeveloperWithAlternativeNamesAndEmails Key { get; private set; }

			private IEnumerable<Commit> items;

			internal CommitGroupingByDeveloper(DeveloperWithAlternativeNamesAndEmails key, IEnumerable<Commit> ie)
			{
				this.Key = key;
				this.items = ie;
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
		public class DeveloperWithAlternativeNamesAndEmails : DeveloperEntity
		{
			private ISet<String> altNames;
			public IReadOnlyCollection<String> AlternativeNames {
				get { return this.altNames.ToList().AsReadOnly(); } }

			private ISet<String> altEmails;
			public IReadOnlyCollection<String> AlternativeEmails {
				get { return this.altEmails.ToList().AsReadOnly(); } }

			public DeveloperWithAlternativeNamesAndEmails() : base()
			{
				this.altNames = new HashSet<String>();
				this.altEmails = new HashSet<String>();
			}

			internal void AddName(String name)
			{
				if (name != this.Name)
				{
					this.altNames.Add(name);
				}
			}

			internal void AddEmail(String email)
			{
				if (email != this.Email)
				{
					this.altEmails.Add(email);
				}
			}
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
		/// <returns>Enumerables of groups, where the key is the developer and the enumerable
		/// group itself represents their commits.</returns>
		public static IEnumerable<IGrouping<DeveloperWithAlternativeNamesAndEmails, Commit>> GroupByDeveloper(this IEnumerable<Commit> commits)
		{
			var byNameDict = new Dictionary<String, DeveloperWithAlternativeNamesAndEmails>();
			var byMailDict = new Dictionary<String, DeveloperWithAlternativeNamesAndEmails>();

			var emptyDev = new DeveloperWithAlternativeNamesAndEmails { Name = String.Empty, Email = String.Empty };
			var dict = new Dictionary<DeveloperWithAlternativeNamesAndEmails, ICollection<Signature>>();

			foreach (var signature in commits.Select(c => c.Author).Where(sig => sig is Signature).OrderBy(c => c.When))
			{
				var name = String.IsNullOrEmpty(signature.Name) ? String.Empty :
					signature.Name.Trim().ToLowerInvariant();
				var mail = String.IsNullOrEmpty(signature.Email) ? String.Empty :
					signature.Email.Trim().ToLowerInvariant();

				DeveloperWithAlternativeNamesAndEmails devEntity;

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
						devEntity = byMailDict[mail] = new DeveloperWithAlternativeNamesAndEmails
						{ Name = String.Empty, Email = signature.Email };
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
						devEntity = byNameDict[name] = new DeveloperWithAlternativeNamesAndEmails
						{ Name = signature.Name, Email = String.Empty };
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
						devEntity = new DeveloperWithAlternativeNamesAndEmails
						{ Name = signature.Name, Email = signature.Email };
						byMailDict[mail] = devEntity;
						byNameDict[name] = devEntity;
					}
				}

				if (!dict.ContainsKey(devEntity))
				{
					dict[devEntity] = new List<Signature>();
				}

				dict[devEntity].Add(signature);
				if (name != String.Empty)
				{
					devEntity.AddName(signature.Name);
				}
				if (mail != String.Empty)
				{
					devEntity.AddEmail(signature.Email);
				}
			}


			foreach (var devEntityKv in dict)
			{
				yield return new CommitGroupingByDeveloper(devEntityKv.Key,
					commits.Where(c => dict[devEntityKv.Key].Contains(c.Author)));
			}
		}

		/// <summary>
		/// Similar to and based on <see cref="GroupByDeveloper(IEnumerable{Commit})"/>, this
		/// method returns a collection of <see cref="Signatures"/> for each unified developer
		/// represented as <see cref="DeveloperWithAlternativeNamesAndEmails"/>.
		/// </summary>
		/// <param name="commits"></param>
		/// <returns>A dictionary where each developer has a set of signatures used.</returns>
		public static IDictionary<DeveloperWithAlternativeNamesAndEmails, ISet<Signature>> GroupByDeveloperToSignatures(this IEnumerable<Commit> commits)
		{
			var dict = new Dictionary<DeveloperWithAlternativeNamesAndEmails, ISet<Signature>>();

			foreach (var group in commits.GroupByDeveloper())
			{
				dict[group.Key] = new HashSet<Signature>(group.Select(commit => commit.Author));
			}

			return dict;
		}

		/// <summary>
		/// Similar to <see cref="GroupByDeveloperToSignatures(IEnumerable{Commit})"/>, this
		/// method returns a mapping from each <see cref="LibGit2Sharp.Signature"/> to a
		/// <see cref="DeveloperEntity"/>. More than one <see cref="LibGit2Sharp.Signature"/>
		/// can point to the same <see cref="DeveloperEntity"/>.
		/// </summary>
		/// <param name="commits"></param>
		/// <returns></returns>
		public static IDictionary<Signature, DeveloperEntity> GroupByDeveloperAsSignatures(this IEnumerable<Commit> commits)
		{
			var fromDict = commits.GroupByDeveloperToSignatures();
			var toDict = new Dictionary<Signature, DeveloperEntity>();

			foreach (var fromKv in fromDict)
			{
				foreach (var signature in fromKv.Value)
				{
					toDict[signature] = fromKv.Key;
				}
			}

			return toDict;
		}
	}
}
