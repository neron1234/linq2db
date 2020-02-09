using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Collections.Concurrent;
	using Common;
	using Configuration;
	using Data;

	public static class SqlServerTools
	{
		#region Init

		private static readonly ConcurrentBag<SqlServerDataProvider> _providers = new ConcurrentBag<SqlServerDataProvider>();
		public static SqlServerProvider Provider = SqlServerProvider.SystemDataSqlClient;

		// System.Data
		// and/or
		// System.Data.SqlClient
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2000sdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2000, SqlServerVersion.v2000, SqlServerProvider.SystemDataSqlClient);

			if (Provider == SqlServerProvider.SystemDataSqlClient)
				DataConnection.AddDataProvider(provider);

			_providers.Add(provider);
			return provider;
		}, true);
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2005sdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2005, SqlServerVersion.v2005, SqlServerProvider.SystemDataSqlClient);

			if (Provider == SqlServerProvider.SystemDataSqlClient)
				DataConnection.AddDataProvider(provider);

			_providers.Add(provider);
			return provider;
		}, true);
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2008sdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.SystemDataSqlClient);

			if (Provider == SqlServerProvider.SystemDataSqlClient)
			{
				DataConnection.AddDataProvider(provider);
			}

			_providers.Add(provider);
			return provider;
		}, true);
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2012sdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient);

			if (Provider == SqlServerProvider.SystemDataSqlClient)
			{
				DataConnection.AddDataProvider(ProviderName.SqlServer2014, provider);
				DataConnection.AddDataProvider(provider);
			}

			_providers.Add(provider);
			return provider;
		}, true);
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2017sdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2017, SqlServerVersion.v2017, SqlServerProvider.SystemDataSqlClient);

			if (Provider == SqlServerProvider.SystemDataSqlClient)
				DataConnection.AddDataProvider(provider);

			_providers.Add(provider);
			return provider;
		}, true);

		// Microsoft.Data.SqlClient
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2000mdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2000, SqlServerVersion.v2000, SqlServerProvider.MicrosoftDataSqlClient);

			if (Provider == SqlServerProvider.MicrosoftDataSqlClient)
				DataConnection.AddDataProvider(provider);

			_providers.Add(provider);
			return provider;
		}, true);
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2005mdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2005, SqlServerVersion.v2005, SqlServerProvider.MicrosoftDataSqlClient);

			if (Provider == SqlServerProvider.MicrosoftDataSqlClient)
				DataConnection.AddDataProvider(provider);

			_providers.Add(provider);
			return provider;
		}, true);
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2008mdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient);

			if (Provider == SqlServerProvider.MicrosoftDataSqlClient)
			{
				DataConnection.AddDataProvider(provider);
			}

			_providers.Add(provider);
			return provider;
		}, true);
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2012mdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient);

			if (Provider == SqlServerProvider.MicrosoftDataSqlClient)
			{
				DataConnection.AddDataProvider(ProviderName.SqlServer2014, provider);
				DataConnection.AddDataProvider(provider);
			}

			_providers.Add(provider);
			return provider;
		}, true);
		private static readonly Lazy<IDataProvider> _sqlServerDataProvider2017mdc = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlServerDataProvider(ProviderName.SqlServer2017, SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient);

			if (Provider == SqlServerProvider.MicrosoftDataSqlClient)
				DataConnection.AddDataProvider(provider);

			_providers.Add(provider);
			return provider;
		}, true);

		public static bool AutoDetectProvider { get; set; } = true;

		internal static string BasicQuoteIdentifier(string identifier)
		{
			return '[' + identifier.Replace("]", "]]") + ']';
		}

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			var provider = Provider;

			if (css.ProviderName      == SqlServerProviderAdapter.MicrosoftClientNamespace)
				provider = SqlServerProvider.MicrosoftDataSqlClient;
			else if (css.ProviderName == SqlServerProviderAdapter.SystemClientNamespace)
				provider = SqlServerProvider.SystemDataSqlClient;

			switch (css.ProviderName)
			{
				case ""                      :
				case null                    :
					if (css.Name == "SqlServer")
						goto case ProviderName.SqlServer;
					break;
					// SqlClient use dot prefix, as SqlClient itself used by some other providers
				case var providerName when providerName.Contains("SqlServer") || providerName.Contains(".SqlClient"):
				case ProviderName.SqlServer:
					if (css.Name.Contains("2000") || css.ProviderName?.Contains("2000") == true) return GetDataProvider(SqlServerVersion.v2000, provider);
					if (css.Name.Contains("2005") || css.ProviderName?.Contains("2005") == true) return GetDataProvider(SqlServerVersion.v2005, provider);
					if (css.Name.Contains("2008") || css.ProviderName?.Contains("2008") == true) return GetDataProvider(SqlServerVersion.v2008, provider);
					if (css.Name.Contains("2012") || css.ProviderName?.Contains("2012") == true) return GetDataProvider(SqlServerVersion.v2012, provider);
					if (css.Name.Contains("2014") || css.ProviderName?.Contains("2014") == true) return GetDataProvider(SqlServerVersion.v2012, provider);
					if (css.Name.Contains("2016") || css.ProviderName?.Contains("2016") == true) return GetDataProvider(SqlServerVersion.v2012, provider);
					if (css.Name.Contains("2017") || css.ProviderName?.Contains("2017") == true) return GetDataProvider(SqlServerVersion.v2017, provider);
					if (css.Name.Contains("2019") || css.ProviderName?.Contains("2019") == true) return GetDataProvider(SqlServerVersion.v2017, provider);

					if (AutoDetectProvider)
					{
						try
						{
							var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

							using (var conn = SqlServerProviderAdapter.GetInstance(provider).CreateConnection(cs))
							{
								conn.Open();

								if (int.TryParse(conn.ServerVersion.Split('.')[0], out var version))
								{
									if (version <= 8)
										return GetDataProvider(SqlServerVersion.v2000, provider);

									using (var cmd = conn.CreateCommand())
									{
										cmd.CommandText = "SELECT compatibility_level FROM sys.databases WHERE name = db_name()";
										var level = Converter.ChangeTypeTo<int>(cmd.ExecuteScalar());

										if (level >= 140)
											return GetDataProvider(SqlServerVersion.v2017, provider);
										if (level >= 110)
											return GetDataProvider(SqlServerVersion.v2012, provider);
										if (level >= 100)
											return GetDataProvider(SqlServerVersion.v2008, provider);
										if (level >= 90)
											return GetDataProvider(SqlServerVersion.v2005, provider);
										if (level >= 80)
											return GetDataProvider(SqlServerVersion.v2000, provider);

										switch (version)
										{
											case  8 : return GetDataProvider(SqlServerVersion.v2000, provider);
											case  9 : return GetDataProvider(SqlServerVersion.v2005, provider);
											case 10 : return GetDataProvider(SqlServerVersion.v2008, provider);
											case 11 :
											case 12 :
											case 13 : return GetDataProvider(SqlServerVersion.v2012, provider);
											case 14 :
											case 15 : return GetDataProvider(SqlServerVersion.v2017, provider);
											default :
												if (version > 15)
													return GetDataProvider(SqlServerVersion.v2017, provider);
												return GetDataProvider(SqlServerVersion.v2008, provider);
										}
									}
								}
							}
						}
						catch
						{
						}
					}

					return GetDataProvider(provider: provider);
			}

			return null;
		}

