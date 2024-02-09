//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
#nullable enable
// Decompiled with JetBrains decompiler
// Type: System.Reflection.Metadata.Ecma335.TypeDefOrRefTag
// Assembly: System.Reflection.Metadata, Version=7.0.0.2, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 2EB35F4B-CF50-496F-AFB8-CC6F6F79CB72

using System.Runtime.CompilerServices;

namespace Datadog.Trace.VendoredMicrosoftCode.System.Reflection.Metadata.Ecma335
{
    internal static class TypeDefOrRefTag
  {
    internal const int NumberOfBits = 2;
    internal const int LargeRowSize = 16384;
    internal const uint TypeDef = 0;
    internal const uint TypeRef = 1;
    internal const uint TypeSpec = 2;
    internal const uint TagMask = 3;
    internal const uint TagToTokenTypeByteVector = 1769730;
    internal const TableMask TablesReferenced = TableMask.TypeRef | TableMask.TypeDef | TableMask.TypeSpec;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static EntityHandle ConvertToHandle(uint typeDefOrRefTag)
    {
      uint num1 = 1769730U >> (((int) typeDefOrRefTag & 3) << 3) << 24;
      uint num2 = typeDefOrRefTag >> 2;
      if (num1 == 0U || ((int) num2 & -16777216) != 0)
        Throw.InvalidCodedIndex();
      return new EntityHandle(num1 | num2);
    }
  }
}