/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
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
	public class MetricTypeEntity
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
			const double epsilonAccuracy = 1e-12;

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


				var mte = session.QueryOver<MetricTypeEntity>()
					.Where(x => x.MetricName.Equals(name, StringComparison.OrdinalIgnoreCase)
						&& x.IsScore == isScore
						&& x.IsRoot == isRoot
						&& x.IsPublic == isPublic
						&& (!forceLookupAccuary || Math.Abs(x.Accuracy - accuracy) < epsilonAccuracy))
					.SingleOrDefault();

				if (mte is MetricTypeEntity)
				{
					if (waitSuccess)
					{
						mutex.ReleaseMutex();
					}
					return mte;
				}

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

				if (waitSuccess)
				{
					mutex.ReleaseMutex();
				}

				return mte;
			}
		}
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