#endregion

#region Public Members

		public static IDataProvider GetDataProvider(
			SqlServerVersion version   = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			switch (provider)
			{
				case SqlServerProvider.SystemDataSqlClient:
					switch (version)
					{
						case SqlServerVersion.v2000: return _sqlServerDataProvider2000sdc.Value;
						case SqlServerVersion.v2005: return _sqlServerDataProvider2005sdc.Value;
						case SqlServerVersion.v2012: return _sqlServerDataProvider2012sdc.Value;
						case SqlServerVersion.v2017: return _sqlServerDataProvider2017sdc.Value;
						default: return _sqlServerDataProvider2008sdc.Value;
					}
				case SqlServerProvider.MicrosoftDataSqlClient:
					switch (version)
					{
						case SqlServerVersion.v2000: return _sqlServerDataProvider2000mdc.Value;
						case SqlServerVersion.v2005: return _sqlServerDataProvider2005mdc.Value;
						case SqlServerVersion.v2012: return _sqlServerDataProvider2012mdc.Value;
						case SqlServerVersion.v2017: return _sqlServerDataProvider2017mdc.Value;
						default: return _sqlServerDataProvider2008mdc.Value;
					}
				default: return _sqlServerDataProvider2008sdc.Value;
			}
		}

		public static void AddUdtType(Type type, string udtName)
		{
			foreach (var provider in _providers)
				provider.AddUdtType(type, udtName);
		}

		public static void AddUdtType<T>(string udtName, T nullValue, DataType dataType = DataType.Undefined)
		{
			foreach (var provider in _providers)
				provider.AddUdtType(udtName, nullValue, dataType);
		}

		/// <summary>
		/// Loads and registers spatial types assembly (Microsoft.SqlServer.Types) using provided path.
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(path, SqlServerTypes.AssemblyName);
		}

		/// <summary>
		/// Registers spatial types assembly (Microsoft.SqlServer.Types).
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes(Assembly assembly)
		{
			var types = assembly.GetTypes();

			SqlHierarchyIdType = types.First(t => t.Name == SqlServerTypes.SqlHierarchyIdType);
			SqlGeographyType   = types.First(t => t.Name == SqlServerTypes.SqlGeographyType);
			SqlGeometryType    = types.First(t => t.Name == SqlServerTypes.SqlGeometryType);
		}

		internal static Type? SqlHierarchyIdType;
		internal static Type? SqlGeographyType;
		internal static Type? SqlGeometryType;

		public static void SetSqlTypes(Type sqlHierarchyIdType, Type sqlGeographyType, Type sqlGeometryType)
		{
			SqlHierarchyIdType = sqlHierarchyIdType;
			SqlGeographyType   = sqlGeographyType;
			SqlGeometryType    = sqlGeometryType;
		}

