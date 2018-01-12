/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitDensity.Util
{
	public abstract class BaseLogger<T> : ILogger<T>
	{
		protected static Lazy<IDictionary<Type, String>> lazyLoadedTypes = new Lazy<IDictionary<Type, String>>(() =>
		{
			var dict = new Dictionary<Type, String>();
			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(asmbly => asmbly.GetTypes()))
			{
				dict[type] = type.Name;
			}

			var set = new HashSet<String>();
			var typesToRename = new List<Type>();
			foreach (var kv in dict)
			{
				if (set.Contains(kv.Value))
				{
					// Then we need to expand the name of the type to avoid collisions
					typesToRename.Add(kv.Key);
				}
			}

			foreach (var type in typesToRename)
			{
				dict[type] = type.AssemblyQualifiedName;
			}

			return dict;
		});

		#region
		/// <summary>
		/// Set or get the current <see cref="LogLevel"/>. Note that a message will
		/// be logged if its level is the same or greater. I.e. if the level was set
		/// to <see cref="LogLevel.Information"/>, the levels Debug and Trace will
		/// not be logged, but Warning, Error and Critical will be logged.
		/// </summary>
		public virtual LogLevel LogLevel { get; set; } = LogLevel.Information;

		/// <summary>
		/// If enabled, logs the time in 24-hour format, e.g. 17:23:56.
		/// </summary>
		public virtual Boolean LogCurrentTime { get; set; } = true;

		/// <summary>
		/// If enabled, logs the current (fully qualified) type, where
		/// the message/entry originates from. Default = true.
		/// </summary>
		public virtual Boolean LogCurrentType { get; set; } = true;

		/// <summary>
		/// Logs the current Scope and its parent scopes, formatted as
		/// [Scope: top, nested, furter-nested, ..]. Default = false.
		/// </summary>
		public virtual Boolean LogCurrentScope { get; set; } = false;

		protected Stack<IScope> scopeStack;

		public BaseLogger()
		{
			this.scopeStack = new Stack<IScope>();
		}


		#region interfaces, classes, Scope
		protected interface IScope : IDisposable
		{
		}

		protected interface IScope<TState> : IScope
		{
			TState ScopeValue { get; }
		}

		protected class Scope<TState> : IScope<TState>
		{
			public TState ScopeValue { get; private set; }

			public Scope(TState scopeValue)
			{
				this.ScopeValue = scopeValue;
			}

			public void Dispose()
			{
				this.ScopeValue = default(TState);
			}

			public override string ToString()
			{
				return this.ScopeValue == null ? String.Empty : this.ScopeValue.ToString();
			}

			public static implicit operator String(Scope<TState> scope)
			{
				return scope.ToString();
			}
		}

		protected String TimeString
		{
			get
			{
				return $"{ DateTime.UtcNow.ToString("HH:mm:ss") } ";
			}
		}

		protected String TypeString
		{
			get
			{
				return $"[{ BaseLogger<T>.lazyLoadedTypes.Value[typeof(T)] }]";
			}
		}

		protected String ScopeString
		{
			get
			{
				return this.scopeStack.Count == 0 ? String.Empty :
					$"[{ String.Join(", ", this.scopeStack.Reverse().Select(sc => sc.ToString())) }]";
			}
		}
		#endregion
		#endregion

		#region ILogger<T>
		public virtual IDisposable BeginScope<TState>(TState state)
		{
			var scope = new Scope<TState>(state);
			this.scopeStack.Push(scope);
			return scope;
		}

		/// <summary>
		/// Returns true, if a <see cref="Microsoft.Extensions.Logging.LogLevel"/> is enabled
		/// either explicitly or covered by another, less restrictive LogLevel being in place.
		/// </summary>
		/// <param name="logLevel"></param>
		/// <returns></returns>
		public virtual bool IsEnabled(LogLevel logLevel)
		{
			return (int)logLevel >= (int)this.LogLevel;
		}

		public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);
		#endregion
	}
}
