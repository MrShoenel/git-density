﻿/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Util;
using Util.Data.Entities;
using Util.Extensions;
using Util.Logging;
using Configuration = Util.Configuration;

namespace GitMetrics.QualityAnalyzer
{
	public class RepositoryAnalyzer : ISupportsExecutionPolicy
	{
		/// <summary>
		/// Used for logging in this and deriving classes.
		/// </summary>
		protected internal static BaseLogger<RepositoryAnalyzer> logger
			= Program.CreateLogger<RepositoryAnalyzer>();

		/// <summary>
		/// For each commit to analyze, the bundled repository is cloned to a new directory.
		/// This copy of the repository is rather useless outside the bounds of
		/// <see cref="RepositoryAnalyzer"/> and can be safely deleted.
		/// </summary>
		public Boolean DeleteClonedRepoAfterwards { get; set; } = true;

		public Configuration Configuration { get; protected internal set; }

		public RepositoryEntity RepositoryEntity { get; protected internal set; }

		public Repository Repository { get => this.RepositoryEntity.BaseObject; }

		public ISet<CommitEntity> CommitEntities { get; protected internal set; }

		public ConcurrentBag<MetricsAnalysisResult> Results { get; protected internal set; }

		public String SelectedAnalyzerImplementation { get; protected internal set; }

		public RepositoryAnalyzer(Configuration configuration, RepositoryEntity repository, IEnumerable<CommitEntity> commits)
		{
			this.Configuration = configuration;
			this.RepositoryEntity = repository;
			this.CommitEntities = new HashSet<CommitEntity>(commits);
			this.Results = new ConcurrentBag<MetricsAnalysisResult>();
		}

		/// <summary>
		/// Needs to be called before the Analysis using <see cref="Analyze"/>
		/// is started.
		/// </summary>
		public void SelectAnalyzerImplementation()
		{
			String analyzerTypeName = null;

			if (String.IsNullOrEmpty(this.Configuration.UseMetricsAnalyzer))
			{
				if (AnalyzerImplementationsFQ.Count == 0)
				{
					throw new Exception($"No implementations of {nameof(IMetricsAnalyzer)} were found!");
				}
				// TODO: Improve selection by basing it on current project's language.
				analyzerTypeName = AnalyzerImplementationsFQ.First().Key;
			}
			else
			{
				analyzerTypeName = this.Configuration.UseMetricsAnalyzer;
			}

			this.SelectedAnalyzerImplementation = analyzerTypeName;

			logger.LogInformation(
				$"Using Metrics Analyzer implementation: {this.SelectedAnalyzerImplementation}");
		}

		public void Analyze()
		{
			if (StringExtensions.IsNullOrEmptyOrWhiteSpace(this.SelectedAnalyzerImplementation))
			{
				throw new Exception($"No Analyzer was selected. Call {nameof(SelectAnalyzerImplementation)} to select one.");
			}
			var analyzerTypeName = this.SelectedAnalyzerImplementation;

			var parallelOptions = new ParallelOptions();
			if (this.ExecutionPolicy == ExecutionPolicy.Linear)
			{
				parallelOptions.MaxDegreeOfParallelism = 1;
			}
			else
			{
				parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount / 2;
			}

			using (var repoBundleCollection = this.Repository.CreateBundleCollection(
				targetPath: Configuration.TempDirectory.FullName,
				numInstances: (UInt16)parallelOptions.MaxDegreeOfParallelism,
				deleteClonedReposAfterwards: this.DeleteClonedRepoAfterwards
			)) {
				Parallel.ForEach(this.CommitEntities, parallelOptions, commit => {
					using (var loanedRepo = repoBundleCollection.Loan())
					// using and dispose will return the item to the collection.
					{
						var copyRepo = loanedRepo.Item;

						try
						{
							logger.LogDebug($"Checking out repository at commit {commit.BaseObject.ShaShort()}");
							Commands.Checkout(copyRepo, commit.BaseObject);
						}
						catch (Exception ex)
						{
							logger.LogError($"Cannot checkout commit {commit.BaseObject.ShaShort()}: {ex.Message}", ex);

							this.Results.Add(new MetricsAnalysisResult
							{
								Commit = commit,
								Metrics = new List<MetricEntity>(),
								MetricTypes = new List<MetricTypeEntity>(),
								Repository = this.RepositoryEntity,
								CommitMetricsStatus = CommitMetricsStatus.CheckoutError
							});
							return;
						}

						var analyzer = CreateAnalyzer(
							this.Configuration, analyzerTypeName, analyzerTypeName.Contains("."));

						this.Results.Add(analyzer.Analyze(copyRepo, this.RepositoryEntity, commit));
					}
				});
			}
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
					.ToDictionary(kv => kv.Value.FullName, kv => kv.Value));
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
		public static IMetricsAnalyzer CreateAnalyzer(Configuration configuration, String typeName, Boolean nameIsFullyQualified = false)
		{
			var dict = nameIsFullyQualified ? AnalyzerImplementationsFQ : AnalyzerImplementations;

			if (!dict.ContainsKey(typeName))
			{
				throw new Exception($"There is no Analyzer with the name {typeName}.");
			}

			var type = dict[typeName];
			var analyzer = (IMetricsAnalyzer)Activator.CreateInstance(type);
			analyzer.Configuration = configuration.MetricsAnalyzers
				.Where(ma => ma.TypeName == typeName).First();
			return analyzer;
		}
		#endregion
	}
}
