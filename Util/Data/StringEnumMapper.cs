/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Data;
using System.Data.Common;
using NHibernate.Engine;

namespace Util.Data
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



		public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
		{
			var tmp = NHibernateUtil.String.NullSafeGet(rs, names[0], session, owner).ToString();
			return Enum.Parse(typeof(T), tmp);
		}

		public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
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
