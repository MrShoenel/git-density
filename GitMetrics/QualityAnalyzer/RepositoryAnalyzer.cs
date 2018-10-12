/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitMetrics. All files in this project,
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
using Util.Extensions;
using Configuration = Util.Configuration;

namespace GitMetrics.QualityAnalyzer
{
	public class RepositoryAnalyzer : ISupportsExecutionPolicy
	{
		public Configuration Configuration { get; protected internal set; }

		public Repository Repository { get; protected internal set; }

		public ISet<Commit> Commits { get; protected internal set; }

		public ConcurrentBag<MetricsAnalysisResult> Results { get; protected internal set; }

		public RepositoryAnalyzer(Configuration configuration, Repository repository, IEnumerable<Commit> commits)
		{
			this.Configuration = configuration;
			this.Repository = repository;
			this.Commits = new HashSet<Commit>(commits);
			this.Results = new ConcurrentBag<MetricsAnalysisResult>();
		}

		public void Analyze()
		{
			String analyzerTypeName = null;

			if (String.IsNullOrEmpty(this.Configuration.UseMetricsAnalyzer))
			{
				if (AnalyzerImplementationsFQ.Count == 0)
				{
					throw new Exception($"No implementations of {nameof(IMetricsAnalyzer)} were found!");
				}
				analyzerTypeName = AnalyzerImplementationsFQ.First().Key;
			}
			else
			{
				analyzerTypeName = this.Configuration.UseMetricsAnalyzer;
			}


			var parallelOptions = new ParallelOptions();
			if (this.ExecutionPolicy == ExecutionPolicy.Linear)
			{
				parallelOptions.MaxDegreeOfParallelism = 1;
			}

			Parallel.ForEach(this.Commits, parallelOptions, commit => {
				// TODO: For each commit, bundle/un-bundle repo, checkout commit
				// Run analysis on it and store result in bag.

				var analyzer = CreateAnalyzer(analyzerTypeName, analyzerTypeName.Contains("."));
				this.Results.Add(analyzer.Analyze(this.Repository, commit));
			});
		}

		#region ISupportsExecPol
		public ExecutionPolicy ExecutionPolicy { get; set; }
		#endregion

		#region Analyzer Factory
		protected internal static Lazy<IReadOnlyDictionary<String, Type>> analyzerImpl
			= new Lazy<IReadOnlyDictionary<string, Type>>(() =>
			{
				var iface = typeof(IMetricsAnalyzer);

				return new ReadOnlyDictionary<String, Type>(AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(s => s.GetTypes())
					.Where(t => iface.IsAssignableFrom(t) && t.IsClass && !t.IsInterface && !t.IsAbstract)
					.ToDictionary(t => t.Name, t => t));
			});

		protected internal static Lazy<IReadOnlyDictionary<String, Type>> analyzerImplFQ
			= new Lazy<IReadOnlyDictionary<string, Type>>(() =>
			{
				return new ReadOnlyDictionary<String, Type>(AnalyzerImplementations
					.ToDictionary(kv => kv.Value.AssemblyQualifiedName, kv => kv.Value));
			});

		/// <summary>
		/// A dictionary with types that implement <see cref="IMetricsAnalyzer"/>. The keys were
		/// obtained using <see cref="System.Reflection.MemberInfo.Name"/>. If fully qualified
		/// type names are required, use <see cref="AnalyzerImplementationsFQ"/> instead.
		/// </summary>
		public static IReadOnlyDictionary<String, Type> AnalyzerImplementations { get => analyzerImpl.Value; }

		/// <summary>
		/// Same as <see cref="AnalyzerImplementations"/>, but the keys were built from calling
		/// <see cref="Type.FullName"/>, which results in fully-qualified names (i.e. the type's
		/// name is preceded by its namespace).
		/// </summary>
		public static IReadOnlyDictionary<String, Type> AnalyzerImplementationsFQ { get => analyzerImplFQ.Value; }

		/// <summary>
		/// Creates an instance of an <see cref="IMetricsAnalyzer"/> by its type name. Note that the
		/// type name must not be fully qualified (i.e. just the name without 
		/// </summary>
		/// <param name="typeName">A (fully qualified) name of a type that implements
		/// <see cref="IMetricsAnalyzer"/>.</param>
		/// <param name="nameIsFullyQualified">Should be set to true, iff the type's name was given
		/// fully qualified.</param>
		/// <returns></returns>
		public static IMetricsAnalyzer CreateAnalyzer(String typeName, Boolean nameIsFullyQualified = false)
		{
			var dict = nameIsFullyQualified ? AnalyzerImplementationsFQ : AnalyzerImplementations;

			if (!dict.ContainsKey(typeName))
			{
				throw new Exception($"There is no Analyzer with the name {typeName}.");
			}

			var type = dict[typeName];
			return (IMetricsAnalyzer)Activator.CreateInstance(type);
		}
		#endregion
	}
}
