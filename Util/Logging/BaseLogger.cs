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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Util.Logging
{
	/// <summary>
	/// Provides common logging functionality for derived loggers.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class BaseLogger<T> : ILogger<T>, IDisposable
	{
		/// <summary>
		/// A dictionary with all runtime-types. The logger can log the current type.
		/// </summary>
		protected static Lazy<IDictionary<Type, String>> lazyLoadedTypes = new Lazy<IDictionary<Type, String>>(() =>
		{
			var dict = new Dictionary<Type, String>();

			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(asmbly =>
			{
				try
				{
					return asmbly.GetTypes();
				}
				catch
				{
					return Enumerable.Empty<Type>();
				}
			}))
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

		/// <summary>
		/// A <see cref="Stack{T}"/> of <see cref="Scope{TState}"/> for each type.
		/// </summary>
		protected IDictionary<Type, Stack<IScope>> scopeStacks;

		/// <summary>
		/// Initialites the stack of scopes and must be called.
		/// </summary>
		public BaseLogger()
		{
			this.scopeStacks = new Dictionary<Type, Stack<IScope>>();
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

			private BaseLogger<T> baseLogger;

			public Scope(TState scopeValue, BaseLogger<T> bl)
			{
				this.ScopeValue = scopeValue;
				this.baseLogger = bl;
			}

			public void Dispose()
			{
				this.ScopeValue = default(TState);
				this.baseLogger.PopScope(this);
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
				return $"{ DateTime.Now.ToString("HH:mm:ss") } ";
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
				return this.scopeStacks.Count == 0 ? String.Empty :
					$"[{ String.Join(", ", this.scopeStacks.Reverse().Select(sc => sc.ToString())) }]";
			}
		}
		#endregion
		#endregion

		#region ILogger<T>
		private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		public virtual IDisposable BeginScope<TState>(TState state)
		{
			var type = typeof(T);
			this.semaphore.Wait();
			if (!this.scopeStacks.ContainsKey(type))
			{
				this.scopeStacks[type] = new Stack<IScope>();
			}

			var scope = new Scope<TState>(state, this);
			this.scopeStacks[type].Push(scope);
			this.semaphore.Release();
			return scope;
		}

		private void PopScope<TState>(Scope<TState> scope)
		{
			var type = typeof(T);
			this.semaphore.Wait();

			if (this.scopeStacks[type].Peek() != scope)
			{
				throw new InvalidOperationException("Invalid nesting of Scopes. The current Scope is not on top of the stack.");
			}

			this.scopeStacks[type].Pop();
			this.semaphore.Release();
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

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.semaphore.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}
}
