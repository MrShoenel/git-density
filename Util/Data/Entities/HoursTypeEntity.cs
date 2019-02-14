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
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Threading;
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// Each <see cref="HoursEntity"/> must now point to one specific type
	/// of <see cref="HoursTypeEntity"/>. This entity represents a specific
	/// configuration, for how times were logged.
	/// </summary>
	public class HoursTypeEntity : IEquatable<HoursTypeEntity>
	{
		public virtual UInt32 ID { get; set; }

		public virtual UInt32 MaxCommitDiffMinutes { get; set; }

		public virtual UInt32 FirstCommitAddMinutes { get; set; }

		public virtual ISet<HoursEntity> Hours { get; set; } = new HashSet<HoursEntity>();

		private readonly Object padLock = new Object();

		#region Methods
		public virtual HoursTypeEntity AddHour(HoursEntity hour)
		{
			lock (this.padLock)
			{
				this.Hours.Add(hour);
				return this;
			}
		}

		public virtual HoursTypeEntity AddHours(IEnumerable<HoursEntity> hours)
		{
			foreach (var hour in hours)
			{
				this.AddHour(hour);
			}
			return this;
		}

		/// <summary>
		/// This is the only valid method to obtain an <see cref="HoursTypeEntity"/> with
		/// the specified parameters.
		/// </summary>
		/// <param name="maxCommitDiffInMinutes"></param>
		/// <param name="firstCommitAddMinutes"></param>
		/// <returns></returns>
		public static HoursTypeEntity ForSettings(UInt32 maxCommitDiffInMinutes, UInt32 firstCommitAddMinutes)
		{
			// Use a Mutex across all processes
			using (var mutex = new Mutex(false, $"mutex_{nameof(HoursTypeEntity)}"))
			using (var session = DataFactory.Instance.OpenSession())
			using (var trans = session.BeginTransaction(System.Data.IsolationLevel.Serializable))
			{
				Boolean waitSuccess = false;
				try
				{
					waitSuccess = mutex.WaitOne(TimeSpan.FromSeconds(5));
				}
				catch (AbandonedMutexException) { }

				var hte = session.QueryOver<HoursTypeEntity>()
					.Where(x => x.MaxCommitDiffMinutes == maxCommitDiffInMinutes
						&& x.FirstCommitAddMinutes == firstCommitAddMinutes)
					.SingleOrDefault();

				if (hte is HoursTypeEntity)
				{
					if (waitSuccess)
					{
						mutex.ReleaseMutex();
					}
					return hte;
				}

				hte = new HoursTypeEntity
				{
					MaxCommitDiffMinutes = maxCommitDiffInMinutes,
					FirstCommitAddMinutes = firstCommitAddMinutes
				};
				session.Save(hte);
				trans.Commit();

				if (waitSuccess)
				{
					mutex.ReleaseMutex();
				}

				return hte;
			}
		}

		#region equality
		public virtual bool Equals(HoursTypeEntity other)
		{
			return other is HoursTypeEntity && other.FirstCommitAddMinutes == this.FirstCommitAddMinutes && other.MaxCommitDiffMinutes == this.MaxCommitDiffMinutes;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as HoursTypeEntity);
		}

		public override int GetHashCode()
		{
			return 31 * this.FirstCommitAddMinutes.GetHashCode() ^ this.MaxCommitDiffMinutes.GetHashCode();
		}
		#endregion
		#endregion
	}

	public class HoursTypeEntityMap : ClassMap<HoursTypeEntity>
	{
		public HoursTypeEntityMap()
		{
			this.Table(nameof(HoursTypeEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.MaxCommitDiffMinutes).Not.Nullable().UniqueKey("UNQ_HOURS_TYPE");
			this.Map(x => x.FirstCommitAddMinutes).Not.Nullable().UniqueKey("UNQ_HOURS_TYPE");

			this.HasMany<HoursEntity>(x => x.Hours).Cascade.Lock();
		}
	}
}
