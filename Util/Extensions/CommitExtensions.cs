using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Util.Logging;


namespace Util.Extensions
{
    /// <summary>
    /// A simple equality comparer for commit objects based in their ID
    /// and commit message.
    /// </summary>
    public class CommitEqualityComparer : IEqualityComparer<Commit>
    {
        /// <summary>
        /// True if the reference is the same or both Id and Message are equal.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(Commit x, Commit y)
        {
            if (ReferenceEquals(x, y)) return true;

            return x is Commit && y is Commit && x.Id == y.Id && x.Message == y.Message;
        }

        /// <summary>
        /// Hashcode based on the commit itself, its Id and Message.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(Commit obj)
        {
            return obj.GetHashCode() ^ obj.Id.GetHashCode() ^ obj.Message.GetHashCode();
        }
    }


    /// <summary>
    /// Static class with extensions for commits.
    /// </summary>
    public static class CommitExtensions
    {
        internal static readonly BaseLogger<Util> logger = ColoredConsole.CreateLogger<Util>();

        /// <summary>
        /// Traverses the tree of parent commits and returns up to N generations
        /// in a <see cref="HashSet{T}"/>. Note that within this tree, some commits
        /// can point to the same parent. Therefore, a set is returned.
        /// </summary>
        /// <param name="commit"></param>
        /// <param name="numGenerations"></param>
        /// <param name="primaryCommit"></param>
        /// <param name="originalNum"></param>
        /// <param name="allowIncompleteChains"></param>
        /// <exception cref="Exception">If parents for a parent-less commit were requested and
        /// allowIncompleteChains=false.</exception>
        /// <returns></returns>
        public static ISet<Commit> ParentGenerations(this Commit commit, UInt32 numGenerations, Commit primaryCommit = null, UInt32? originalNum = null, bool allowIncompleteChains = false)
        {
            var usePrimary = primaryCommit ?? commit;
            var useOrgNum = originalNum ?? numGenerations;
            var results = new HashSet<Commit>(comparer: new CommitEqualityComparer());
            if (numGenerations == 0)
            {
                return results;
            }


            var parents = commit.Parents.ToList();
            if (parents.Count == 0)
            {
                var msg = $"Cannot export {useOrgNum} parent generations. Commit with ID {usePrimary.ShaShort()} only has {useOrgNum - numGenerations} parent generation(s) along the chain that ends with commit {commit.ShaShort()}.";
                if (allowIncompleteChains)
                {
                    logger.LogCritical(msg);
                    return results;
                }
                throw new Exception(msg + " You may use the option --allow-incomplete-chains to enable too-short the export of too-short chains.");
            }

            results.AddAll(parents);

            // Let's decrease it. Since it's an UInt32, it was > 0 before (no overflow here).
            numGenerations--;
            if (numGenerations > 0)
            {
                results.AddAll(parents.SelectMany(parent =>
                    parent.ParentGenerations(numGenerations: numGenerations, primaryCommit: usePrimary, originalNum: useOrgNum, allowIncompleteChains: allowIncompleteChains)));
            }

            return results;
        }
    }
}
