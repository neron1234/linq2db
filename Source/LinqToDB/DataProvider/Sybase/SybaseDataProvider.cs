﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;
	using Mapping;
	using Common;
	using SchemaProvider;
	using SqlProvider;
	using LinqToDB.Extensions;

	public class SybaseDataProvider : DynamicDataProviderBase
	{
		#region Init

		public SybaseDataProvider()
			: this(SybaseTools.DetectedProviderName)
		{
		}

		public SybaseDataProvider(string name)
			: base(name, null!)
		{
			SqlProviderFlags.AcceptsTakeAsParameter           = false;
			SqlProviderFlags.IsSkipSupported                  = false;
			SqlProviderFlags.IsSubQueryTakeSupported          = false;
			//SqlProviderFlags.IsCountSubQuerySupported       = false;
			SqlProviderFlags.CanCombineParameters             = false;
			SqlProviderFlags.IsSybaseBuggyGroupBy             = true;
			SqlProviderFlags.IsCrossJoinSupported             = false;
			SqlProviderFlags.IsSubQueryOrderBySupported       = false;
			SqlProviderFlags.IsDistinctOrderBySupported       = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported = false;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("nchar", (r, i) => DataTools.GetChar(r, i));

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1900, 1, 1));
			SetField<IDataReader,DateTime>("time", (r,i) => GetDateTimeAsTime(r, i));

			_sqlOptimizer = new SybaseSqlOptimizer(SqlProviderFlags);

			Wrapper = new Lazy<SybaseWrappers.ISybaseWrapper>(() => Initialize(), true);
		}

		public             string AssemblyName        => Name == ProviderName.Sybase ? SybaseTools.NativeAssemblyName : "AdoNetCore.AseClient";
		public    override string ConnectionNamespace => Name == ProviderName.Sybase ? "Sybase.Data.AseClient" : "AdoNetCore.AseClient";
		protected override string ConnectionTypeName  => $"{ConnectionNamespace}.AseConnection, {AssemblyName}";
		protected override string DataReaderTypeName  => $"{ConnectionNamespace}.AseDataReader, {AssemblyName}";

#if !NETSTANDARD2_0 && !NETCOREAPP2_1
		public override string DbFactoryProviderName => "Sybase.Data.AseClient";
#endif

		static DateTime GetDateTimeAsTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1900 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		internal readonly Lazy<SybaseWrappers.ISybaseWrapper> Wrapper;

		private SybaseWrappers.ISybaseWrapper Initialize() => SybaseWrappers.Initialize(this);

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			
		}

		#endregion

		#region Overrides

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			type = base.ConvertParameterType(type, dataType);

			// native client BulkCopy cannot stand nullable types
			// AseBulkManager.IsWrongType
			if (Name == ProviderName.Sybase)
			{
				type = type.ToNullableUnderlying();
				if (type == typeof(char) || type == typeof(Guid))
					type = typeof(string);
				else if (type == typeof(TimeSpan))
					type = typeof(DateTime);
			}

			return type;
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SybaseSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			public static readonly SybaseMappingSchema.NativeMappingSchema  NativeMappingSchema  = new SybaseMappingSchema.NativeMappingSchema();
			public static readonly SybaseMappingSchema.ManagedMappingSchema ManagedMappingSchema = new SybaseMappingSchema.ManagedMappingSchema();
		}

		public override MappingSchema MappingSchema => Name == ProviderName.Sybase
			? MappingSchemaInstance.NativeMappingSchema as MappingSchema
			: MappingSchemaInstance.ManagedMappingSchema;

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SybaseSchemaProvider(Name);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.SByte      :
					dataType = dataType.WithDataType(DataType.Int16);
					if (value is sbyte)
						value = (short)(sbyte)value;
					break;

				case DataType.Time       :
					if (value is TimeSpan ts) value = new DateTime(1900, 1, 1) + ts;
					break;

				case DataType.Xml        :
					dataType = dataType.WithDataType(DataType.NVarChar);
						 if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;

				case DataType.Guid       :
					if (value != null)
						value = value.ToString();
					dataType = dataType.WithDataType(DataType.Char);
					parameter.Size = 36;
					break;

				case DataType.Undefined  :
					if (value == null)
						dataType = dataType.WithDataType(DataType.Char);
					break;
				case DataType.Char       :
				case DataType.NChar      :
					if (Name == ProviderName.Sybase)
						if (value is char)
							value = value.ToString();
					break;
			}

			base.SetParameter(dataConnection, parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			SybaseWrappers.AseDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.Text          : type = SybaseWrappers.AseDbType.Text;             break;
				case DataType.NText         : type = SybaseWrappers.AseDbType.Unitext;          break;
				case DataType.Blob          :
				case DataType.VarBinary     : type = SybaseWrappers.AseDbType.VarBinary;        break;
				case DataType.Image         : type = SybaseWrappers.AseDbType.Image;            break;
				case DataType.SmallMoney    : type = SybaseWrappers.AseDbType.SmallMoney;       break;
				case DataType.SmallDateTime : type = SybaseWrappers.AseDbType.SmallDateTime;    break;
				case DataType.Timestamp     : type = SybaseWrappers.AseDbType.TimeStamp;        break;
			}

			if (type != null)
			{
				var param = TryConvertParameter(Wrapper.Value.ParameterType, parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					Wrapper.Value.TypeSetter(param, type.Value);
					return;
				}
			}

			switch (dataType.DataType)
			{
				// fallback types
				case DataType.Text          : parameter.DbType = DbType.AnsiString; break;
				case DataType.NText         : parameter.DbType = DbType.String;     break;
				case DataType.Timestamp     :
				case DataType.Image         : parameter.DbType = DbType.Binary;     break;
				case DataType.SmallMoney    : parameter.DbType = DbType.Currency;   break;
				case DataType.SmallDateTime : parameter.DbType = DbType.DateTime;   break;

				case DataType.VarNumeric    : parameter.DbType = DbType.Decimal;    break;
				case DataType.Binary        : parameter.DbType = DbType.Binary;     break;
				case DataType.Money         : parameter.DbType = DbType.Currency;   break;
				case DataType.DateTime2     : parameter.DbType = DbType.DateTime;   break;
				default                     :
					base.SetParameterType(dataConnection, parameter, dataType);     break;
			}
		}

		#endregion

		#region BulkCopy

		SybaseBulkCopy? _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (_bulkCopy == null)
				_bulkCopy = new SybaseBulkCopy(this);

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SybaseTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		#endregion
	}
}
