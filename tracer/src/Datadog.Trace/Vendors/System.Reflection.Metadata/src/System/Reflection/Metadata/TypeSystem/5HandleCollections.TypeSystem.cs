//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
// Decompiled with JetBrains decompiler
// Type: System.Reflection.Metadata.FieldDefinitionHandleCollection
// Assembly: System.Reflection.Metadata, Version=7.0.0.2, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 2EB35F4B-CF50-496F-AFB8-CC6F6F79CB72

using System;
using System.Collections;
using System.Collections.Generic;


#nullable enable
namespace Datadog.Trace.VendoredMicrosoftCode.System.Reflection.Metadata
{
    internal readonly struct FieldDefinitionHandleCollection : 
    IReadOnlyCollection<FieldDefinitionHandle>,
    IEnumerable<FieldDefinitionHandle>,
    IEnumerable
  {

    #nullable disable
    private readonly MetadataReader _reader;
    private readonly int _firstRowId;
    private readonly int _lastRowId;


    #nullable enable
    internal FieldDefinitionHandleCollection(MetadataReader reader)
    {
      this._reader = reader;
      this._firstRowId = 1;
      this._lastRowId = reader.FieldTable.NumberOfRows;
    }

    internal FieldDefinitionHandleCollection(
      MetadataReader reader,
      TypeDefinitionHandle containingType)
    {
      this._reader = reader;
      reader.GetFieldRange(containingType, out this._firstRowId, out this._lastRowId);
    }

    public int Count => this._lastRowId - this._firstRowId + 1;

    public FieldDefinitionHandleCollection.Enumerator GetEnumerator() => new FieldDefinitionHandleCollection.Enumerator(this._reader, this._firstRowId, this._lastRowId);


    #nullable disable
    IEnumerator<FieldDefinitionHandle> IEnumerable<FieldDefinitionHandle>.GetEnumerator() => (IEnumerator<FieldDefinitionHandle>) this.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();


    #nullable enable
    internal struct Enumerator : IEnumerator<FieldDefinitionHandle>, IDisposable, IEnumerator
    {

      #nullable disable
      private readonly MetadataReader _reader;
      private readonly int _lastRowId;
      private int _currentRowId;
      private const int EnumEnded = 16777216;


      #nullable enable
      internal Enumerator(MetadataReader reader, int firstRowId, int lastRowId)
      {
        this._reader = reader;
        this._currentRowId = firstRowId - 1;
        this._lastRowId = lastRowId;
      }

      public FieldDefinitionHandle Current => this._reader.UseFieldPtrTable ? this.GetCurrentFieldIndirect() : FieldDefinitionHandle.FromRowId((int) ((long) this._currentRowId & 16777215L));

      private FieldDefinitionHandle GetCurrentFieldIndirect() => this._reader.FieldPtrTable.GetFieldFor(this._currentRowId & 16777215);

      public bool MoveNext()
      {
        if (this._currentRowId >= this._lastRowId)
        {
          this._currentRowId = 16777216;
          return false;
        }
        ++this._currentRowId;
        return true;
      }

      object IEnumerator.Current => (object) this.Current;

      void IEnumerator.Reset() => throw new NotSupportedException();

      void IDisposable.Dispose()
      {
      }
    }
  }
}
