/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
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
using FluentNHibernate.Conventions.Instances;
using System;
using System.Linq;
using Util.Extensions;

namespace Util.Data
{
	/// <summary>
	/// Convention that ascertains that foreign keys always have a deterministic name.
	/// </summary>
	public class ForeignKeyConvention : FluentNHibernate.Conventions.IReferenceConvention
	{
		public void Apply(IManyToOneInstance instance)
		{
			var referencedColumns = String.Join("_", instance.Columns.Select(col => col.Name.ToSimpleUnderscoreCase().ToUpper()));
			instance.ForeignKey($"FK_{instance.EntityType.Name.ToSimpleUnderscoreCase().ToUpper()}_{referencedColumns}");
		}
	}
}
