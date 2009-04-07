﻿/*
  Copyright (C) 2008 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using IKVM.Reflection.Emit;
using IKVM.Reflection.Emit.Impl;

namespace IKVM.Reflection.Emit.Writer
{
	static class ModuleWriter
	{
		private const int versionInfoResourceHeaderLength = 0x58;

		internal static void WriteModule(string directory, StrongNameKeyPair keyPair, ModuleBuilder moduleBuilder, PEFileKinds fileKind, PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine, ByteBuffer versionInfoData, int entryPointToken)
		{
			moduleBuilder.FixupMethodBodyTokens();

			moduleBuilder.Tables.Module.Add(0, moduleBuilder.Strings.Add(moduleBuilder.moduleName), moduleBuilder.Guids.Add(moduleBuilder.ModuleVersionId), 0, 0);

			if (moduleBuilder.UserStrings.IsEmpty)
			{
				// for compat with Ref.Emit, if there aren't any user strings, we add one
				moduleBuilder.UserStrings.Add(" ");
			}

			string fileName = moduleBuilder.fileName;
			if (directory != null)
			{
				fileName = Path.Combine(directory, fileName);
			}
			using (FileStream fs = new FileStream(fileName, FileMode.Create))
			{
				PEWriter writer = new PEWriter(fs);
				switch (imageFileMachine)
				{
					case ImageFileMachine.I386:
						writer.Headers.FileHeader.Machine = IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_I386;
						writer.Headers.FileHeader.Characteristics |= IMAGE_FILE_HEADER.IMAGE_FILE_32BIT_MACHINE;
						break;
					case ImageFileMachine.AMD64:
						writer.Headers.FileHeader.Machine = IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_AMD64;
						writer.Headers.FileHeader.Characteristics |= IMAGE_FILE_HEADER.IMAGE_FILE_LARGE_ADDRESS_AWARE;
						writer.Headers.FileHeader.SizeOfOptionalHeader = 0xF0;
						writer.Headers.OptionalHeader.Magic = IMAGE_OPTIONAL_HEADER.IMAGE_NT_OPTIONAL_HDR64_MAGIC;
						writer.Headers.OptionalHeader.SizeOfStackReserve = 0x400000;
						writer.Headers.OptionalHeader.SizeOfStackCommit = 0x4000;
						writer.Headers.OptionalHeader.SizeOfHeapCommit = 0x2000;
						break;
					case ImageFileMachine.IA64:
						writer.Headers.FileHeader.Machine = IMAGE_FILE_HEADER.IMAGE_FILE_MACHINE_IA64;
						writer.Headers.FileHeader.Characteristics |= IMAGE_FILE_HEADER.IMAGE_FILE_LARGE_ADDRESS_AWARE;
						writer.Headers.FileHeader.SizeOfOptionalHeader = 0xF0;
						writer.Headers.OptionalHeader.Magic = IMAGE_OPTIONAL_HEADER.IMAGE_NT_OPTIONAL_HDR64_MAGIC;
						writer.Headers.OptionalHeader.SizeOfStackReserve = 0x400000;
						writer.Headers.OptionalHeader.SizeOfStackCommit = 0x4000;
						writer.Headers.OptionalHeader.SizeOfHeapCommit = 0x2000;
						break;
					default:
						throw new ArgumentOutOfRangeException("imageFileMachine");
				}
				if (fileKind == PEFileKinds.Dll)
				{
					writer.Headers.FileHeader.Characteristics |= IMAGE_FILE_HEADER.IMAGE_FILE_DLL;
				}

				switch (fileKind)
				{
					case PEFileKinds.WindowApplication:
						writer.Headers.OptionalHeader.Subsystem = IMAGE_OPTIONAL_HEADER.IMAGE_SUBSYSTEM_WINDOWS_GUI;
						break;
					default:
						writer.Headers.OptionalHeader.Subsystem = IMAGE_OPTIONAL_HEADER.IMAGE_SUBSYSTEM_WINDOWS_CUI;
						break;
				}
				writer.Headers.OptionalHeader.DllCharacteristics =
					IMAGE_OPTIONAL_HEADER.IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE |
					IMAGE_OPTIONAL_HEADER.IMAGE_DLLCHARACTERISTICS_NO_SEH |
					IMAGE_OPTIONAL_HEADER.IMAGE_DLLCHARACTERISTICS_NX_COMPAT |
					IMAGE_OPTIONAL_HEADER.IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE;

				CliHeader cliHeader = new CliHeader();
				cliHeader.Cb = 0x48;
				cliHeader.MajorRuntimeVersion = 2;
				cliHeader.MinorRuntimeVersion = 5;
				if ((portableExecutableKind & PortableExecutableKinds.ILOnly) != 0)
				{
					cliHeader.Flags |= CliHeader.COMIMAGE_FLAGS_ILONLY;
				}
				if ((portableExecutableKind & PortableExecutableKinds.Required32Bit) != 0)
				{
					cliHeader.Flags |= CliHeader.COMIMAGE_FLAGS_32BITREQUIRED;
				}
				if (keyPair != null)
				{
					cliHeader.Flags |= CliHeader.COMIMAGE_FLAGS_STRONGNAMESIGNED;
				}
				if (moduleBuilder.IsPseudoToken(entryPointToken))
				{
					entryPointToken = moduleBuilder.ResolvePseudoToken(entryPointToken);
				}
				cliHeader.EntryPointToken = (uint)entryPointToken;
				TextSection code = new TextSection(writer, cliHeader, moduleBuilder);

				// Import Directory
				writer.Headers.OptionalHeader.DataDirectory[1].VirtualAddress = code.ImportDirectoryRVA;
				writer.Headers.OptionalHeader.DataDirectory[1].Size = code.ImportDirectoryLength;

				// Import Address Table Directory
				writer.Headers.OptionalHeader.DataDirectory[12].VirtualAddress = code.ImportAddressTableRVA;
				writer.Headers.OptionalHeader.DataDirectory[12].Size = code.ImportAddressTableLength;

				// COM Descriptor Directory
				writer.Headers.OptionalHeader.DataDirectory[14].VirtualAddress = code.ComDescriptorRVA;
				writer.Headers.OptionalHeader.DataDirectory[14].Size = code.ComDescriptorLength;

				// Debug Directory
				if (code.DebugDirectoryLength != 0)
				{
					writer.Headers.OptionalHeader.DataDirectory[6].VirtualAddress = code.DebugDirectoryRVA;
					writer.Headers.OptionalHeader.DataDirectory[6].Size = code.DebugDirectoryLength;
				}

				writer.Headers.FileHeader.NumberOfSections = 2;

				if (moduleBuilder.initializedData.Length != 0)
				{
					writer.Headers.FileHeader.NumberOfSections++;
				}

				if (versionInfoData != null)
				{
					writer.Headers.FileHeader.NumberOfSections++;
				}

				SectionHeader text = new SectionHeader();
				text.Name = ".text";
				text.VirtualAddress = code.BaseRVA;
				text.VirtualSize = (uint)code.Length;
				text.PointerToRawData = code.PointerToRawData;
				text.SizeOfRawData = writer.ToFileAlignment((uint)code.Length);
				text.Characteristics = SectionHeader.IMAGE_SCN_CNT_CODE | SectionHeader.IMAGE_SCN_MEM_EXECUTE | SectionHeader.IMAGE_SCN_MEM_READ;

				SectionHeader sdata = new SectionHeader();
				sdata.Name = ".sdata";
				sdata.VirtualAddress = text.VirtualAddress + writer.ToSectionAlignment(text.VirtualSize);
				sdata.VirtualSize = (uint)moduleBuilder.initializedData.Length;
				sdata.PointerToRawData = text.PointerToRawData + text.SizeOfRawData;
				sdata.SizeOfRawData = writer.ToFileAlignment((uint)moduleBuilder.initializedData.Length);
				sdata.Characteristics = SectionHeader.IMAGE_SCN_CNT_INITIALIZED_DATA | SectionHeader.IMAGE_SCN_MEM_READ | SectionHeader.IMAGE_SCN_MEM_WRITE;

				SectionHeader rsrc = new SectionHeader();
				rsrc.Name = ".rsrc";
				rsrc.VirtualAddress = sdata.VirtualAddress + writer.ToSectionAlignment(sdata.VirtualSize);
				rsrc.PointerToRawData = sdata.PointerToRawData + sdata.SizeOfRawData;
				if (versionInfoData != null)
				{
					rsrc.VirtualSize = (uint)versionInfoData.Length + versionInfoResourceHeaderLength;
					rsrc.SizeOfRawData = writer.ToFileAlignment((uint)versionInfoData.Length + versionInfoResourceHeaderLength);
				}
				rsrc.Characteristics = SectionHeader.IMAGE_SCN_MEM_READ | SectionHeader.IMAGE_SCN_CNT_INITIALIZED_DATA;

				if (rsrc.SizeOfRawData != 0)
				{
					// Resource Directory
					writer.Headers.OptionalHeader.DataDirectory[2].VirtualAddress = rsrc.VirtualAddress;
					writer.Headers.OptionalHeader.DataDirectory[2].Size = rsrc.VirtualSize;
				}

				SectionHeader reloc = new SectionHeader();
				reloc.Name = ".reloc";
				reloc.VirtualAddress = rsrc.VirtualAddress + writer.ToSectionAlignment(rsrc.VirtualSize);
				reloc.VirtualSize = 12;
				reloc.PointerToRawData = rsrc.PointerToRawData + rsrc.SizeOfRawData;
				reloc.SizeOfRawData = writer.ToFileAlignment(reloc.VirtualSize);
				reloc.Characteristics = SectionHeader.IMAGE_SCN_MEM_READ | SectionHeader.IMAGE_SCN_CNT_INITIALIZED_DATA | SectionHeader.IMAGE_SCN_MEM_DISCARDABLE;

				// Base Relocation Directory
				writer.Headers.OptionalHeader.DataDirectory[5].VirtualAddress = reloc.VirtualAddress;
				writer.Headers.OptionalHeader.DataDirectory[5].Size = reloc.VirtualSize;

				writer.Headers.OptionalHeader.SizeOfCode = text.SizeOfRawData;
				writer.Headers.OptionalHeader.SizeOfInitializedData = sdata.SizeOfRawData + rsrc.SizeOfRawData + reloc.SizeOfRawData;
				writer.Headers.OptionalHeader.SizeOfUninitializedData = 0;
				writer.Headers.OptionalHeader.SizeOfImage = reloc.VirtualAddress + writer.ToSectionAlignment(reloc.VirtualSize);
				writer.Headers.OptionalHeader.SizeOfHeaders = text.PointerToRawData;
				writer.Headers.OptionalHeader.BaseOfCode = code.BaseRVA;
				writer.Headers.OptionalHeader.BaseOfData = sdata.VirtualAddress;

				if (imageFileMachine == ImageFileMachine.IA64)
				{
					// apparently for IA64 AddressOfEntryPoint points to the address of the entry point
					// (i.e. there is an additional layer of indirection), so we add the offset to the pointer
					writer.Headers.OptionalHeader.AddressOfEntryPoint = code.StartupStubRVA + 0x20;
				}
				else
				{
					writer.Headers.OptionalHeader.AddressOfEntryPoint = code.StartupStubRVA;
				}

				writer.WritePEHeaders();
				writer.WriteSectionHeader(text);
				if (sdata.SizeOfRawData != 0)
				{
					writer.WriteSectionHeader(sdata);
				}
				if (rsrc.SizeOfRawData != 0)
				{
					writer.WriteSectionHeader(rsrc);
				}
				writer.WriteSectionHeader(reloc);

				MetadataWriter mw = new MetadataWriter(moduleBuilder, fs);

				fs.Seek(text.PointerToRawData, SeekOrigin.Begin);
				code.Write(mw, (int)sdata.VirtualAddress);

				fs.Seek(sdata.PointerToRawData, SeekOrigin.Begin);
				mw.Write(moduleBuilder.initializedData);

				if (versionInfoData != null)
				{
					fs.Seek(rsrc.PointerToRawData, SeekOrigin.Begin);
					WriteVersionInfoResource(mw, (int)rsrc.VirtualAddress, versionInfoData);
				}

				fs.Seek(reloc.PointerToRawData, SeekOrigin.Begin);
				// .reloc section
				uint relocAddress = code.StartupStubRVA;
				switch (imageFileMachine)
				{
					case ImageFileMachine.I386:
					case ImageFileMachine.AMD64:
						relocAddress += 2;
						break;
					case ImageFileMachine.IA64:
						relocAddress += 0x20;
						break;
				}
				uint pageRVA = relocAddress & ~0xFFFU;
				mw.Write(pageRVA);	// PageRVA
				mw.Write(0x000C);	// Block Size
				if (imageFileMachine == ImageFileMachine.I386)
				{
					mw.Write(0x3000 + relocAddress - pageRVA);				// Type / Offset
				}
				else if (imageFileMachine == ImageFileMachine.AMD64)
				{
					mw.Write(0xA000 + relocAddress - pageRVA);				// Type / Offset
				}
				else if (imageFileMachine == ImageFileMachine.IA64)
				{
					// on IA64 the StartupStubRVA is 16 byte aligned, so these two addresses won't cross a page boundary
					mw.Write((short)(0xA000 + relocAddress - pageRVA));		// Type / Offset
					mw.Write((short)(0xA000 + relocAddress - pageRVA + 8));	// Type / Offset
				}

				// file alignment
				mw.Write(new byte[writer.Headers.OptionalHeader.FileAlignment - reloc.VirtualSize]);

				// do the strong naming
				if (keyPair != null)
				{
					StrongName(fs, keyPair, writer.HeaderSize, text.PointerToRawData, code.StrongNameSignatureRVA - text.VirtualAddress + text.PointerToRawData, code.StrongNameSignatureLength);
				}
			}

			if (moduleBuilder.symbolWriter != null)
			{
				moduleBuilder.WriteSymbolTokenMap();
				moduleBuilder.symbolWriter.Close();
			}
		}

		private static void StrongName(FileStream fs, StrongNameKeyPair keyPair, uint headerLength, uint textSectionFileOffset, uint strongNameSignatureFileOffset, uint strongNameSignatureLength)
		{
			SHA1Managed hash = new SHA1Managed();
			using (CryptoStream cs = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write))
			{
				fs.Seek(0, SeekOrigin.Begin);
				byte[] buf = new byte[8192];
				HashChunk(fs, cs, buf, (int)headerLength);
				fs.Seek(textSectionFileOffset, SeekOrigin.Begin);
				HashChunk(fs, cs, buf, (int)(strongNameSignatureFileOffset - textSectionFileOffset));
				fs.Seek(strongNameSignatureLength, SeekOrigin.Current);
				HashChunk(fs, cs, buf, (int)(fs.Length - (strongNameSignatureFileOffset + strongNameSignatureLength)));
			}
			using (RSA rsa = CryptoHack.CreateRSA(keyPair))
			{
				RSAPKCS1SignatureFormatter sign = new RSAPKCS1SignatureFormatter(rsa);
				sign.SetHashAlgorithm("SHA1");
				byte[] signature = sign.CreateSignature(hash.Hash);
				Array.Reverse(signature);
				Debug.Assert(signature.Length == strongNameSignatureLength);
				fs.Seek(strongNameSignatureFileOffset, SeekOrigin.Begin);
				fs.Write(signature, 0, signature.Length);
			}

			// compute the PE checksum
			fs.Seek(0, SeekOrigin.Begin);
			int count = (int)fs.Length / 4;
			BinaryReader br = new BinaryReader(fs);
			long sum = 0;
			for (int i = 0; i < count; i++)
			{
				sum += br.ReadUInt32();
				int carry = (int)(sum >> 32);
				sum &= 0xFFFFFFFFU;
				sum += carry;
			}
			while ((sum >> 16) != 0)
			{
				sum = (sum & 0xFFFF) + (sum >> 16);
			}
			sum += fs.Length;

			// write the PE checksum, note that it is always at offset 0xD8 in the file
			ByteBuffer bb = new ByteBuffer(4);
			bb.Write((int)sum);
			fs.Seek(0xD8, SeekOrigin.Begin);
			bb.WriteTo(fs);
		}

		internal static void HashChunk(FileStream fs, CryptoStream cs, byte[] buf, int length)
		{
			while (length > 0)
			{
				int read = fs.Read(buf, 0, Math.Min(buf.Length, length));
				cs.Write(buf, 0, read);
				length -= read;
			}
		}

		private static void WriteVersionInfoResource(MetadataWriter mw, int rsrcRVA, ByteBuffer versionInfoData)
		{
			mw.Write(new byte[] {
			  0x00, 0x00, 0x00, 0x00,	// Characteristics
			  0x00, 0x00, 0x00, 0x00,	// Time/Date Stamp
			  0x00, 0x00,				// Major Version
			  0x00, 0x00,				// Minor Version
			  0x00, 0x00,				// Number of Name Entries
			  0x01, 0x00,				// Number of ID Entries

			  // Resource Directory Entry
			  0x10, 0x00, 0x00, 0x00,	// Integer ID
			  0x18, 0x00, 0x00, 0x80,   // Subdirectory RVA (offset 0x18)

			  // Resource Directory Table (at offset 0x18)
			  0x00, 0x00, 0x00, 0x00,	// Characterics
			  0x00, 0x00, 0x00, 0x00,	// Time/Date Stamp
			  0x00, 0x00,				// Major Version
			  0x00, 0x00,				// Minor Version
			  0x00, 0x00,				// Number of Name Entries
			  0x01, 0x00,				// Number of ID Entries

			  // Resource Directory Entry
			  0x01, 0x00, 0x00, 0x00,	// Integer ID
			  0x30, 0x00, 0x00, 0x80,	// Subdirectory RVA (offset 0x30)

			  // Resource Directory Table (at offset 0x30)
			  0x00, 0x00, 0x00, 0x00,	// Characterics
			  0x00, 0x00, 0x00, 0x00,	// Time/Date Stamp
			  0x00, 0x00,				// Major Version
			  0x00, 0x00,				// Minor Version
			  0x00, 0x00,				// Number of Name Entries
			  0x01, 0x00,				// Number of ID Entries

			  // Resource Directory Entry  
			  0x00, 0x00, 0x00, 0x00,	// Integer ID
			  0x48, 0x00, 0x00, 0x00,	// Date Entry RVA (offset 0x48)
			});

			// Data Entry (at offset 0x48)
			mw.Write(versionInfoResourceHeaderLength + rsrcRVA);	// Data RVA
			mw.Write(versionInfoData.Length);	// Size
			mw.Write(0);	// Codepage
			mw.Write(0);	// Reserved

			mw.Write(versionInfoData);
		}
	}
}
