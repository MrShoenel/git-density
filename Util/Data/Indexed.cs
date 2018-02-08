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
