# AssetBundle-ContentHasher
This tool can be integrated into a build pipeline in order to generate more reliable hashes for AssetBundles using Unity's built in pipeline.

When building with the built in buildPipeline (Not with ScriptableBuildPipeline) https://docs.unity3d.com/2019.2/Documentation/Manual/AssetBundles-Building.html. The AssetBundle's are accompanied by .manifest files. The assetHash in these files are the mechanism Unity uses to determine if an AssetBundle needs to be rebuilt. It uses the hashes that are used to determine if an Asset needs to be reimported. Doing so on the Assets set to the AssetBundle to determine this assetHash for the AssetBundle.
This hash is calculated by the binary content of the file itself (In the Assets folder, and not the built asset). The platform and importer versions, any post processor versions affecting the Asset, and the .meta file binary.

This assetHash has quite a few flaws, if used as a mechanism for identifying the contents of an AssetBundle. Such as: MonoBehaviour on a script is just a GUID and FileID to reference it in the Editor AssetDatabase (Allowing changes such as filename, namespace etc). So if you were to change something about the MonoScript that it uses to load the Script at runtime. e.g. The namespace. Then the AssetHash of the Scene will not change. This could lead to the AssetBundle not rebuilding or if you were to use the assetHash for the cacheHash which is a common issue. The the bundle for the Scene may be loading an old version, that attempts to load a Script with invalid information. Though these issues are a rare situation for many people, they are hard to debug when they occur; and often happen in live projects which is a big problem.

Unity uses the assetHash for estimating if an AssetBundles need to be rebuilt, improving development iteration times, by not rebuilding AssetBundles during development. Due to the flaws however. You should always do a ForceRebuildAssetBundles build option when you need to make sure that the AssetBundles are correct for the project content. Such as beta or especially final release builds.

The problem comes when this hash is used as an identifier for an AssetBundle during a live game. Such as using the assetHash as the cacheHash (version) when downloading using UnityWebRequest. Because on rare occasions an AssetBundle with content changes, could result in the same assetHash. This resulted in old AssetBundles being used from the cache instead of downloaded the updated ones.

This is where this tool comes into play. Because it generates a hash from the built uncompressed content data contained within an AssetBundle. It will always change whenever the content changes.

Why can I just not use the CRC of the AssetBundle file contained in the manifest?
The CRC of an AssetBundle is calculated based upon the entire uncompressed bytes, and not just of the data. This includes the AssetBundle header information. This includes the Unity version that built the file, as a result of which. When building AssetBundles between different Editor versions the CRC will change. So this cannot be used as an identifier of if the content of the AssetBundle has been changed.

# How to use

Here is an example method for Building AssetBundles which will then calculate the hashes for the AssetBundles and write a report upon completion.

```c#
[MenuItem("Assets/Build AssetBundles/With Hash Report")]
public static void BuildAssetBundle()
{
	string buildLocation = "Assets/StreamingAssets";
	if( ! Directory.Exists(buildLocation) )
		Directory.CreateDirectory(buildLocation);
		
	AssetBundleManifest m = BuildPipeline.BuildAssetBundles( buildLocation, BuildAssetBundleOptions.ForceRebuildAssetbundle, EditorUserBuildSettings.activeBuiltTarget );
	var hasher = BundleHashing.LegacyBundleHashing.GenerateAssetBundleHashes( buildLocation, manifest, Path.Combine(buildLocation, "AssetBundleDetails.json") );
	if( buildLocation.StartsWith( "Assets/" ) )
		hasher.OnCompleted = AssetDatabase.Refresh;
}
```
