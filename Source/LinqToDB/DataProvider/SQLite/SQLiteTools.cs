﻿#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.SQLite
{
	using System.Linq.Expressions;
	using Common;
	using Configuration;
	using Data;
	using Extensions;

	public static class SQLiteTools
	{
		public static string AssemblyName;

		static readonly SQLiteDataProvider _SQLiteClassicDataProvider  = new SQLiteDataProvider(ProviderName.SQLiteClassic);
		static readonly SQLiteDataProvider _SQLiteMSDataProvider       = new SQLiteDataProvider(ProviderName.SQLiteMS);

		public static bool AlwaysCheckDbNull = true;

		static SQLiteTools()
		{
			AssemblyName = DetectedProviderName == ProviderName.SQLiteClassic ? "System.Data.SQLite" : "Microsoft.Data.Sqlite";

			DataConnection.AddDataProvider(ProviderName.SQLite, DetectedProvider);
			DataConnection.AddDataProvider(_SQLiteClassicDataProvider);
			DataConnection.AddDataProvider(_SQLiteMSDataProvider);

			DataConnection.AddProviderDetector(ProviderDetector);
		}

		static IDataProvider ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.IsGlobal)
				return null;

			switch (css.ProviderName)
			{
				case ""                                :
				case null                              :

					if (css.Name.Contains("SQLite"))
						goto case "SQLite";
					break;

				case "SQLite.MS"             :
				case "SQLite.Microsoft"      :
				case "Microsoft.Data.Sqlite" :
				case "Microsoft.Data.SQLite" : return _SQLiteMSDataProvider;
				case "SQLite.Classic"        :
				case "System.Data.SQLite"    : return _SQLiteClassicDataProvider;
				case "SQLite"                :

					if (css.Name.Contains("MS") || css.Name.Contains("Microsoft"))
						return _SQLiteMSDataProvider;

					if (css.Name.Contains("Classic"))
						return _SQLiteClassicDataProvider;

					return DetectedProvider;
			}

			return null;
		}

		static string _detectedProviderName;

		public static string  DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		static SQLiteDataProvider DetectedProvider =>
			DetectedProviderName == ProviderName.SQLiteClassic ? _SQLiteClassicDataProvider : _SQLiteMSDataProvider;

		static string DetectProviderName()
		{
			try
			{
				var path = typeof(SQLiteTools).Assembly.GetPath();

				if (!File.Exists(Path.Combine(path, "System.Data.SQLite.dll")))
					if (File.Exists(Path.Combine(path, "Microsoft.Data.Sqlite.dll")))
						return ProviderName.SQLiteMS;
			}
			catch (Exception)
			{
			}

			return ProviderName.SQLiteClassic;
		}


		public static IDataProvider GetDataProvider()
		{
			return DetectedProvider;
		}

		public static void ResolveSQLite(string path)
		{
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveSQLite(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(DetectedProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(
				connection.GetType().Namespace.Contains("Microsoft") ? _SQLiteMSDataProvider : _SQLiteClassicDataProvider,
				connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(
				transaction.GetType().Namespace.Contains("Microsoft") ? _SQLiteMSDataProvider : _SQLiteClassicDataProvider,
				transaction);
		}

		#endregion

		static Action<string> _createDatabase;

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DataTools.CreateFileDatabase(
				databaseName, deleteIfExists, ".sqlite",
				dbName =>
				{
					if (_createDatabase == null)
					{
						var connectionType = DetectedProvider.GetConnectionType();
						var method = connectionType.GetMethodEx("CreateFile");
						if (method != null)
						{
							var p = Expression.Parameter(typeof(string));
							var l = Expression.Lambda<Action<string>>(
									Expression.Call(method, p),
								p);
							_createDatabase = l.Compile();
						}
						else
						{
							// emulate for Microsoft.Data.SQLite
							// that's actually what System.Data.SQLite does
							_createDatabase = name =>
							{
								using (File.Create(name)) { };
							};
						}
					}

					_createDatabase(dbName);
				});
		}

		public static void DropDatabase(string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DataTools.DropFileDatabase(databaseName, ".sqlite");
		}

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int                        maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
