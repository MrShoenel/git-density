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
	public class HoursTypeEntity
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
			using (var mutex = new Mutex(false, $"mutex_{nameof(HoursEntity)}"))
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
