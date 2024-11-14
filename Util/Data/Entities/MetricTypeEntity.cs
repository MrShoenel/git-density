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
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// The <see cref="MetricTypeEntity"/> reflects a specific metric (e.g. LOC) and
	/// its properties, such as its precision/accuracy, name or whether it is a score.
	/// </summary>
	public class MetricTypeEntity : IEquatable<MetricTypeEntity>
	{
		public virtual UInt32 ID { get; set; }

		public virtual String MetricName { get; set; }

		public virtual Boolean IsScore { get; set; }

		public virtual Boolean IsRoot { get; set; }

		public virtual Boolean IsPublic { get; set; }

		public virtual Double Accuracy { get; set; }

		public virtual ISet<MetricEntity> Metrics { get; set; }
			= new HashSet<MetricEntity>();

		private readonly Object padLock = new Object();

		#region Methods
		public virtual MetricTypeEntity AddMetric(MetricEntity metric)
		{
			lock (this.padLock)
			{
				this.Metrics.Add(metric);
				return this;
			}
		}

		public virtual MetricTypeEntity AddMetrics(IEnumerable<MetricEntity> metrics)
		{
			foreach (var metric in metrics)
			{
				this.AddMetric(metric);
			}
			return this;
		}

		private const double lookupEpsilonAccuracy = 1e-12;

		private static readonly Object padlock = new Object();

		private static ISet<MetricTypeEntity> cache = new HashSet<MetricTypeEntity>();

		private static Boolean TryCache(out MetricTypeEntity mte, String name, Boolean isScore, Boolean isRoot, Boolean isPublic, Double accuracy, Boolean forceLookupAccuary = false)
		{
			lock (padlock)
			{
				mte = default(MetricTypeEntity);
				if (cache.Count == 0)
				{
					return false;
				}

				mte = cache.Where(x => x.MetricName.Equals(name, StringComparison.InvariantCultureIgnoreCase) && x.IsScore == isScore && x.IsRoot == isRoot && x.IsPublic == isPublic && (!forceLookupAccuary || Math.Abs(x.Accuracy - accuracy) < lookupEpsilonAccuracy)).SingleOrDefault();

				return mte is MetricTypeEntity;
			}
		}

		/// <summary>
		/// Exclusively obtains an <see cref="MetricTypeEntity"/> for the given settings.
		/// If the entity does not exist yet, it will be created and saved before it is
		/// returned. Note that all parameters except for <see cref="Accuracy"/> are taken
		/// into account for the lookup (i.e. there is a unique-constraint on the name and
		/// all boolean properties).
		/// </summary>
		/// <param name="name">The name of the metric. It is important that it does not
		/// contain any hints as to whether the metric is supposed to be a score. This is
		/// solely specified by <see cref="IsScore"/>.</param>
		/// <param name="isScore">This should be set to true, if this metric is a score.
		/// The metric's name must not contain hints as to whether it is a score.</param>
		/// <param name="isRoot">This should be set to true if this metric is not a child
		/// to another metric (i.e. a top-level or root metric).</param>
		/// <param name="isPublic">This should be set to true if this metric is a public
		/// (exposed) metric.</param>
		/// <param name="accuracy">The value of accuracy is only used for when a metric
		/// is created, not for looking it up.</param>
		/// <param name="forceLookupAccuary">Should be set to true, if accuracy needs to
		/// be explicitly used when looking up an existing metric. An epsilon of 1e-12 is
		/// used then.</param>
		/// <returns>The <see cref="MetricTypeEntity"/> for the settings.</returns>
		public static MetricTypeEntity ForSettings(String name, Boolean isScore, Boolean isRoot, Boolean isPublic, Double accuracy, Boolean forceLookupAccuary = false)
		{
			MetricTypeEntity mte;
			if (TryCache(out mte, name, isScore, isRoot, isPublic, accuracy, forceLookupAccuary))
			{
				return mte;
			}

			// Use a Mutex across all processes
			using (var mutex = new Mutex(false, $"mutex_{nameof(MetricTypeEntity)}"))
			using (var session = DataFactory.Instance.OpenSession())
			using (var trans = session.BeginTransaction(System.Data.IsolationLevel.Serializable))
			{
				Boolean waitSuccess = false;
				try
				{
					waitSuccess = mutex.WaitOne(TimeSpan.FromSeconds(5));
				}
				catch (AbandonedMutexException) { }


				var mtes = session.QueryOver<MetricTypeEntity>()
					.WhereRestrictionOn(x => x.MetricName).IsInsensitiveLike(name)
					.Where(x => x.IsScore == isScore && x.IsRoot == isRoot && x.IsPublic == isPublic).List();

			CheckCount:
				switch (mtes.Count)
				{
					case 0:
						goto CreateNew;
					case 1:
						mte = mtes[0];
						goto ReleaseReturnCache;
					default:
						// More than 1, let's check if we shall filter by accuracy:
						if (forceLookupAccuary)
						{
							mtes = mtes.Where(x => Math.Abs(x.Accuracy - accuracy) < lookupEpsilonAccuracy).ToList();
							goto CheckCount;
						}
						else
						{
							throw new Exception($"Cannot identify the requested {nameof(MetricTypeEntity)}, got {mtes.Count} results.");
						}
				}

			CreateNew:
				mte = new MetricTypeEntity
				{
					MetricName = name,
					IsScore = isScore,
					IsRoot = isRoot,
					IsPublic = isPublic,
					Accuracy = accuracy
				};
				session.Save(mte);
				trans.Commit();

			ReleaseReturnCache:

				if (waitSuccess)
				{
					mutex.ReleaseMutex();
				}

				lock (padlock)
				{
					cache.Add(mte);
				}
				return mte;
			}
		}

		#region equality
		public override int GetHashCode()
		{
			return 31 * (this.MetricName ?? String.Empty).GetHashCode() ^ this.IsPublic.GetHashCode() ^ this.IsRoot.GetHashCode() ^ this.IsScore.GetHashCode();
		}

		public virtual bool Equals(MetricTypeEntity other)
		{
			return other is MetricTypeEntity && other.IsPublic == this.IsPublic && other.IsRoot == this.IsRoot && other.IsScore == this.IsScore && other.MetricName.Equals(this.MetricName, StringComparison.InvariantCultureIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as MetricTypeEntity);
		}
		#endregion
		#endregion
	}

	public class MetricTypeEntityMap : ClassMap<MetricTypeEntity>
	{
		public MetricTypeEntityMap()
		{
			this.Table(nameof(MetricTypeEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			const string unqKeyName = "UNQ_METRIC_TYPE";

			this.Map(x => x.MetricName).Not.Nullable().UniqueKey(unqKeyName);
			this.Map(x => x.IsScore).Not.Nullable().UniqueKey(unqKeyName);
			this.Map(x => x.IsRoot).Not.Nullable().UniqueKey(unqKeyName);
			this.Map(x => x.IsPublic).Not.Nullable().UniqueKey(unqKeyName);

			this.Map(x => x.Accuracy).Not.Nullable();

			this.HasMany<MetricEntity>(x => x.Metrics).Cascade.Lock();
		}
	}
}
