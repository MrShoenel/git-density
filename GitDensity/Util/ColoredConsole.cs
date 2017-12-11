using System;
using System.IO;
using System.Threading;

namespace GitDensity.Util
{
	/// <summary>
	/// A class for writing colored messages to the Console conveniently. It is
	/// recommended to use this class with an alias, e.g. 'using CC = ColoredConsole;'.
	/// </summary>
	/// <remarks>@Copyright Sebastian Hönel [sebastian.honel@lnu.se]</remarks>
	public static class ColoredConsole
	{
		private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		public static string Title
		{
			get { return Console.Title; }
			set { Console.Title = value; }
		}

		public static ConsoleColor BackgroundColor
		{
			get { return Console.BackgroundColor; }
			set { Console.BackgroundColor = value; }
		}

		public static readonly ConsoleColor InitialBackgroundColor = Console.BackgroundColor;

		public static readonly ConsoleColor InitialForegroundColor = Console.ForegroundColor;

		public static void SetInitialColors()
		{
			semaphore.Wait();
			Console.BackgroundColor = InitialBackgroundColor;
			C(InitialForegroundColor);
			semaphore.Release();
		}

		static ColoredConsole()
		{
			Title = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
		}


		#region Console-control
		private static void C(ConsoleColor cc)
		{
			Console.ForegroundColor = cc;
		}
		#endregion

		#region reading
		public static ConsoleKey ReadKey()
		{
			return Console.ReadKey().Key;
		}

		public static string ReadLine()
		{
			return Console.ReadLine();
		}
		#endregion



		#region bare-writing
		public static void Line()
		{
			Console.WriteLine();
		}

		public static void Line<T>(T val) where T : struct
		{
			Console.WriteLine(val);
		}

		public static void Line(object val)
		{
			Console.WriteLine(val);
		}

		public static void Line(string format, params object[] vals)
		{
			var l = vals.Length;

			switch (l)
			{
				case 0:
					Console.WriteLine(format);
					return;
				case 1:
					Console.WriteLine(format, vals[0]);
					return;
				case 2:
					Console.WriteLine(format, vals[0], vals[1]);
					return;
				case 3:
					Console.WriteLine(format, vals[0], vals[1], vals[2]);
					return;
				case 4:
					Console.WriteLine(format, vals[0], vals[1], vals[2], vals[3]);
					return;
				default:
					Console.WriteLine(format, vals);
					return;
			}
		}
		#endregion

		#region white
		public static void WhiteLine()
		{
			semaphore.Wait();
			C(ConsoleColor.White);
			Console.WriteLine();
			semaphore.Release();
		}

		public static void WhiteLine<T>(T val) where T : struct
		{
			semaphore.Wait();
			C(ConsoleColor.White);
			Line(val);
			semaphore.Release();
		}

		public static void WhiteLine(object val)
		{
			semaphore.Wait();
			C(ConsoleColor.White);
			Line(val);
			semaphore.Release();
		}

		public static void WhiteLine(string format, params object[] vals)
		{
			semaphore.Wait();
			C(ConsoleColor.White);
			Line(format, vals);
			semaphore.Release();
		}
		#endregion

		#region transparent
		public static void TransparentLine()
		{
			semaphore.Wait();
			Console.BackgroundColor = InitialBackgroundColor;
			C(InitialForegroundColor);
			Console.WriteLine();
			semaphore.Release();
		}

		public static void TransparentLine<T>(T val) where T : struct
		{
			semaphore.Wait();
			Console.BackgroundColor = InitialBackgroundColor;
			C(InitialForegroundColor);
			Line(val);
			semaphore.Release();
		}

		public static void TransparentLine(object val)
		{
			semaphore.Wait();
			Console.BackgroundColor = InitialBackgroundColor;
			C(InitialForegroundColor);
			Line(val);
			semaphore.Release();
		}

		public static void TransparentLine(string format, params object[] vals)
		{
			semaphore.Wait();
			Console.BackgroundColor = InitialBackgroundColor;
			C(InitialForegroundColor);
			Line(format, vals);
			semaphore.Release();
		}
		#endregion

		#region yellow
		public static void YellowLine()
		{
			Console.WriteLine();
		}

		public static void YellowLine<T>(T val) where T : struct
		{
			semaphore.Wait();
			C(ConsoleColor.Yellow);
			Line(val);
			semaphore.Release();
		}

		public static void YellowLine(object val)
		{
			semaphore.Wait();
			C(ConsoleColor.Yellow);
			Line(val);
			semaphore.Release();
		}

		public static void YellowLine(string format, params object[] vals)
		{
			semaphore.Wait();
			C(ConsoleColor.Yellow);
			Line(format, vals);
			semaphore.Release();
		}
		#endregion

		#region red
		public static void RedLine()
		{
			Console.WriteLine();
		}

		public static void RedLine<T>(T val) where T : struct
		{
			semaphore.Wait();
			C(ConsoleColor.Red);
			Line(val);
			semaphore.Release();
		}

		public static void RedLine(object val)
		{
			semaphore.Wait();
			C(ConsoleColor.Red);
			Line(val);
			semaphore.Release();
		}

		public static void RedLine(string format, params object[] vals)
		{
			semaphore.Wait();
			C(ConsoleColor.Red);
			Line(format, vals);
			semaphore.Release();
		}
		#endregion

		#region green
		public static void GreenLine()
		{
			Console.WriteLine();
		}

		public static void GreenLine<T>(T val) where T : struct
		{
			semaphore.Wait();
			C(ConsoleColor.Green);
			Line(val);
			semaphore.Release();
		}

		public static void GreenLine(object val)
		{
			semaphore.Wait();
			C(ConsoleColor.Green);
			Line(val);
			semaphore.Release();
		}

		public static void GreenLine(string format, params object[] vals)
		{
			semaphore.Wait();
			C(ConsoleColor.Green);
			Line(format, vals);
			semaphore.Release();
		}
		#endregion

		#region cyan
		public static void CyanLine()
		{
			Console.WriteLine();
		}

		public static void CyanLine<T>(T val) where T : struct
		{
			semaphore.Wait();
			C(ConsoleColor.Cyan);
			Line(val);
			semaphore.Release();
		}

		public static void CyanLine(object val)
		{
			semaphore.Wait();
			C(ConsoleColor.Cyan);
			Line(val);
			semaphore.Release();
		}

		public static void CyanLine(string format, params object[] vals)
		{
			semaphore.Wait();
			C(ConsoleColor.Cyan);
			Line(format, vals);
			semaphore.Release();
		}
		#endregion

		#region magenta
		public static void MagentaLine()
		{
			Console.WriteLine();
		}

		public static void MagentaLine<T>(T val) where T : struct
		{
			semaphore.Wait();
			C(ConsoleColor.Magenta);
			Line(val);
			semaphore.Release();
		}

		public static void MagentaLine(object val)
		{
			semaphore.Wait();
			C(ConsoleColor.Magenta);
			Line(val);
			semaphore.Release();
		}

		public static void MagentaLine(string format, params object[] vals)
		{
			semaphore.Wait();
			C(ConsoleColor.Magenta);
			Line(format, vals);
			semaphore.Release();
		}
		#endregion
	}
}