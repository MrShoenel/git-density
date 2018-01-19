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
using LibGit2Sharp;
using System;
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// Holds the amount of hours worked within two consecutive <see cref="Commit"/>s
	/// of a developer and their total amount of hours worked since the initial commit.
	/// </summary>
	public class HoursEntity
	{
		public virtual UInt32 ID { get; set; }

		public virtual DeveloperEntity Developer { get; set; }

		/// <summary>
		/// Marks the beginning of the computed span (the older of the two consecutive
		/// commits of the current developer). This <see cref="CommitEntity"/> will be
		/// null for the analyzed span where <see cref="CommitUntil"/> points to the
		/// current developer's initial commit (because there possibly cannot another
		/// commit before).
		/// </summary>
		public virtual CommitEntity CommitSince { get; set; }

		/// <summary>
		/// Marks the end of the computed span (the younger of the two consecutive
		/// commits of the current developer).
		/// </summary>
		public virtual CommitEntity CommitUntil { get; set; }

		/// <summary>
		/// The amount of hours worked during the two consecutive <see cref="Commit"/>s.
		/// </summary>
		public virtual Double Hours { get; set; }

		/// <summary>
		/// The amount of hours worked since the developer's initial commit.
		/// </summary>
		public virtual Double HoursTotal { get; set; }

		/// <summary>
		/// Points to the initial <see cref="CommitEntity"/> for the current developer.
		/// That means, that for all <see cref="HoursEntity"/> for the current developer,
		/// this property points to the same <see cref="CommitEntity"/> (because it never
		/// changes).
		/// </summary>
		public virtual CommitEntity InitialCommit { get; set; }
	}

	public class HoursEntityMap : ClassMap<HoursEntity>
	{
		public HoursEntityMap()
		{
			this.Table(nameof(HoursEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.Hours).Not.Nullable();
			this.Map(x => x.HoursTotal).Not.Nullable();

			this.References<DeveloperEntity>(x => x.Developer).Not.Nullable();
			this.References<CommitEntity>(x => x.CommitSince).Nullable();
			this.References<CommitEntity>(x => x.CommitUntil).Not.Nullable();
			this.References<CommitEntity>(x => x.InitialCommit).Not.Nullable();
		}
	}
}
