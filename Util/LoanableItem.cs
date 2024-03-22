using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Util
{
	/// <summary>
	/// Describes a collection items can be loaned from and
	/// returned to. A loaned item is given out as <see cref="ILoanableItem{T}"/>,
	/// so that an implementing class can keep track of them.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ILoanCollection<T> : IEnumerable<ILoanableItem<T>>, IDisposable
	{
		/// <summary>
		/// Return (or add a new) item to the collection, making it available
		/// for future loans.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		ILoanCollection<T> Return(ILoanableItem<T> item);

		/// <summary>
		/// Loan an item. This returns an <see cref="ILoanableItem{T}"/> that
		/// this collection can use to track loans
		/// </summary>
		/// <returns></returns>
		ILoanableItem<T> Loan();
	}

	/// <summary>
	/// Desc
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ILoanableItem<out T> : IDisposable
	{
		/// <summary>
		/// The item that was loaned.
		/// </summary>
		T Item { get; }
	}

	/// <summary>
	/// A simple implementation of <see cref="ILoanableItem{T}"/> that implements
	/// a dispose-pattern so that it returns itself after using it.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class LoanableItem<T> : ILoanableItem<T>, IEquatable<LoanableItem<T>>
	{
		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public T Item { get; protected internal set; }

		private readonly ILoanCollection<T> owningCollection;

		/// <summary>
		/// Create a new loaned item and keep track of the collection it
		/// belongs to.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="owningCollection"></param>
		public LoanableItem(T item, ILoanCollection<T> owningCollection)
		{
			this.Item = item;
			this.owningCollection = owningCollection;
		}

		#region Dispose
		private bool wasDisposed = false;
		/// <summary>
		/// Returns this to the owning collection.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (!wasDisposed)
			{
				if (disposing)
				{
					this.owningCollection.Return(this);
				}

				wasDisposed = true;
			}
		}

		/// <summary>
		/// Does not dispose this <see cref="Item"/>, but rather returns
		/// this instance to the owning collection.
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region Equals
		/// <summary>
		/// Calls <see cref="Equals(LoanableItem{T})"/>.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as LoanableItem<T>);
		}

		/// <summary>
		/// Uses the hash codes of the wrapped item and the owning collection
		/// to compute a hash code.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			if (EqualityComparer<T>.Default.Equals(this.Item, default))
			{
				return 0 ^ this.owningCollection.GetHashCode();
			}

			return this.Item.GetHashCode() ^ this.owningCollection.GetHashCode();
		}

		/// <summary>
		/// Returns true if the wrapped items and owning collection are equal
		/// to those of the other object.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(LoanableItem<T> other)
		{
			return other is LoanableItem<T>
				&& Object.Equals(this.Item, other.Item)
				&& Object.Equals(this.owningCollection, other.owningCollection);
		}
		#endregion
	}


	/// <summary>
	/// Implementation of an <see cref="ILoanCollection{T}"/> that uses a
	/// queue for its items internally, as well as a semaphore (and thus
	/// blocking behavior) for giving out items.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class LoanCollection<T> : ILoanCollection<T>
	{
		/// <summary>
		/// Returns the amount of items currently available for loan.
		/// </summary>
		public int Count => this.items.Count;

		private readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Queue used for loaning items.
		/// </summary>
		protected readonly ConcurrentQueue<T> items;

		/// <summary>
		/// Creates a new <see cref="LoanCollection{T}"/>.
		/// </summary>
		public LoanCollection()
		{
			this.semaphore = new SemaphoreSlim(initialCount: 0);
			this.items = new ConcurrentQueue<T>();
		}

		/// <summary>
		/// Add a new item to this collection that can be loaned.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public virtual ILoanCollection<T> Add(T item)
		{
			this.items.Enqueue(item);
			this.semaphore.Release();
			return this;
		}

		/// <summary>
		/// Return a previously loaned item. This method will not check
		/// whether the item was previously loaned from this collection,
		/// but rather unwrap it and call <see cref="Add(T)"/>.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public virtual ILoanCollection<T> Return(ILoanableItem<T> item)
		{
			return this.Add(item.Item);
		}

		/// <summary>
		/// Loan an item. This method is subject to availability of items
		/// and will block until an item is available. If you want to avoid
		/// blocking, you can check the availability with <see cref="Count"/>.
		/// Loaning and returning items is subject to a FIFO queue.
		/// </summary>
		/// <returns></returns>
		public virtual ILoanableItem<T> Loan()
		{
			this.semaphore.Wait();
			this.items.TryDequeue(out T item);
			return new LoanableItem<T>(item, this);
		}

		private readonly Object enumerableLock = new Object();

		/// <summary>
		/// Can be used to enumerate this collection. However, enumerating
		/// results in loaning each item, so that after full enumeration, this
		/// collection is empty. Furthermore, this is a synchronized method,
		/// so that no two enumerators can obtain loans simultaneously, i.e.,
		/// after full enumeration of this collection, another enumerator can
		/// enter.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerator<ILoanableItem<T>> GetEnumerator()
		{
			lock (this.enumerableLock)
			{
				while (this.semaphore.CurrentCount > 0)
				{
					yield return this.Loan();
				}
			}
		}

		/// <summary>
		/// Returns <see cref="GetEnumerator"/>.
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}


		#region IDispose
		protected bool wasDisposed = false;

		/// <summary>
		/// Disposes the underlying <see cref="SemaphoreSlim"/>, but does not
		/// dispose the items in the collection.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.wasDisposed)
			{
				if (disposing)
				{
					while (this.items.TryDequeue(out T _));
					this.semaphore.Dispose();
				}

				this.wasDisposed = true;
			}
		}

		public virtual void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
