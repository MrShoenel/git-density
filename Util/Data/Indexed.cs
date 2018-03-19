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
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;
using System;

namespace Util.Data
{
	/// <summary>
	/// Used to annotate an entity's property so that it becomes indexed.
	/// This attribute can also be used for properties that shall be unique.
	/// </summary>
	public class IndexedAttribute : Attribute
	{
		public Boolean Unique { get; set; }

		public String Name { get; set; }

		public IndexedAttribute()
		{
		}
	}

	public class IndexedConvention : AttributePropertyConvention<IndexedAttribute>
	{
		protected override void Apply(IndexedAttribute attribute, IPropertyInstance instance)
		{
			var name = (attribute.Name ?? instance.Name).ToUpperInvariant();

			if (attribute.Unique)
			{
				instance.UniqueKey($"UNQ_{name}");
			}
			else
			{
				instance.Index($"IDX_{name}");
			}
		}
	}
}
