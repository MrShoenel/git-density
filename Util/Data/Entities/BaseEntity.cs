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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util.Data.Entities
{
	/// <summary>
	/// Useful for some entities to keep track of the object they were created
	/// from. Defaults to default(T).
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class BaseEntity<T>
	{
		/// <summary>
		/// The object or struct this entity represents.
		/// </summary>
		public virtual T BaseObject { get; set; } = default(T);
	}
}
