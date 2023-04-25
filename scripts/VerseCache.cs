using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VerseCache : MonoBehaviour
{
	[SerializeField]
	[Tooltip("Shows debug info per operation in console when true")]
	public bool ShowDebugInfo = true;

	private const int OFFSET_LENGTH = sizeof(long);
	private readonly Dictionary<string, long> fileOffsets = new Dictionary<string, long>();
	[HideInInspector]
	[Tooltip("The full path of the cache file")]
	public string cacheFilePath;

	[Tooltip("The full path of where you want files to go into the cache from (put), or be extracted to (get)")]
	[SerializeField]
	private string _inOutFilepath = "";
	public string InOutFilepath
	{
		get
		{
			// If _inOutFilepath is empty or null, set it to a default value
			if (string.IsNullOrEmpty(_inOutFilepath))
			{
				_inOutFilepath = Application.persistentDataPath + "/";
			}
			// If _inOutFilepath doesn't end with a slash character, add one
			else if (!_inOutFilepath.EndsWith("/"))
			{
				_inOutFilepath += "/";
			}

			return _inOutFilepath;
		}
		set { _inOutFilepath = value; }
	}

	[Tooltip("Input/output filename, depending on isPutInCache and isGetFromCache values -- you would use one or the other at a time (file in or out)")]
	[SerializeField]
	public string InOutFilename = "filename.ext";

	[Tooltip("The path to the folder where the cache file is stored")]
	[SerializeField]
	private string _cachePath = "";

	public string cachePath
	{
		get
		{
			// If _cachePath is empty or null, set it to a default value
			if (string.IsNullOrEmpty(_cachePath))
			{
				_cachePath = Application.persistentDataPath + "/";
			}
			// If _cachePath doesn't end with a slash character, add one
			else if (!_cachePath.EndsWith("/"))
			{
				_cachePath += "/";
			}

			return _cachePath;
		}
		set { _cachePath = value; }
	}

	[Tooltip("Sets path for cache file, to the local user's application data folder on Start()")]
	[SerializeField]
	public bool setCachePathOnStart = true;

	[Tooltip("The name of the cache file")]
	[SerializeField]
	public string cacheName = "cache.sack";

	[Tooltip("True for putting a file into cache")]
	[SerializeField]
	public bool isPutInCache = false;

	[Tooltip("True for extracting a file from cache")]
	[SerializeField]
	public bool isGetFromCache = true;

	[Tooltip("Do things async or not")]
	[SerializeField]
	public bool UseAsync = true;

	[Tooltip("Encrypt the contents of the cache file data chunks or not")]
	[SerializeField]
	public bool UseEncryption = true;

	[Tooltip("The encryption key for use with encryption, required when enabled")]
	[ByteTextBox]
	public string EncryptionKey;

	[Tooltip("The encryption IV for use with encryption, required when enabled")]
	[ByteTextBox]
	public string EncryptionIV;

	
	// The byte arrays used to store the encryption key and initialization vector
	private byte[] encryptionKeyArray;
	private byte[] initializationVectorArray;

	// The property to access the encryption key byte array
	public byte[] EncryptionKeyArray
	{
		get
		{
			// If the encryption key array has not been set and there is an encryption key string,
			// convert the encryption key string to a byte array and set the encryption key array
			if (encryptionKeyArray == null && !string.IsNullOrEmpty(EncryptionKey))
			{
				encryptionKeyArray = System.Convert.FromBase64String(EncryptionKey);
			}

			return encryptionKeyArray;
		}
	}

	// The property to access the initialization vector byte array
	public byte[] InitializationVectorArray
	{
		get
		{
			// If the initialization vector array has not been set and there is an initialization vector string,
			// convert the initialization vector string to a byte array and set the initialization vector array
			if (initializationVectorArray == null && !string.IsNullOrEmpty(EncryptionIV))
			{
				initializationVectorArray = System.Convert.FromBase64String(EncryptionIV);
			}

			return initializationVectorArray;
		}
	}

	void Start()
	{
		// Set the cache path to the user's application data folder if setCachePathOnStart is true
		if (setCachePathOnStart) cachePath = Application.persistentDataPath.ToString().Replace("\\","/") + "/";
	}

	// Set the encryption key and IV from byte arrays
	// If the key or IV arrays are null or empty, use default Aes values
	public void SetEncryptionData(byte[] key, byte[] iv)
	{
		EncryptionKey = System.Convert.ToBase64String(key);
		EncryptionIV = System.Convert.ToBase64String(iv);

		encryptionKeyArray = null; // Invalidate cached byte array
		initializationVectorArray = null; // Invalidate cached byte array
	}

	public async Task DoAction()
	{
		CacheCheck();
		if (isPutInCache)
		{
		    //read the file data from disk
		    byte[] indata = File.ReadAllBytes(InOutFilepath + "/" + InOutFilename);
		    byte[] encData = null;
		    if (UseEncryption) encData = Encrypt(indata, EncryptionKeyArray, InitializationVectorArray); //encryption needs to be made async still
		    if (!UseEncryption) encData = indata;

		    //save it into the cache
		    if (!UseAsync) StoreFile(InOutFilename, encData);
		    if (UseAsync) await SaveFileAsync(InOutFilename, encData);
		}

		if (isGetFromCache)
		{
			//retrieve the bytedata for the specific file name from the cache/sack
			byte[] oData = null;
			if (!UseAsync) oData = GetFile(InOutFilename);
			if (UseAsync) oData = await LoadFileAsync(InOutFilename);

			byte[] data = null;
			if (UseEncryption) data = Decrypt(oData, EncryptionKeyArray, InitializationVectorArray);  //decryption needs to be made async still
			if (!UseEncryption) data = oData;
			//write the file data we pulled from the cache to the same location as our cache/sack file
			File.WriteAllBytes(InOutFilepath + "/" +  InOutFilename, data);
		}
	}

	// Encrypts the given byte array with the specified encryption key and initialization vector (IV)
	public static byte[] Encrypt(byte[] input, byte[] key, byte[] iv)
	{
		using (Aes aes = Aes.Create())
		{
			aes.Key = key;
			aes.IV = iv;

			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
				{
					cryptoStream.Write(input, 0, input.Length);
					cryptoStream.FlushFinalBlock();
				}

				return memoryStream.ToArray();
			}
		}
	}

	// Decrypts the given byte array with the specified encryption key and initialization vector (IV)
	public static byte[] Decrypt(byte[] input, byte[] key, byte[] iv)
	{
		using (Aes aes = Aes.Create())
		{
			aes.Key = key;
			aes.IV = iv;

			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
				{
					cryptoStream.Write(input, 0, input.Length);
					cryptoStream.FlushFinalBlock();
				}

				return memoryStream.ToArray();
			}
		}
	}

	// Checks the cache file path and sets the cacheFilePath variable accordingly
	public void CacheCheck()
	{
		cacheFilePath = cachePath + cacheName;
	}

	// Generate a new AES encryption key and initialization vector
	public void GenerateKeyAndIV(out byte[] key, out byte[] iv)
	{
		using (Aes aes = Aes.Create())
		{
			key = aes.Key;
			iv = aes.IV;
		}
	}

	public void GenerateRandomKeyIV()
	{
		byte[] outKey = null;
		byte[] outIv = null;
		// Generate a new AES encryption key and initialization vector
		GenerateKeyAndIV(out outKey, out outIv);
		// Set the encryption key and IV from byte arrays
		SetEncryptionData(outKey, outIv);
	}

	// Stores a file in the cache with the given name and byte data
	public void StoreFile(string name, byte[] data)
	{
		// Open a FileStream to the cache file in append mode
		using (var stream = new FileStream(cacheFilePath, FileMode.Append, FileAccess.Write))
		{
			// Convert the filename to a byte array
			var nameBytes = Encoding.UTF8.GetBytes(name);

			// Convert the length of the filename to a byte array
			var nameLengthBytes = BitConverter.GetBytes(nameBytes.Length);

			// Convert the length of the file data to a byte array
			var lengthBytes = BitConverter.GetBytes(data.Length);

			// Convert the current position of the FileStream to a byte array (to use as the offset)
			var offsetBytes = BitConverter.GetBytes(stream.Position);

			// Write the length of the filename, the filename, the length of the file data, the offset, and the file data to the FileStream
			stream.Write(nameLengthBytes, 0, nameLengthBytes.Length);
			stream.Write(nameBytes, 0, nameBytes.Length);
			stream.Write(lengthBytes, 0, lengthBytes.Length);
			stream.Write(offsetBytes, 0, offsetBytes.Length);
			stream.Write(data, 0, data.Length);
			stream.Flush();

			// Store the file offset in the fileOffsets dictionary
			fileOffsets[name] = stream.Position;

			// Log that the file was stored in the cache if debug info is enabled
			if (ShowDebugInfo) Debug.Log("VerseCache - StoreFile: '" + name + "' saved to cache");
		}
	}

	// Asynchronously store a file in the cache
	public async Task StoreFileAsync(string name, byte[] data)
	{
		await Task.Run(() =>
		{
			// Open the cache file in append mode and get its current position
			using (var stream = new FileStream(cacheFilePath, FileMode.Append, FileAccess.Write))
			{
				// Encode the filename as UTF-8 and get its length in bytes
				var nameBytes = Encoding.UTF8.GetBytes(name);
				var nameLengthBytes = BitConverter.GetBytes(nameBytes.Length);

				// Get the length of the data and the current offset position in the file
				var lengthBytes = BitConverter.GetBytes(data.Length);
				var offsetBytes = BitConverter.GetBytes(stream.Position);

				// Write the filename length, filename bytes, data length, and offset to the file
				stream.Write(nameLengthBytes, 0, nameLengthBytes.Length);
				stream.Write(nameBytes, 0, nameBytes.Length);
				stream.Write(lengthBytes, 0, lengthBytes.Length);
				stream.Write(offsetBytes, 0, offsetBytes.Length);

				// Write the data to the file and flush the stream
				stream.Write(data, 0, data.Length);
				stream.Flush();

				// Update the file offsets dictionary with the new file's position
				fileOffsets[name] = stream.Position;

				// Log a message to the console if debug info is enabled
				if (ShowDebugInfo) Debug.Log("VerseCache - StoreFileAsync: '" + name + "' saved to cache");
			}
		});
	}


	// Asynchronous method to store file in the cache
	public async Task SaveFileAsync(string filename, byte[] encData)
	{
		await StoreFileAsync(filename, encData);
	}

	public byte[] GetFile(string name)
	{
		// If the specified file is not found in the cache, return null
		if (!fileOffsets.TryGetValue(name, out var offset))
		{
			if (ShowDebugInfo) Debug.LogError("VerseCache - GetFile: File not found in cache: " + name);
			return null;
		}

		// Open the cache file for reading
		using (var stream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read))
		using (var reader = new BinaryReader(stream))
		{
			// Seek to the position in the file where the specified file's data is located
			stream.Seek(offset, SeekOrigin.Begin);

			// Read the length of the file name
			var nameLength = reader.ReadInt32();

			// Read the bytes of the file name and convert them to a string
			var nameBytes = reader.ReadBytes(nameLength);
			var length = reader.ReadInt32();
			var offsetval = reader.ReadInt64();
			var buffer = new byte[length];
			stream.Read(buffer, 0, length);

			// If debug info is enabled, log that the file was retrieved from the cache
			if (ShowDebugInfo) Debug.Log("VerseCache - GetFile: '" + name + "' retrieved from cache");

			// Return the contents of the file as a byte array
			return buffer;
		}
	}


	// Asynchronous method to get file data from the cache with the specified name
	public async Task<byte[]> GetFileAsync(string name)
	{
		// Check if the file with the specified name exists in the cache and get its offset
		if (!fileOffsets.TryGetValue(name, out var offset))
		{
			if (ShowDebugInfo) Debug.LogError("VerseCache - GetFile: File not found in cache: " + name);
			return null;
		}

		byte[] buffer = null;
		await Task.Run(() =>
		{
			// Read the file data from the cache file asynchronously
			using (var stream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read))
			using (var reader = new BinaryReader(stream))
			{
				stream.Seek(offset, SeekOrigin.Begin);

				var nameLength = reader.ReadInt32();
				var nameBytes = reader.ReadBytes(nameLength);
				var length = reader.ReadInt32();
				var offsetval = reader.ReadInt64();
				buffer = new byte[length];
				stream.Read(buffer, 0, length);
			}
		});

		// Log that the file data has been retrieved from the cache
		if (ShowDebugInfo) Debug.Log("VerseCache - GetFile: '" + name + "' retrieved from cache");

		// Return the file data
		return buffer;
	}

	// Asynchronous method to get file from the cache
	public async Task<byte[]> LoadFileAsync(string filename)
	{
		var data = await GetFileAsync(filename);
		return data;
	}

	// Create a new cache file at the specified path
	private void CreateCacheFile()
	{
		using (File.Create(cacheFilePath)) { }
	}

	// Load file offsets from the cache file and store them in the fileOffsets dictionary
	private void LoadFileOffsets()
	{
		// If the cache file is empty, do nothing
		if (new FileInfo(cacheFilePath).Length == 0)
		{
			if (ShowDebugInfo) Debug.Log("VerseCache - Cache file is empty, created new empty.");
			return;
		}

		using (var stream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read))
		using (var reader = new BinaryReader(stream))
		{
			// Read file offsets from the cache file
			while (reader.BaseStream.Position < stream.Length)
			{
				var nameLength = reader.ReadInt32();
				var nameBytes = reader.ReadBytes(nameLength);
				var name = Encoding.UTF8.GetString(nameBytes);
				var length = reader.ReadInt32();
				var offset = reader.ReadInt64();
				var ddata = reader.ReadBytes(length);
				fileOffsets[name] = offset;
			}
		}
	}
	public void ListFiles()
	{
		try
		{
			// Open the cache file for reading
			using (var stream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read))
			using (var reader = new BinaryReader(stream))
			{
				if (ShowDebugInfo) Debug.Log("VerseCache - Listing cache contents from: " + cacheFilePath);

				// Read through the entire file, logging the name, offset, and length of each stored file
				while (reader.BaseStream.Position < stream.Length)
				{
					var nameLength = reader.ReadInt32();
					var nameBytes = reader.ReadBytes(nameLength);
					var name = Encoding.UTF8.GetString(nameBytes);
					var length = reader.ReadInt32();
					var offset = reader.ReadInt64();
					var ddata = reader.ReadBytes(length);

					if (ShowDebugInfo) Debug.Log($"Cache filename: {name}, Offset: {offset}, nameLength: {nameLength}");

					// Add the file name and offset to the dictionary for future reference
					fileOffsets[name] = offset;
				}
			}
		}
		catch
		{
			// If the file doesn't exist, create it
			CreateCacheFile();
			ListFiles();
		}
	}

	public void Load()
	{
		// Load the offsets for each file from the cache file
		LoadFileOffsets();
	}

	public void Clear()
	{
		// Clear the dictionary of file offsets and create a new, empty cache file
		fileOffsets.Clear();
		CreateCacheFile();
	}
	
	// Display a list of all the files in the cache file in the Unity Editor's console.
	public void GUIListFiles()
	{
		CacheCheck();
		ListFiles();
	}
}

public class ByteTextBoxAttribute : PropertyAttribute
{
}
