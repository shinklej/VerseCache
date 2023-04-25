# VerseCache

VerseCache is a secure cache system for Unity developers creating online games that will contain random UGC (user-generated content) at runtime. This system allows developers to store avatar assets, scene assets, sounds, music, mesh, textures, video, or any other type of content they need to download and cache.

The VerseCache system uses encryption to ensure that the cached files are only accessible to authorized users. Without the correct key and initialization vector (IV), the cache file would be worthless to someone trying to pry at it to steal content such as the end user/player. The system is designed to be easy to use and highly secure, ensuring that the cached files are protected from unauthorized access.

![Alt text](/screenshot1.png "VerseCache Screenshot 1")
![Alt text](/screenshot2.png "VerseCache Screenshot 2")
![Alt text](/screenshot3.png "VerseCache Screenshot 3")

## Installation
1. Download the latest release of the VerseCache repository as a ZIP file or clone it from the repository.
2. Put the VerseCache.cs in your desired scripts folder/location within the Unity project.
3. Put the VerseCacheEditor.cs script to the projects 'Assets\Editor' folder
4. Add the VerseCache.cs script to an object in your scene.
5. In the inspector, set the cacheFilePath and cacheFilename properties to the path and filename of your cache file if custom. If the file doesn't exist, it will be created when you save.
6. Set the inOutFilepath property to the full path of where you want files to go into the cache from (put), or be extracted to (get). Make sure to include the trailing forward slash /.
7. If encryption mode is enabled, set the encryptionKey and encryptionIV properties to the desired encryption key and initialization vector in base64 format or press the generate button in the inspector.

## Usage
The VerseCache system is designed to work at runtime (but also will work for demoing purposes out-of-box in editor-mode while the project is not running) -- it can be used along with your game client's asset downloader mechanism such as TriLib to check the cache for an existing file first, download if it isn't and put it in the cache, if it's already in there pull it from VerseCache instead of downloading it. To use the system, attach the VerseCache script to an object in your scene, provide the valid cache file/path and input/output file/path in the Inspector, check the desired Put or Get option (one only) and click the Put/Get button.

We can access the system via a reference to the running VerseCache script like so...

Storing a file in the cache:
```
// Read the file data into a byte array
byte[] fileData = File.ReadAllBytes(filePath);
// Store the file data in the cache using the cache filename as the key
VerseCache.StoreFile(cacheFilename, fileData);
```

Retrieving a file from the cache:
```
// Get the cached file data from the cache using the cache filename as the key
byte[] cachedData = VerseCache.GetFile(cacheFilename);
// The cached file data is now available as a byte array, which can be used by an asset loader or other code without needing to save the file to disk first
```

## Inspector
The VerseCache script provides the following properties in the inspector:
- cacheFilePath: The full path of the cache file.
- cacheFilename: The filename of the cache file.
- inOutFilepath: The full path of where you want files to go into the cache from (put), or be extracted to (get). Make sure to include the trailing forward slash /.
- inOutFilename: The input/output filename, depending on isPutInCache and isGetFromCache values.
- encryptionMode: Enables or disables encryption mode. If encryption mode is disabled, files will be stored in plain text.
- encryptionKey: The encryption key in base64 format.
- encryptionIV: The initialization vector in base64 format.
- cacheSizeLimit: The maximum size of the cache file in bytes.


## Other Notes
Note that this script only provides the caching mechanism, and you'll need to customize it to fit your needs by tying it in with your game client code, asset downloader that checks the VerseCache for existing files first, etc.

The VerseCache system is highly customizable and can be adapted to fit your specific needs. You can customize the encryption key and initialization vector, modify the cache file/path and input/output file/path, and safely store/get content.  Note that this script is a cache-only system, and you will need to tie it into your game client code and asset downloader to check the cache for an existing file first.  I highly reccommend using generated UUIDs as the cache filenames for server to client referencing.

### Contact
If you have any questions or suggestions about VerseCache, please feel free to contact me via this GitHub profile.
