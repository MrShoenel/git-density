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

namespace Util
{
	/// <summary>
	/// An enumeration with supported ways to execute GitDensity and related programs.
	/// </summary>
	public enum ExecutionPolicy : Int32
	{
		/// <summary>
		/// Use as many resources as possible in parallel, to accomplish the task quicker.
		/// </summary>
		Parallel = 1,
		/// <summary>
		/// No parallelism, use a linear model to process in a series. This approach is
		/// more resource friendly, but slower.
		/// </summary>
		Linear = 2
	}

	/// <summary>
	/// An interface for types that support parallel- and lines execution.
	/// </summary>
	public interface ISupportsExecutionPolicy
	{
		/// <summary>
		/// A property to be set on supporting types.
		/// </summary>
		ExecutionPolicy ExecutionPolicy { get; set; }
	}
}
