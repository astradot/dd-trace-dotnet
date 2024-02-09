//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
// Decompiled with JetBrains decompiler
// Type: System.Reflection.Metadata.ImportDefinitionCollection
// Assembly: System.Reflection.Metadata, Version=7.0.0.2, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 2EB35F4B-CF50-496F-AFB8-CC6F6F79CB72

using System;
using System.Collections;
using System.Collections.Generic;
using Datadog.Trace.VendoredMicrosoftCode.System.Reflection.Internal;
using Datadog.Trace.VendoredMicrosoftCode.System.Reflection.Metadata.Ecma335;


#nullable enable
namespace Datadog.Trace.VendoredMicrosoftCode.System.Reflection.Metadata
{
    internal readonly struct ImportDefinitionCollection : IEnumerable<ImportDefinition>, IEnumerable
  {
    private readonly MemoryBlock _block;

    internal ImportDefinitionCollection(MemoryBlock block) => this._block = block;

    public ImportDefinitionCollection.Enumerator GetEnumerator() => new ImportDefinitionCollection.Enumerator(this._block);


    #nullable disable
    IEnumerator<ImportDefinition> IEnumerable<ImportDefinition>.GetEnumerator() => (IEnumerator<ImportDefinition>) this.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();


    #nullable enable
    internal struct Enumerator : IEnumerator<ImportDefinition>, IDisposable, IEnumerator
    {
      private BlobReader _reader;
      private ImportDefinition _current;

      internal Enumerator(MemoryBlock block)
      {
        this._reader = new BlobReader(block);
        this._current = new ImportDefinition();
      }

      /// <exception cref="T:System.BadImageFormatException">Invalid blob format.</exception>
      public bool MoveNext()
      {
        if (this._reader.RemainingBytes == 0)
          return false;
        ImportDefinitionKind importDefinitionKind = (ImportDefinitionKind) this._reader.ReadByte();
        switch (importDefinitionKind)
        {
          case ImportDefinitionKind.ImportNamespace:
            int kind1 = (int) importDefinitionKind;
            Handle handle1 = (Handle) MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger());
            BlobHandle alias1 = new BlobHandle();
            AssemblyReferenceHandle assembly1 = new AssemblyReferenceHandle();
            Handle typeOrNamespace1 = handle1;
            this._current = new ImportDefinition((ImportDefinitionKind) kind1, alias1, assembly1, typeOrNamespace1);
            break;
          case ImportDefinitionKind.ImportAssemblyNamespace:
            int kind2 = (int) importDefinitionKind;
            AssemblyReferenceHandle assemblyReferenceHandle = MetadataTokens.AssemblyReferenceHandle(this._reader.ReadCompressedInteger());
            Handle handle2 = (Handle) MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger());
            BlobHandle alias2 = new BlobHandle();
            AssemblyReferenceHandle assembly2 = assemblyReferenceHandle;
            Handle typeOrNamespace2 = handle2;
            this._current = new ImportDefinition((ImportDefinitionKind) kind2, alias2, assembly2, typeOrNamespace2);
            break;
          case ImportDefinitionKind.ImportType:
            int kind3 = (int) importDefinitionKind;
            Handle handle3 = (Handle) this._reader.ReadTypeHandle();
            BlobHandle alias3 = new BlobHandle();
            AssemblyReferenceHandle assembly3 = new AssemblyReferenceHandle();
            Handle typeOrNamespace3 = handle3;
            this._current = new ImportDefinition((ImportDefinitionKind) kind3, alias3, assembly3, typeOrNamespace3);
            break;
          case ImportDefinitionKind.ImportXmlNamespace:
          case ImportDefinitionKind.AliasNamespace:
            int kind4 = (int) importDefinitionKind;
            BlobHandle alias4 = MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger());
            Handle handle4 = (Handle) MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger());
            AssemblyReferenceHandle assembly4 = new AssemblyReferenceHandle();
            Handle typeOrNamespace4 = handle4;
            this._current = new ImportDefinition((ImportDefinitionKind) kind4, alias4, assembly4, typeOrNamespace4);
            break;
          case ImportDefinitionKind.ImportAssemblyReferenceAlias:
            this._current = new ImportDefinition(importDefinitionKind, MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger()));
            break;
          case ImportDefinitionKind.AliasAssemblyReference:
            this._current = new ImportDefinition(importDefinitionKind, MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger()), MetadataTokens.AssemblyReferenceHandle(this._reader.ReadCompressedInteger()));
            break;
          case ImportDefinitionKind.AliasAssemblyNamespace:
            this._current = new ImportDefinition(importDefinitionKind, MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger()), MetadataTokens.AssemblyReferenceHandle(this._reader.ReadCompressedInteger()), (Handle) MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger()));
            break;
          case ImportDefinitionKind.AliasType:
            int kind5 = (int) importDefinitionKind;
            BlobHandle alias5 = MetadataTokens.BlobHandle(this._reader.ReadCompressedInteger());
            Handle handle5 = (Handle) this._reader.ReadTypeHandle();
            AssemblyReferenceHandle assembly5 = new AssemblyReferenceHandle();
            Handle typeOrNamespace5 = handle5;
            this._current = new ImportDefinition((ImportDefinitionKind) kind5, alias5, assembly5, typeOrNamespace5);
            break;
          default:
            throw new BadImageFormatException();
        }
        return true;
      }

      public ImportDefinition Current => this._current;

      object IEnumerator.Current => (object) this._current;

      public void Reset()
      {
        this._reader.Reset();
        this._current = new ImportDefinition();
      }

      void IDisposable.Dispose()
      {
      }
    }
  }
}