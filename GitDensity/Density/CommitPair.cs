using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Density
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>If disposed and the <see cref="Patch"/> has been
	/// accessed, will dispose the <see cref="Patch"/>.</remarks>
	public sealed class CommitPair : IDisposable, IEquatable<CommitPair>
	{
		private Tuple<Commit, Commit> pair;

		/// <summary>
		/// The child-commit represents the next commit on the same branch
		/// that springs off its parent (i.e. it is younger/newer).
		/// </summary>
		public Commit Child { get { return this.pair.Item1; } }

		/// <summary>
		/// The parent-commit is the ancestor on the same branch that
		/// directly precedes the <see cref="Child"/>.
		/// </summary>
		public Commit Parent { get { return this.pair.Item2; } }

		/// <summary>
		/// Returns a string that uniquely identifies this pair. The string
		/// is guaranteed not to be longer than 32 characters and contains
		/// sub-strings of the parent- and child-commit's SHA1-IDs.
		/// </summary>
		public String Id
		{
			get => $"{this.Parent.Sha.Substring(0, 15)}_{Child.Sha.Substring(0, 15)}";
		}

		private Lazy<Patch> lazyPatch;

		/// <summary>
		/// The <see cref="Patch"/> that represents the difference between
		/// parent- and child-commit. Computed using <see cref="Repository.Diff"/>.
		/// </summary>
		public Patch Patch { get { return this.lazyPatch.Value; } }

		private Lazy<TreeChanges> lazyTree;

		/// <summary>
		/// The <see cref="TreeChanges"/> that represent the difference
		/// between the two file-system trees of both commits, computed
		/// using <see cref="Repository.Diff"/>.
		/// </summary>
		public TreeChanges TreeChanges { get { return this.lazyTree.Value; } }

		/// <summary>
		/// C'tor; initializes the pair and its patch.
		/// </summary>
		/// <param name="diff"></param>
		/// <param name="child"></param>
		/// <param name="parent"></param>
		internal CommitPair(Diff diff, Commit child, Commit parent = null)
		{
			this.pair = Tuple.Create(child, parent);

			this.lazyPatch = new Lazy<Patch>(() =>
			{
				return diff.Compare<Patch>(this.Parent?.Tree, this.Child.Tree);
			});

			this.lazyTree = new Lazy<TreeChanges>(() =>
			{
				return diff.Compare<TreeChanges>(this.Parent?.Tree, this.Child.Tree);
			});
		}

		/// <summary>
		/// Retrieves a <see cref="CommitPair"/> by ID, where the ID has been
		/// obtained from <see cref="CommitPair.Id"/> (and follows its format).
		/// </summary>
		/// <param name="commitPairId"></param>
		/// <param name="repository"></param>
		/// <returns></returns>
		public static CommitPair FromId(String commitPairId, Repository repository)
		{
			var parentId = commitPairId.Split('_')[0];
			var childId = commitPairId.Split('_')[1];

			var parentCommit = repository.Commits.Where(c => c.Sha.StartsWith(parentId)).Single();
			var childCommit = repository.Commits.Where(c => c.Sha.StartsWith(childId)).Single();

			return new CommitPair(repository.Diff, childCommit, parentCommit);
		}

		#region Equals
		public bool Equals(CommitPair other)
		{
			return this.pair.Equals(other);
		}
		#endregion

		#region Dispose
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.lazyPatch.IsValueCreated)
				{
					this.lazyPatch.Value.Dispose();
				}
				if (this.lazyTree.IsValueCreated)
				{
					this.lazyTree.Value.Dispose();
				}
			}
		}
		#endregion
	}
}
