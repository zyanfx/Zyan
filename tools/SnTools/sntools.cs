// Strong name tools
// Written by Alexey Yakovlev <yallie@yandex.ru>

// This file is a part of the following projects:
// Exepack.NET - http://exepack.codeplex.com
// Zyan Communications Framework - http://zyan.codeplex.com

// Compilation:
// C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc sntools.cs

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

class StrongNameTools
{
	const int KeySize = 2048;
	const string KeyName = "zyan.snk";
	static Regex publicKeyRegex = new Regex(@"(assembly: InternalsVisibleTo\(""[^""]+,\s*PublicKey)=[0123456789abcdefABCDEF]*""");

	static int Main(string[] args)
	{
		var command = args.FirstOrDefault();

		try
		{
			switch (command)
			{
				case "g":
				case "G":
					return GenerateKeyFile(args.Skip(1).FirstOrDefault());

				case "x":
				case "X":
					return ExtractPublicKey(args.Skip(1).FirstOrDefault(), args.Skip(1).LastOrDefault());

				case "d":
				case "D":
					return DisplayToken(args.Skip(1).FirstOrDefault());

				case "p":
				case "P":
					return PatchFile(args.Skip(1).FirstOrDefault(), args.Length > 2 ? args.Skip(1).LastOrDefault() : String.Empty);

				default:
					return DisplayHelp();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Unhandled exception: {0}", ex.GetType());
			Console.WriteLine("Message: {0}", ex.Message);
			Console.WriteLine("Stack trace:{0}{1}", Environment.NewLine, ex.StackTrace);
			return -1;
		}
	}

	static int DisplayHelp()
	{
		var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
		Console.WriteLine("Syntax:");
		Console.WriteLine("  {0} d [publicKeyFileName] -- display public key file contents", exeName);
		Console.WriteLine("  {0} g [keyPairFileName] -- generate key pair", exeName);
		Console.WriteLine("  {0} x [keyPairFileName [publicKeyFileName]] -- extract public key", exeName);
		Console.WriteLine("  {0} p fileName.cs [publicKeyFileName] -- patch attributes in fileName.cs", exeName);
		return -2;
	}

	static string GetFileName(string fileName, string defaultName = KeyName)
	{
		return String.IsNullOrEmpty(fileName) ? defaultName : fileName;
	}

	static int GenerateKeyFile(string fileName)
	{
		Console.WriteLine("Generating key file: {0}...", fileName);

		// do not overwrite if the file already exists
		var keyFile = GetFileName(fileName);
		if (File.Exists(keyFile))
		{
			Console.WriteLine("Key file {0} already exists.", fileName);
			return 0;
		}

		// generate key pair and export it as a blob
		var parms = new CspParameters();
		parms.KeyNumber = 2;
		var provider = new RSACryptoServiceProvider(KeySize, parms);
		var array = provider.ExportCspBlob(!provider.PublicOnly);

		File.WriteAllBytes(keyFile, array);
		Console.WriteLine("Key file {0} generated.", fileName);
		return 0;
	}

	static int ExtractPublicKey(string inFile, string outFile)
	{
		var inputName = GetFileName(inFile);
		var outputName = GetFileName(outFile);
		if (inputName == outputName)
		{
			outputName = Path.ChangeExtension(outputName, "public.snk");
		}

		// do not overwrite output file
		Console.WriteLine("Extracting public key from file {0}...", inFile);
		if (File.Exists(outputName))
		{
			Console.WriteLine("Output file {0} already exists.", outputName);
			return 0;
		}

		// read key pair blob and extract public key
		var array = File.ReadAllBytes(inputName);
		var snk = new StrongNameKeyPair(array);
		var publicKey = snk.PublicKey;

		File.WriteAllBytes(outputName, publicKey);
		Console.WriteLine("Public key file {0} generated.", outputName);
		return 0;
	}

	static int DisplayToken(string fileName)
	{
		Console.WriteLine("Key: {0}", ReadToken(fileName));
		return -2;
	}

	static string ReadToken(string fileName)
	{
		// read public key file and convert it to a string token
		var keyName = GetFileName(fileName);
		var data = File.ReadAllBytes(keyName);
		var sb = new StringBuilder();
		Array.ForEach(data, b => sb.AppendFormat("{0:x2}", b));
		return sb.ToString();
	}

	static int PatchFile(string srcFileName, string publicKeyFile)
	{
		Console.WriteLine("Patching the file: {0}...", srcFileName);

		// prepare new public key token
		var tempFileName = srcFileName + ".sntools-patch-tmp";
		var keyFile = GetFileName(publicKeyFile, Path.ChangeExtension(KeyName, "public.snk"));
		var token = ReadToken(keyFile);

		// extract text and patch
		var text = File.ReadAllText(srcFileName);
		var newText = publicKeyRegex.Replace(text, "$1=" + token + "\"");
		if (newText == text)
		{
			// public key wasn't changed
			Console.WriteLine("Public key wasn't changed: file {0} is not modified.", srcFileName);
			return 0;
		}

		// flush data and replace an old file with a newer version
		File.WriteAllText(tempFileName, newText);
		File.Delete(srcFileName);
		File.Move(tempFileName, srcFileName);
		Console.WriteLine("Patched the file: {0}.", srcFileName);
		return 0;
	}
}