#endregion

#region CreateDataConnection

		public static DataConnection CreateDataConnection(
			string            connectionString,
			SqlServerVersion  version  = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider), connectionString);
		}

		public static DataConnection CreateDataConnection(
			IDbConnection     connection,
			SqlServerVersion  version  = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider), connection);
		}

		public static DataConnection CreateDataConnection(
			IDbTransaction    transaction,
			SqlServerVersion  version  = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider), transaction);
		}

#endregion

#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;

		public static BulkCopyRowsCopied ProviderSpecificBulkCopy<T>(
			DataConnection              dataConnection,
			IEnumerable<T>              source,
			int?                        maxBatchSize       = null,
			int?                        bulkCopyTimeout    = null,
			bool                        keepIdentity       = false,
			bool                        checkConstraints   = false,
			int                         notifyAfter        = 0,
			Action<BulkCopyRowsCopied>? rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.ProviderSpecific,
					MaxBatchSize       = maxBatchSize,
					BulkCopyTimeout    = bulkCopyTimeout,
					KeepIdentity       = keepIdentity,
					CheckConstraints   = checkConstraints,
					NotifyAfter        = notifyAfter,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

#endregion

		public static class Sql
		{
			public const string OptionRecompile = "OPTION(RECOMPILE)";
		}

		public static Func<IDataReader,int,decimal> DataReaderGetMoney   = (dr, i) => dr.GetDecimal(i);
		public static Func<IDataReader,int,decimal> DataReaderGetDecimal = (dr, i) => dr.GetDecimal(i);
	}
}
