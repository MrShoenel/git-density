using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Data;

namespace GitDensity.Data
{
	/// <summary>
	/// A class that can handle enums as strings (not integers) and supports generic
	/// conversion between the actual type of the enumeration and its values represented
	/// as <see cref="string"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class StringEnumMapper<T> : IUserType
	{
		public Boolean IsMutable { get { return false; } }
		public Type ReturnedType { get { return typeof(T); } }
		public SqlType[] SqlTypes { get { return new[] { new SqlType(DbType.String) }; } }
		
		public object NullSafeGet(IDataReader rs, string[] names, object owner)
		{
			var tmp = NHibernateUtil.String.NullSafeGet(rs, names[0]).ToString();

			return Enum.Parse(typeof(T), tmp);
		}

		public void NullSafeSet(IDbCommand cmd, object value, int index)
		{
			if (value == null)
			{
				((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
			}
			else
			{
				((IDataParameter)cmd.Parameters[index]).Value = value.ToString();
			}
		}

		public object DeepCopy(object value)
		{
			return value;
		}

		public object Replace(object original, object target, object owner)
		{
			return original;
		}

		public object Assemble(object cached, object owner)
		{
			return cached;
		}

		public object Disassemble(object value)
		{
			return value;
		}

		public new bool Equals(object x, object y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			return x.Equals(y);
		}

		public int GetHashCode(object x)
		{
			return x == null ? typeof(T).GetHashCode() : x.GetHashCode();
		}
	}
}
