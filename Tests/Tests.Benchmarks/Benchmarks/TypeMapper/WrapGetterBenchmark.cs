﻿using System;
using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// benchmark shows big performance degradation and memory allocations for enum accessors
	// due to use of Enum.Parse and boxing. Other types are fine
	// FIX:
	// we should update enum mapper to use value cast for enums with fixed values and probably add some
	// optimizations for others (npgsql)
	public class WrapGetterBenchmark
	{
		private Original.TestClass2 _originalInstance;
		private Wrapped.TestClass2 _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new Original.TestClass2();
			_wrapperInstance = typeMapper.CreateAndWrap(() => new Wrapped.TestClass2());
		}

		[Benchmark]
		public string TypeMapperString()
		{
			return _wrapperInstance.StringProperty;
		}

		[Benchmark(Baseline = true)]
		public string DirectAccessString()
		{
			return _originalInstance.StringProperty;
		}

		[Benchmark]
		public int TypeMapperInt()
		{
			return _wrapperInstance.IntProperty;
		}

		[Benchmark]
		public int DirectAccessInt()
		{
			return _originalInstance.IntProperty;
		}

		[Benchmark]
		public long TypeMapperLong()
		{
			return _wrapperInstance.LongProperty;
		}

		[Benchmark]
		public long DirectAccessLong()
		{
			return _originalInstance.LongProperty;
		}

		[Benchmark]
		public bool TypeMapperBoolean()
		{
			return _wrapperInstance.BooleanProperty;
		}

		[Benchmark]
		public bool DirectAccessBoolean()
		{
			return _originalInstance.BooleanProperty;
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperWrapper()
		{
			return _wrapperInstance.WrapperProperty;
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessWrapper()
		{
			return _originalInstance.WrapperProperty;
		}

		[Benchmark]
		public Wrapped.TestEnum TypeMapperEnum()
		{
			return _wrapperInstance.EnumProperty;
		}

		[Benchmark]
		public Original.TestEnum DirectAccessEnum()
		{
			return _originalInstance.EnumProperty;
		}

		[Benchmark]
		public Version TypeMapperVersion()
		{
			return _wrapperInstance.VersionProperty;
		}

		[Benchmark]
		public Version DirectAccessVersion()
		{
			return _originalInstance.VersionProperty;
		}
	}
}