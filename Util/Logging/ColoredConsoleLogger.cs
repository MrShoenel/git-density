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
using Microsoft.Extensions.Logging;
using System;
using CC = Util.ColoredConsole;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Util.Logging
{
	public class ColoredConsoleLogger<T> : BaseLogger<T>
	{
		/// <summary>
		/// Logs the message if the <see cref="LogLevel"/> is enabled.
		/// Each loglevel has a different color associated. The event is logged, if its ID is not 0.
		/// The state-object is converted to string and logged, if not null. The same goes for the
		/// exception. If a formatter is provided and at least the state or the exception is non-null,
		/// it will be called and its output will be appended as well.
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <param name="logLevel"></param>
		/// <param name="eventId"></param>
		/// <param name="state">Usually the message as <see cref="String"/>.</param>
		/// <param name="exception"></param>
		/// <param name="formatter"></param>
		public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, String> formatter)
		{
			// cyan, green, white, magenta, red, yellow
			if (!this.IsEnabled(logLevel))
			{
				return; // We're not logging this
			}

			var timeString = this.LogCurrentTime ? this.TimeString : String.Empty;
			var typeString = this.LogCurrentType ? this.TypeString : String.Empty;
			var scopeString = this.LogCurrentScope ? this.ScopeString : String.Empty;

			var prefix = timeString == String.Empty && typeString == String.Empty && scopeString == String.Empty ?
				String.Empty : $"{timeString}{typeString}{scopeString}: ";
			var eventString = eventId.Id == 0 ? String.Empty : $"({ eventId.Id }, { eventId.Name }) ";
			// If state and exception are null, there is nothing to format.
			// Else, check if there is a formatter and use it. If there is
			// no formatter, call ToString() on the state and append the
			// exception's message, if there is an exception.
			var stateAndExString = state == null && exception == null ? String.Empty :
				(formatter is Func<TState, Exception, String> ? $"{ formatter(state, exception) }" :
				(state == null ? String.Empty : $"{ state.ToString() }, " +
				$"{ (exception == null ? String.Empty : exception.Message) }"));

			var wholeLogString = $"{prefix}{eventString}{stateAndExString}".Trim();

			using (var colorScope = CC.WithColorScope())
			{
				// Crit, Debug, Err, info, none, trace, warn
				switch (logLevel)
				{
					case LogLevel.Trace:
						CC.GrayLine(wholeLogString);
						break;
					case LogLevel.Debug:
						CC.GreenLine(wholeLogString);
						break;
					case LogLevel.Information:
						CC.WhiteLine(wholeLogString);
						break;
					case LogLevel.Warning:
						CC.YellowLine(wholeLogString);
						break;
					case LogLevel.Error:
						CC.RedLine(wholeLogString);
						break;
					case LogLevel.Critical:
						CC.MagentaLine(wholeLogString);
						break;
				}
			}
		}
	}
}
