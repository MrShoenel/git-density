using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace GitDensity.Density
{
	/// <summary>
	/// This interface is to be implemented by all strategies that can
	/// determine the density of code commited to a <see cref="Repository"/>
	/// over time.
	/// </summary>
	public interface IDensityMeasure
	{
	}

	public class GitDiffDensityMeasure
	{

	}
}
