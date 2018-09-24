using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
	/// <summary>
	/// This enum's values are used by the command-line applications
	/// in this solution.
	/// </summary>
	public enum ExitCodes : Int32
	{
		OK = 0,
		ConfigError = -1,
		RepoInvalid = -2,
		UsageInvalid = -3,
		CmdError = -4,
		OtherError = Int32.MinValue + 1
	}
}
